import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import {
	PREFETCH_AT_ONCE,
	aheadOf,
	overCap,
	pooled,
	prefetchOrder,
	stale,
	tileUrls
} from './cache';
import { REEL_WINDOW } from '$lib/dreams/board';

function image(id: string, ready: boolean, kind: DreamImageKind = 'dreamt'): DreamImage {
	return {
		id,
		sortOrder: 0,
		kind,
		width: 1280,
		height: 853,
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

function dream(id: string, images: DreamImage[], over: Partial<Dream> = {}): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder: 0,
		focusRank: null,
		targetYear: null,
		layout: 0,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-10T00:00:00Z',
		updatedAt: '2026-09-10T00:00:00Z',
		images,
		...over
	};
}

/** A promise somebody else decides when to settle. */
function deferred() {
	let settle!: () => void;
	const promise = new Promise<void>((resolve) => {
		settle = resolve;
	});
	return { promise, settle };
}

const laptop = { dpr: 1, metered: false };
const phone = { dpr: 3, metered: false };

describe('tileUrls', () => {
	it('takes the first ready photograph of each dream: the thumb, then the picture', () => {
		const dreams = [
			// One still being made and one ready: a single photograph, not a collage.
			dream('a', [image('a1', false), image('a2', true)]),
			dream('b', [image('b1', true)]),
			dream('c', []),
			dream('d', [image('d1', false)])
		];
		expect(tileUrls(dreams, laptop)).toEqual([
			'/media/d/a2/thumb.webp',
			'/media/d/a2/screen.webp',
			'/media/d/b1/thumb.webp',
			'/media/d/b1/screen.webp'
		]);
	});

	it('asks for the rung the screen reads, so what is fetched ahead is what is shown', () => {
		const dreams = [dream('a', [image('a1', true)])];
		expect(tileUrls(dreams, phone)).toEqual(['/media/d/a1/thumb.webp', '/media/d/a1/full.webp']);
	});

	it('keeps both halves of a pair, so the wall is whole without a signal', () => {
		const dreams = [
			dream('a', [image('a1', true), image('a2', true, 'achieved')]),
			// The achieved one alone is still worth keeping: it is what the
			// wall shows when a dream never had a dreamt photograph.
			dream('b', [image('b1', true, 'achieved')])
		];

		expect(tileUrls(dreams, laptop)).toEqual([
			'/media/d/a1/thumb.webp',
			'/media/d/a1/screen.webp',
			'/media/d/a2/screen.webp',
			'/media/d/b1/screen.webp'
		]);
	});
});

describe('prefetchOrder', () => {
	it('is the reel as it will be swiped, and then the wall', () => {
		const rows = [
			dream('a', []),
			dream('done', [], { status: 'achieved', achievedAt: '2026-09-01T00:00:00Z' }),
			dream('b', []),
			dream('c', [])
		];

		// The sequence is the shuffle the board opened with (D30); the
		// achieved dream is not in it and goes on the end.
		expect(prefetchOrder(rows, ['c', 'a', 'b']).map((d) => d.id)).toEqual(['c', 'a', 'b', 'done']);
	});

	it('keeps every dream exactly once, however the reel was shuffled', () => {
		const rows = [
			dream('a', []),
			dream('b', []),
			dream('old', [], { status: 'achieved', achievedAt: '2026-08-01T00:00:00Z' }),
			dream('new', [], { status: 'achieved', achievedAt: '2026-09-01T00:00:00Z' })
		];

		const ordered = prefetchOrder(rows, ['b', 'a']).map((d) => d.id);

		expect(ordered).toHaveLength(rows.length);
		expect([...ordered].sort()).toEqual(['a', 'b', 'new', 'old']);
	});

	it('is board order when the board has not been shuffled yet', () => {
		const rows = [dream('a', []), dream('b', [])];

		expect(prefetchOrder(rows, []).map((d) => d.id)).toEqual(['a', 'b']);
	});
});

