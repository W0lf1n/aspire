import { describe, expect, it } from 'vitest';
import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import {
	photoComing,
	photoOf,
	matIsBlur,
	matStyle,
	photoStyle,
	photosOf,
	photosToReplace,
	reelUrl,
	tileStyle,
	rungFor
} from './photos';

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
	const at = (focusX: number, focusY: number, zoom: number) => ({
		focusX,
		focusY,
		zoom,
		fit: 'fill' as const,
		mat: 'night' as const
	});

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

describe('rungFor', () => {
	it('is the large rung on a phone on wifi, which is 2× or 3×', () => {
		expect(rungFor({ dpr: 2, metered: false })).toBe('large');
		expect(rungFor({ dpr: 3, metered: false })).toBe('large');
	});

	it('is the screen rung on a laptop at 1×, where 1280 is already more than the pixels', () => {
		expect(rungFor({ dpr: 1, metered: false })).toBe('screen');
		expect(rungFor({ dpr: 1.5, metered: false })).toBe('screen');
	});

	it('is the screen rung on mobile data, whatever the screen (D66)', () => {
		expect(rungFor({ dpr: 3, metered: true })).toBe('screen');
		expect(rungFor({ dpr: 2, metered: true })).toBe('screen');
	});

	it('is the large rung where the browser will not say, which is every iPhone', () => {
		// Safari has no Network Information API, and the phone this app is
		// built for must still get the rung that was built for it.
		expect(rungFor({ dpr: 3, metered: null })).toBe('large');
	});
});

describe('reelUrl', () => {
	it('is the URL of the rung the screen reads', () => {
		const photo = image('d', 'dreamt');
		expect(reelUrl(photo, { dpr: 3, metered: false })).toBe('/media/d/d/full.webp');
		expect(reelUrl(photo, { dpr: 1, metered: false })).toBe('/media/d/d/screen.webp');
		expect(reelUrl(photo, { dpr: 3, metered: true })).toBe('/media/d/d/screen.webp');
	});

	it('is nothing for no photograph', () => {
		expect(reelUrl(null, { dpr: 3, metered: false })).toBeNull();
	});
});

describe('photoComing', () => {
	it('is true for a row whose sizes are not made yet', () => {
		expect(photoComing(dream([image('d', 'dreamt', false)]), 'dreamt')).toBe(true);
	});

	it('is false once the sizes are there, which is when the tile can show it', () => {
		expect(photoComing(dream([image('d', 'dreamt')]), 'dreamt')).toBe(false);
	});

	it('is false for a dream with no photograph at all — the sky that wants one (D52)', () => {
		expect(photoComing(dream([]), 'dreamt')).toBe(false);
	});

	it('does not mistake the other kind for this one', () => {
		const rows = dream([image('a', 'achieved', false)]);

		expect(photoComing(rows, 'dreamt')).toBe(false);
		expect(photoComing(rows, 'achieved')).toBe(true);
	});
});

describe('a photograph shown whole (D81)', () => {
	const whole = (focusX: number, focusY: number, zoom: number, mat: 'night' | 'dusk' | 'blur') => ({
		focusX,
		focusY,
		zoom,
		fit: 'whole' as const,
		mat
	});

	it('is contained on a tile, placed and scaled by the same two properties', () => {
		expect(tileStyle(whole(0.5, 0.5, 1, 'night'))).toBe('object-fit:contain;');
		expect(tileStyle(whole(0.5, 0.2, 1.5, 'night'))).toBe(
			'object-fit:contain;object-position:50% 20%;transform:scale(1.5);transform-origin:50% 20%;'
		);
	});

	it('is its point alone wherever a photograph always fills its frame', () => {
		// Twice the whole picture is not twice the crop: a circle ignores it.
		expect(photoStyle(whole(0.5, 0.2, 2, 'night'))).toBe('object-position:50% 20%;');
	});

	it('fills a tile exactly as it always did when it is not whole', () => {
		const filled = {
			focusX: 0.2,
			focusY: 0.4,
			zoom: 1.5,
			fit: 'fill' as const,
			mat: 'dusk' as const
		};
		expect(tileStyle(filled)).toBe(photoStyle(filled));
	});

	it('names its mat by the token and never by a colour', () => {
		expect(matStyle(whole(0.5, 0.5, 1, 'dusk'))).toBe('background:var(--mat-dusk);');
		expect(matStyle(whole(0.5, 0.5, 1, 'night'))).toBe('background:var(--mat-night);');
	});

	it('has no mat colour when it fills, and none when the mat is the photograph', () => {
		expect(matStyle({ fit: 'fill', mat: 'dusk' })).toBe('');
		expect(matStyle(whole(0.5, 0.5, 1, 'blur'))).toBe('');
		expect(matStyle(null)).toBe('');
	});

	it('knows when the mat is the blurred thumb', () => {
		expect(matIsBlur(whole(0.5, 0.5, 1, 'blur'))).toBe(true);
		expect(matIsBlur(whole(0.5, 0.5, 1, 'night'))).toBe(false);
		expect(matIsBlur({ fit: 'fill', mat: 'blur' })).toBe(false);
		expect(matIsBlur(null)).toBe(false);
	});
});
