import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage } from '@aspire/contracts';
import {
	MAX_CANVAS_EDGE,
	MAX_ON_WALLPAPER,
	MIN_CANVAS_EDGE,
	canvasFor,
	toggleChosen,
	wallpaperCandidates
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
		thumbUrl: `/media/d/${id}/thumb.webp`,
		screenUrl: `/media/d/${id}/screen.webp`,
		fullUrl: `/media/d/${id}/full.webp`
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
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
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
