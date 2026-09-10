import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import { screenUrls, stale } from './cache';

function image(id: string, ready: boolean, kind: DreamImageKind = 'dreamt'): DreamImage {
	return {
		id,
		sortOrder: 0,
		kind,
		width: 1280,
		height: 853,
		ready,
		thumbUrl: `/media/d/${id}/thumb.webp`,
		screenUrl: `/media/d/${id}/screen.webp`,
		fullUrl: `/media/d/${id}/full.webp`
	};
}

function dream(id: string, images: DreamImage[]): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		sortOrder: 0,
		targetYear: null,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-10T00:00:00Z',
		images
	};
}

describe('screenUrls', () => {
	it('takes the first ready photograph of each dream, at screen size', () => {
		const dreams = [
			dream('a', [image('a1', false), image('a2', true), image('a3', true)]),
			dream('b', [image('b1', true)]),
			dream('c', []),
			dream('d', [image('d1', false)])
		];
		expect(screenUrls(dreams)).toEqual(['/media/d/a2/screen.webp', '/media/d/b1/screen.webp']);
	});

	it('keeps both halves of a pair, so the wall is whole without a signal', () => {
		const dreams = [
			dream('a', [image('a1', true), image('a2', true, 'achieved')]),
			// The achieved one alone is still worth keeping: it is what the
			// wall shows when a dream never had a dreamt photograph.
			dream('b', [image('b1', true, 'achieved')])
		];

		expect(screenUrls(dreams)).toEqual([
			'/media/d/a1/screen.webp',
			'/media/d/a2/screen.webp',
			'/media/d/b1/screen.webp'
		]);
	});
});

describe('stale', () => {
	it('names what is cached and no longer wanted', () => {
		expect(stale(['/x', '/y', '/z'], ['/y'])).toEqual(['/x', '/z']);
		expect(stale([], ['/y'])).toEqual([]);
	});
});
