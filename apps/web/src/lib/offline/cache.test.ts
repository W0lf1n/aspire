import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage } from '@aspire/contracts';
import { screenUrls, stale } from './cache';

function image(id: string, ready: boolean): DreamImage {
	return {
		id,
		sortOrder: 0,
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
		status: 'dreaming',
		sortOrder: 0,
		targetYear: null,
		likes: 0,
		achievedAt: null,
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
});

describe('stale', () => {
	it('names what is cached and no longer wanted', () => {
		expect(stale(['/x', '/y', '/z'], ['/y'])).toEqual(['/x', '/z']);
		expect(stale([], ['/y'])).toEqual([]);
	});
});
