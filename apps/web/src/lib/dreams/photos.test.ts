import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import { photoOf, photoStyle, photosOf, photosToReplace } from './photos';

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
		fullUrl: `/media/d/${id}/full.webp`
	};
}

const dream = (images: DreamImage[]): Pick<Dream, 'images'> => ({ images });

describe('photoOf', () => {
	it('takes the first ready one of the kind asked for', () => {
		const rows = dream([image('a', 'achieved'), image('d', 'dreamt')]);

		expect(photoOf(rows, 'dreamt')?.id).toBe('d');
		expect(photoOf(rows, 'achieved')?.id).toBe('a');
	});

	it('does not show one whose sizes are not ready', () => {
		expect(photoOf(dream([image('d', 'dreamt', false)]), 'dreamt')).toBeNull();
	});

	it('is nothing when the dream has no photograph of that kind', () => {
		expect(photoOf(dream([image('d', 'dreamt')]), 'achieved')).toBeNull();
		expect(photoOf(dream([]), 'dreamt')).toBeNull();
	});
});

describe('photosOf', () => {
	it('gives the pair the wall stands side by side', () => {
		const both = photosOf(dream([image('d', 'dreamt'), image('a', 'achieved')]));

		expect(both.dreamt?.id).toBe('d');
		expect(both.achieved?.id).toBe('a');
	});

	it('gives what there is when there is only one', () => {
		expect(photosOf(dream([image('d', 'dreamt')]))).toEqual({
			dreamt: expect.objectContaining({ id: 'd' }),
			achieved: null
		});
	});
});

describe('photosToReplace', () => {
	it('takes only the kind being replaced, and the ones still resizing too', () => {
		const rows = dream([
			image('d1', 'dreamt'),
			image('d2', 'dreamt', false),
			image('a', 'achieved')
		]);

		expect(photosToReplace(rows, 'dreamt').map((i) => i.id)).toEqual(['d1', 'd2']);
		expect(photosToReplace(rows, 'achieved').map((i) => i.id)).toEqual(['a']);
	});
});

describe('photoStyle', () => {
	const at = (focusX: number, focusY: number, zoom: number) => ({ focusX, focusY, zoom });

	it('says nothing at all for a photograph nobody has moved', () => {
		// The default is what `object-fit: cover` already does, and an identity
		// transform on five full-screen tiles is five layers bought for nothing.
		expect(photoStyle(at(0.5, 0.5, 1))).toBe('');
		expect(photoStyle(null)).toBe('');
	});

	it('is the object-position the point means', () => {
		expect(photoStyle(at(0.25, 0.75, 1))).toBe('object-position:25% 75%;');
	});

	it('scales around the point it is looking at', () => {
		// The origin is the point, so what is being looked at stays where it is
		// while the picture grows around it.
		expect(photoStyle(at(0.5, 0.5, 2))).toBe('transform:scale(2);transform-origin:50% 50%;');
		expect(photoStyle(at(0.2, 0.4, 1.5))).toBe(
			'object-position:20% 40%;transform:scale(1.5);transform-origin:20% 40%;'
		);
	});

	it('takes numbers that cannot mean anything back to something that can', () => {
		expect(photoStyle(at(-1, 5, 0.2))).toBe('object-position:0% 100%;');
		expect(photoStyle(at(Number.NaN, 0.5, Number.NaN))).toBe('');
	});
});