describe('pooled', () => {
	it('runs every task, in the order it was given', async () => {
		const started: number[] = [];
		const tasks = Array.from({ length: 10 }, (_, i) => async () => {
			started.push(i);
		});

		await pooled(tasks, 3);

		expect(started).toEqual([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
	});

	it('never has more than the limit in flight', async () => {
		let running = 0;
		let most = 0;
		const gates = Array.from({ length: 9 }, () => deferred());
		const tasks = gates.map((gate) => async () => {
			running++;
			most = Math.max(most, running);
			await gate.promise;
			running--;
		});

		const all = pooled(tasks, 3);

		// Three are in flight and the other six are waiting their turn.
		await Promise.resolve();
		expect(most).toBe(3);

		for (const gate of gates) gate.settle();
		await all;

		expect(most).toBe(3);
		expect(running).toBe(0);
	});

	it('does not let one failure stop the rest', async () => {
		const done: string[] = [];
		const tasks = [
			async () => {
				done.push('first');
			},
			async () => {
				throw new Error('this photograph will not come');
			},
			async () => {
				done.push('third');
			}
		];

		await expect(pooled(tasks, 2)).resolves.toBeUndefined();
		expect(done).toEqual(['first', 'third']);
	});

	it('has nothing to do with an empty list, and does not hang on one', async () => {
		await expect(pooled([], PREFETCH_AT_ONCE)).resolves.toBeUndefined();
	});

	it('runs fewer workers than the limit when there is less to do', async () => {
		const done: number[] = [];
		const tasks = [
			async () => {
				done.push(1);
			}
		];

		await pooled(tasks, PREFETCH_AT_ONCE);

		expect(done).toEqual([1]);
	});
});

describe('stale', () => {
	it('names what is cached and no longer wanted', () => {
		expect(stale(['/x', '/y', '/z'], ['/y'])).toEqual(['/x', '/z']);
		expect(stale([], ['/y'])).toEqual([]);
	});
});

describe('overCap', () => {
	it('has no ceiling when there is none to have', () => {
		expect(overCap(['/a', '/b', '/c'], null)).toEqual([]);
	});

	it('leaves a cache that is under the ceiling alone', () => {
		expect(overCap(['/a', '/b'], 2)).toEqual([]);
		expect(overCap([], 2)).toEqual([]);
	});

	it('drops the oldest fetched, which is the front of the list', () => {
		// The Cache API hands its keys back in insertion order, so the window
		// that just arrived is at the end and is never what goes.
		expect(overCap(['/old', '/older', '/new'], 1)).toEqual(['/old', '/older']);
	});
});

describe('aheadOf', () => {
	const reel = Array.from({ length: 12 }, (_, i) => dream(`d${i}`, [image(`i${i}`, true)]));

	it('asks for the tiles in the document and one screenful more', () => {
		expect(aheadOf(reel, REEL_WINDOW).map((d) => d.id)).toEqual([
			'd0',
			'd1',
			'd2',
			'd3',
			'd4',
			'd5',
			'd6',
			'd7',
			'd8',
			'd9'
		]);
	});

	it('grows with the window, which is what makes a morning cost ten and not a hundred', () => {
		expect(aheadOf(reel, 5)).toHaveLength(10);
		expect(aheadOf(reel, 10)).toHaveLength(12);
	});

	it('stops at the end of the reel rather than running off it', () => {
		expect(aheadOf(reel, 12)).toHaveLength(12);
		expect(aheadOf(reel.slice(0, 3), REEL_WINDOW)).toHaveLength(3);
		expect(aheadOf([], REEL_WINDOW)).toEqual([]);
	});
});

describe('tileUrls, for a collage (D82)', () => {
	it('keeps every cell at the rung a cell reads, and the cover for the list', () => {
		const three = dream('house', [image('a', true), image('b', true), image('c', true)]);

		expect(tileUrls([three], { dpr: 3, metered: false })).toEqual([
			'/media/d/a/thumb.webp',
			'/media/d/a/screen.webp',
			'/media/d/b/screen.webp',
			'/media/d/c/screen.webp'
		]);
	});

	it('does not count a photograph that is still being made as a cell', () => {
		// One ready and one on its way is still a single photograph on the tile.
		const one = dream('house', [image('a', true), image('b', false)]);

		expect(tileUrls([one], { dpr: 1, metered: false })).toEqual([
			'/media/d/a/thumb.webp',
			'/media/d/a/screen.webp'
		]);
	});
});
