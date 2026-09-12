import { describe, expect, it, vi } from 'vitest';
import type { Dream, DreamImage, DreamImageKind, FocalInput } from '@aspire/contracts';
import { READY_ATTEMPTS, photographDone, replacePhotograph, type PhotoApi } from './upload';

function image(id: string, kind: DreamImageKind, ready = true): DreamImage {
	return {
		id,
		sortOrder: 0,
		kind,
		width: 1600,
		height: 2000,
		ready,
		focusX: 0.5,
		focusY: 0.5,
		zoom: 1,
		thumbUrl: `/media/d/${id}/thumb.webp`,
		screenUrl: `/media/d/${id}/screen.webp`,
		largeUrl: `/media/d/${id}/full.webp`
	};
}

function dream(images: DreamImage[]): Dream {
	return {
		id: 'dream-1',
		title: 'Island na kole',
		why: '',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder: 0,
		focusRank: null,
		targetYear: null,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-12T08:00:00.000Z',
		images
	};
}

const photo = new Blob(['not really a jpeg'], { type: 'image/jpeg' });

/** The API, written down: what was called, in what order, and what it answers. */
function fake(answers: Dream[]) {
	const calls: string[] = [];
	const api: PhotoApi = {
		deleteImage: (dreamId, imageId) => {
			calls.push(`delete ${imageId}`);
			return Promise.resolve();
		},
		uploadImage: (dreamId, blob, kind) => {
			calls.push(`upload ${kind}`);
			return Promise.resolve(image('new', kind, false));
		},
		getDream: () => {
			calls.push('get');
			return Promise.resolve(answers.shift() ?? dream([]));
		}
	};
	// No timers: the wait is the thing a test should not actually do.
	return { api, calls, wait: () => Promise.resolve() };
}

describe('replacePhotograph', () => {
	it('takes the old photograph out before it puts the new one in', async () => {
		// So an upload that fails leaves the sky rather than the wrong picture.
		const { api, calls, wait } = fake([dream([image('new', 'dreamt')])]);

		await replacePhotograph(dream([image('old', 'dreamt')]), photo, 'dreamt', api, undefined, wait);

		expect(calls.slice(0, 2)).toEqual(['delete old', 'upload dreamt']);
	});

	it('leaves the other kind where it is', async () => {
		// Changing the dreamt photograph must never take the proof with it (D28).
		const { api, calls, wait } = fake([
			dream([image('new', 'dreamt'), image('proof', 'achieved')])
		]);
		const both = dream([image('old', 'dreamt'), image('proof', 'achieved')]);

		await replacePhotograph(both, photo, 'dreamt', api, undefined, wait);

		expect(calls.filter((call) => call.startsWith('delete'))).toEqual(['delete old']);
	});

	it('also takes out one that is still being resized', async () => {
		// Or it comes back as a second picture a moment later.
		const { api, calls, wait } = fake([dream([image('new', 'dreamt')])]);

		await replacePhotograph(
			dream([image('waiting', 'dreamt', false)]),
			photo,
			'dreamt',
			api,
			undefined,
			wait
		);

		expect(calls).toContain('delete waiting');
	});

	it('adds a photograph to a dream that had none', async () => {
		const { api, calls, wait } = fake([dream([image('new', 'dreamt')])]);

		const after = await replacePhotograph(dream([]), photo, 'dreamt', api, undefined, wait);

		expect(calls.filter((call) => call.startsWith('delete'))).toEqual([]);
		expect(after.images.map((i) => i.id)).toEqual(['new']);
	});

	it('sends the crop with the photograph', async () => {
		// A picture is positioned before it is sent, so the upload carries it
		// rather than a second request doing it afterwards (D54).
		const sent: (FocalInput | undefined)[] = [];
		const { api, wait } = fake([dream([image('new', 'dreamt')])]);
		const watching: PhotoApi = {
			...api,
			uploadImage: (dreamId, blob, kind, focal) => {
				sent.push(focal);
				return api.uploadImage(dreamId, blob, kind, focal);
			}
		};

		await replacePhotograph(
			dream([]),
			photo,
			'dreamt',
			watching,
			{ focusX: 0.2, focusY: 0.8, zoom: 1.5 },
			wait
		);

		expect(sent).toEqual([{ focusX: 0.2, focusY: 0.8, zoom: 1.5 }]);
	});

	it('stops asking the moment the sizes are ready', async () => {
		const { api, calls, wait } = fake([
			dream([image('new', 'dreamt', false)]),
			dream([image('new', 'dreamt', true)])
		]);

		const after = await replacePhotograph(dream([]), photo, 'dreamt', api, undefined, wait);

		expect(calls.filter((call) => call === 'get')).toHaveLength(2);
		expect(after.images[0].ready).toBe(true);
	});

	it('gives up in the end and hands back the dream as it stands', async () => {
		// A slow box is not a reason to ask forever. The row is there, the tile
		// shows the sky, and the next open of the board has the picture.
		const { api, calls, wait } = fake([]);

		const after = await replacePhotograph(dream([]), photo, 'dreamt', api, undefined, wait);

		expect(calls.filter((call) => call === 'get')).toHaveLength(READY_ATTEMPTS);
		expect(after.images).toEqual([]);
	});

	it('throws what the upload threw, with the old photograph already gone', async () => {
		const { calls, wait } = fake([]);
		const api: PhotoApi = {
			deleteImage: (_dreamId, imageId) => {
				calls.push(`delete ${imageId}`);
				return Promise.resolve();
			},
			uploadImage: () => Promise.reject(new Error('413')),
			getDream: vi.fn()
		};

		await expect(
			replacePhotograph(dream([image('old', 'dreamt')]), photo, 'dreamt', api, undefined, wait)
		).rejects.toThrow('413');
		expect(calls).toEqual(['delete old']);
		expect(api.getDream).not.toHaveBeenCalled();
	});
});

describe('photographDone', () => {
	it('says which of the two pictures it was', () => {
		expect(photographDone('dreamt')).toBe('Fotka je na nástěnce');
		expect(photographDone('achieved')).toBe('Skutečná fotka je u snu');
	});
});
