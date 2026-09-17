import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage } from '@aspire/contracts';
import {
	MAX_CANVAS_EDGE,
	MAX_ON_WALLPAPER,
	MIN_CANVAS_EDGE,
	canvasFor,
	toggleChosen,
	wallpaperCandidates,
	wallpaperLink,
	wallpaperPick
} from './wallpaper';

function image(id: string, kind: DreamImage['kind'], ready: boolean): DreamImage {
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
		photoView: 'collage',
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
		updatedAt: '2026-09-01T00:00:00Z',
		images,
		...over
	};
}

describe('wallpaperCandidates', () => {
	it('takes the dreams with a dreamt photograph that is ready', () => {
		const rows = [
			dream('ready', [image('a', 'dreamt', true)]),
			dream('waiting', [image('b', 'dreamt', false)]),
			dream('none', [])
		];

		expect(wallpaperCandidates(rows).map((d) => d.id)).toEqual(['ready']);
	});

	it('does not count an achieved photograph as the dream', () => {
		const rows = [dream('proof-only', [image('a', 'achieved', true)])];

		expect(wallpaperCandidates(rows)).toEqual([]);
	});

	it('keeps an achieved dream, which is exactly the kind you want up there', () => {
		const rows = [
			dream('done', [image('a', 'dreamt', true), image('b', 'achieved', true)], {
				status: 'achieved',
				achievedAt: '2026-09-01T00:00:00Z'
			})
		];

		expect(wallpaperCandidates(rows).map((d) => d.id)).toEqual(['done']);
	});
});

describe('wallpaperPick', () => {
	const NOW = new Date('2026-09-10T09:00:00Z');
	const A_WEEK_AGO = '2026-09-03T09:00:00Z';
	const TEN_DAYS_AGO = '2026-08-31T09:00:00Z';

	/** A dream with a ready photograph, which is the only kind that can be on one. */
	function shot(id: string, over: Partial<Dream> = {}): Dream {
		return dream(id, [image(`i-${id}`, 'dreamt', true)], over);
	}

	it('puts the dream of the day first, then the ones with the most fuel', () => {
		const rows = [
			shot('week', { lastShownAt: A_WEEK_AGO, sortOrder: 0 }),
			shot('picked', { lastShownAt: '2026-09-10T07:00:00Z', sortOrder: 1 }),
			shot('loved', { lastShownAt: A_WEEK_AGO, likes: 10, sortOrder: 2 })
		];

		expect(wallpaperPick(rows, 'picked', 3, NOW).map((d) => d.id)).toEqual([
			'picked',
			'loved',
			'week'
		]);
	});

	it('takes the dreams nobody has seen before any that have been', () => {
		const rows = [
			shot('loved', { lastShownAt: TEN_DAYS_AGO, likes: 10 }),
			shot('never', { sortOrder: 1 })
		];

		expect(wallpaperPick(rows, null, 2, NOW).map((d) => d.id)).toEqual(['never', 'loved']);
	});

	it('falls to board order between two nobody has seen', () => {
		const rows = [shot('second', { sortOrder: 2 }), shot('first', { sortOrder: 1 })];

		expect(wallpaperPick(rows, null, 2, NOW).map((d) => d.id)).toEqual(['first', 'second']);
	});

	it('leaves out an achieved dream, and one whose photograph is not ready', () => {
		const rows = [
			shot('ahead', { lastShownAt: A_WEEK_AGO }),
			shot('done', { status: 'achieved', achievedAt: A_WEEK_AGO }),
			dream('waiting', [image('w', 'dreamt', false)], { lastShownAt: TEN_DAYS_AGO })
		];

		expect(wallpaperPick(rows, null, 6, NOW).map((d) => d.id)).toEqual(['ahead']);
	});

	it('never hands back more than the collage holds', () => {
		const rows = Array.from({ length: 9 }, (_, i) => shot(`d${i}`, { sortOrder: i }));

		expect(wallpaperPick(rows, null, undefined, NOW)).toHaveLength(MAX_ON_WALLPAPER);
	});

	it('is nothing at all when no dream ahead has a photograph', () => {
		expect(wallpaperPick([dream('none', [])], null, 6, NOW)).toEqual([]);
	});

	it('ignores a pick that cannot be on one', () => {
		const rows = [shot('ahead', { lastShownAt: A_WEEK_AGO }), dream('none', [])];

		expect(wallpaperPick(rows, 'none', 6, NOW).map((d) => d.id)).toEqual(['ahead']);
	});
});

describe('wallpaperLink', () => {
	const CANVAS = { width: 1179, height: 2556 };

	it('carries the canvas and the day of the phone it was copied on', () => {
		expect(wallpaperLink('https://sny.example', '/api/v1/w/abc', CANVAS, 120)).toBe(
			'https://sny.example/api/v1/w/abc?width=1179&height=2556&offset=120'
		);
	});

	it('says a negative offset as one, rather than as an escape', () => {
		// `-` is not escaped in a query value, and a Shortcut is holding this
		// string by hand: anything that needs decoding is a typo waiting.
		expect(wallpaperLink('https://sny.example', '/w/k', CANVAS, -300)).toContain('offset=-300');
	});

	it('is a URL the browser itself can parse back', () => {
		const url = new URL(wallpaperLink('https://sny.example', '/api/v1/w/abc', CANVAS, 0));

		expect(url.pathname).toBe('/api/v1/w/abc');
		expect(url.searchParams.get('offset')).toBe('0');
		expect(url.searchParams.get('width')).toBe('1179');
	});
});

describe('canvasFor', () => {
	it('is the screen in real pixels', () => {
		expect(canvasFor(390, 844, 3)).toEqual({ width: 1170, height: 2532 });
	});

	it('rounds a ratio that is not whole', () => {
		expect(canvasFor(412, 915, 2.625)).toEqual({ width: 1082, height: 2402 });
	});

	it('clamps to what the server will draw', () => {
		expect(canvasFor(10, 10, 1)).toEqual({ width: MIN_CANVAS_EDGE, height: MIN_CANVAS_EDGE });
		expect(canvasFor(3000, 5000, 4)).toEqual({
			width: MAX_CANVAS_EDGE,
			height: MAX_CANVAS_EDGE
		});
	});

	it('survives a screen it cannot read', () => {
		expect(canvasFor(0, 0, 0)).toEqual({ width: MIN_CANVAS_EDGE, height: MIN_CANVAS_EDGE });
		expect(canvasFor(NaN, 844, 3)).toHaveProperty('width', MIN_CANVAS_EDGE);
	});
});

describe('toggleChosen', () => {
	it('adds in the order tapped and takes back out', () => {
		expect(toggleChosen(['a'], 'b')).toEqual(['a', 'b']);
		expect(toggleChosen(['a', 'b'], 'a')).toEqual(['b']);
	});

	it('stops at the limit rather than dropping the first', () => {
		const full = ['a', 'b', 'c', 'd', 'e', 'f'];
		expect(full).toHaveLength(MAX_ON_WALLPAPER);

		expect(toggleChosen(full, 'g')).toEqual(full);
		// And there is still room again once one is taken off.
		expect(toggleChosen(toggleChosen(full, 'a'), 'g')).toEqual(['b', 'c', 'd', 'e', 'f', 'g']);
	});
});
