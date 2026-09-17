import { describe, expect, it, vi } from 'vitest';
import type { Dream, DreamImage, DreamImageKind, FocalInput } from '@aspire/contracts';
import {
	READY_ATTEMPTS,
	addPhotographs,
	photographDone,
	photographsAdded,
	replacePhotograph,
	roomFor,
	type PhotoApi
} from './upload';

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
		fit: 'fill',
		mat: 'night',
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
		layout: 0,
		photoView: 'collage',
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-12T08:00:00.000Z',
		updatedAt: '2026-09-12T08:00:00.000Z',
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

	it('says how many, when it was more than one (D84)', () => {
		expect(photographDone('dreamt', 3)).toBe('Fotky jsou na nástěnce');
		expect(photographsAdded(1)).toBe('Fotka je u snu');
		expect(photographsAdded(4)).toBe('Fotky jsou u snu');
	});
});

describe('addPhotographs (D82, D84)', () => {
	it('takes nothing out: that is the whole difference from replacing', async () => {
		const had = dream([image('first', 'dreamt')]);
		const { api, calls, wait } = fake([dream([image('first', 'dreamt'), image('new', 'dreamt')])]);

		await addPhotographs(had, [photo], api, undefined, wait);

		expect(calls.filter((call) => call.startsWith('delete'))).toEqual([]);
		expect(calls[0]).toBe('upload dreamt');
	});

	it('waits for this photograph, not for any of its kind', async () => {
		// The dream had a ready one before the upload began, so „is there a
		// ready dreamt photograph“ would say yes on the first look.
		const waiting = dream([image('first', 'dreamt'), image('new', 'dreamt', false)]);
		const done = dream([image('first', 'dreamt'), image('new', 'dreamt')]);
		const { api, calls, wait } = fake([waiting, waiting, done]);

		const after = await addPhotographs(
			dream([image('first', 'dreamt')]),
			[photo],
			api,
			undefined,
			wait
		);

		expect(calls.filter((call) => call === 'get')).toHaveLength(3);
		expect(after.dream).toBe(done);
	});

	it('hands the dream back as it stands when the sizes never come', async () => {
		const waiting = dream([image('first', 'dreamt'), image('new', 'dreamt', false)]);
		const { api, calls, wait } = fake(Array.from({ length: READY_ATTEMPTS + 5 }, () => waiting));

		const after = await addPhotographs(
			dream([image('first', 'dreamt')]),
			[photo],
			api,
			undefined,
			wait
		);

		expect(calls.filter((call) => call === 'get')).toHaveLength(READY_ATTEMPTS);
		expect(after.dream).toBe(waiting);
	});

	/** An API that numbers what it is sent, and refuses the upload numbered `refuse`. */
	function numbered(answers: Dream[], refuse = 0) {
		const sent: { id: string; focal: FocalInput | undefined }[] = [];
		const gets: string[] = [];
		const api: Pick<PhotoApi, 'uploadImage' | 'getDream'> = {
			uploadImage: (_dreamId, _blob, kind, focal) => {
				const id = `p${sent.length + 1}`;
				if (sent.length + 1 === refuse) return Promise.reject(new Error('409'));
				sent.push({ id, focal });
				return Promise.resolve(image(id, kind, false));
			},
			getDream: () => {
				gets.push('get');
				return Promise.resolve(answers.shift() ?? dream([]));
			}
		};
		return { api, sent, gets, wait: () => Promise.resolve() };
	}

	it('sends several in the order they were picked, the crop with the first alone', async () => {
		// The server stands each behind the last, so the first picked is the cover.
		const three = dream([image('p1', 'dreamt'), image('p2', 'dreamt'), image('p3', 'dreamt')]);
		const { api, sent, wait } = numbered([three]);
		const crop = { focusX: 0.3, focusY: 0.6, zoom: 1.2 };

		const after = await addPhotographs(dream([]), [photo, photo, photo], api, crop, wait);

		expect(sent).toEqual([
			{ id: 'p1', focal: crop },
			{ id: 'p2', focal: undefined },
			{ id: 'p3', focal: undefined }
		]);
		expect(after).toEqual({ dream: three, sent: 3, failed: null });
	});

	it('waits once, until every one of them is ready', async () => {
		const half = dream([image('p1', 'dreamt'), image('p2', 'dreamt', false)]);
		const all = dream([image('p1', 'dreamt'), image('p2', 'dreamt')]);
		const { api, gets, wait } = numbered([half, half, all]);

		const after = await addPhotographs(dream([]), [photo, photo], api, undefined, wait);

		expect(gets).toHaveLength(3);
		expect(after.dream).toBe(all);
	});

	it('keeps what was sent when one is refused halfway, and says why', async () => {
		// Two are on the server by then; a screen that threw would show neither.
		const two = dream([image('p1', 'dreamt'), image('p2', 'dreamt')]);
		const { api, sent, wait } = numbered([two], 3);

		const after = await addPhotographs(
			dream([]),
			[photo, photo, photo, photo],
			api,
			undefined,
			wait
		);

		expect(sent.map((one) => one.id)).toEqual(['p1', 'p2']);
		expect(after.dream).toBe(two);
		expect(after.sent).toBe(2);
		expect(after.failed).toEqual(new Error('409'));
	});

	it('asks for nothing when the first is refused', async () => {
		const had = dream([]);
		const { api, gets, wait } = numbered([], 1);

		const after = await addPhotographs(had, [photo, photo], api, undefined, wait);

		expect(gets).toEqual([]);
		expect(after).toEqual({ dream: had, sent: 0, failed: new Error('409') });
	});
});

describe('roomFor (D84)', () => {
	const picked = ['a', 'b', 'c', 'd', 'e', 'f', 'g'];

	it('takes every one when they fit, and has nothing to say', () => {
		expect(roomFor(['a', 'b'], 0)).toEqual({ taken: ['a', 'b'], note: null });
		expect(roomFor(picked.slice(0, 5), 0)).toEqual({ taken: picked.slice(0, 5), note: null });
	});

	it('takes the first ones that fit, in the order picked, and says so', () => {
		expect(roomFor(picked, 0)).toEqual({
			taken: ['a', 'b', 'c', 'd', 'e'],
			note: 'Ke snu se vejde nejvýš 5 fotek, beru prvních 5.'
		});
		expect(roomFor(picked, 2)).toEqual({
			taken: ['a', 'b', 'c'],
			note: 'Ke snu se vejdou ještě 3 fotky, beru první 3.'
		});
		expect(roomFor(picked, 4)).toEqual({
			taken: ['a'],
			note: 'Ke snu se vejde ještě jedna fotka, beru první.'
		});
	});

	it('takes none onto a dream that is full', () => {
		expect(roomFor(['a'], 5)).toEqual({
			taken: [],
			note: 'Ke snu se už žádná další fotka nevejde.'
		});
	});
});
