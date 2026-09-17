import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage, PhotoView } from '@aspire/contracts';
import { slideKey, slidesOf, viewOf } from './slides';

function image(id: string, ready = true, kind: DreamImage['kind'] = 'dreamt'): DreamImage {
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

function dream(
	images: DreamImage[],
	photoView?: PhotoView,
	layout = 0
): Pick<Dream, 'images' | 'layout' | 'photoView'> {
	return { images, layout, photoView: photoView as PhotoView };
}

const kinds = (slides: ReturnType<typeof slidesOf>) =>
	slides.map((slide) => (slide.kind === 'collage' ? 'collage' : slide.photo.id));

describe('slidesOf (D87)', () => {
	it('is the collage and then each photograph', () => {
		expect(kinds(slidesOf(dream([image('a'), image('b'), image('c')], 'collage')))).toEqual([
			'collage',
			'a',
			'b',
			'c'
		]);
	});

	it('is the photographs alone when the dream is a carousel', () => {
		expect(kinds(slidesOf(dream([image('a'), image('b')], 'carousel')))).toEqual(['a', 'b']);
	});

	it('cuts the collage by the dream’s own template', () => {
		const [first] = slidesOf(dream([image('a'), image('b')], 'collage', 1));
		expect(first.kind === 'collage' && first.template.cols).toBe('1fr 1fr');
	});

	it('is nothing for one photograph or none, which are the photograph and the sky', () => {
		expect(slidesOf(dream([image('a')], 'carousel'))).toEqual([]);
		expect(slidesOf(dream([], 'collage'))).toEqual([]);
	});

	it('counts only photographs that are ready, and never the achieved one', () => {
		const images = [image('a'), image('b', false), image('proof', true, 'achieved')];
		expect(slidesOf(dream(images, 'carousel'))).toEqual([]);
	});

	it('keys every slide apart', () => {
		const keys = slidesOf(dream([image('a'), image('b')])).map(slideKey);
		expect(new Set(keys).size).toBe(keys.length);
	});
});

describe('viewOf', () => {
	it('is a collage for a board remembered from before the choice', () => {
		expect(viewOf({ photoView: undefined as unknown as PhotoView })).toBe('collage');
		expect(viewOf({ photoView: 'carousel' })).toBe('carousel');
	});
});
