import { describe, expect, it } from 'vitest';
import {
	CENTRED,
	MAX_ZOOM,
	MIN_ZOOM,
	cellFocal,
	dragged,
	fitted,
	isCentred,
	overflow,
	room,
	samePlace,
	sane,
	spread,
	startsWhole,
	zoomed
} from './focal';

/** A phone screen, and a photograph wider than it is tall. */
const FRAME = { width: 400, height: 800 };
const WIDE = { width: 2000, height: 1000 };

describe('overflow', () => {
	it('is what hangs over the frame once the picture covers it', () => {
		// Cover scales 2000x1000 to 1600x800, so 1200 hangs over sideways and
		// nothing hangs over downwards.
		expect(overflow(FRAME, WIDE, 1)).toEqual({ width: 1200, height: 0 });
	});

	it('gives the flush axis something to hang over once there is zoom', () => {
		// The whole reason zoom is worth having: at 1 exactly one axis can be
		// moved along, and at 2 both can.
		const room = overflow(FRAME, WIDE, 2);

		expect(room.width).toBe(2800);
		expect(room.height).toBe(800);
	});

	it('is nothing for a picture with no size', () => {
		expect(overflow(FRAME, { width: 0, height: 0 }, 1)).toEqual({ width: 0, height: 0 });
	});
});

describe('dragged', () => {
	it('brings the left of the picture into view when it is pulled right', () => {
		// The number is where the frame is looking, not where the picture is.
		const moved = dragged(CENTRED, 120, 0, FRAME, WIDE);

		expect(moved.x).toBeCloseTo(0.4, 5);
		expect(moved.y).toBe(0.5);
	});

	it('stops at the edges of the picture', () => {
		expect(dragged(CENTRED, -100000, 0, FRAME, WIDE).x).toBe(1);
		expect(dragged(CENTRED, 100000, 0, FRAME, WIDE).x).toBe(0);
	});

	it('does not move along an axis there is nothing more of to see', () => {
		// Dividing by that zero is how a focal point ends up at infinity.
		expect(dragged(CENTRED, 0, 300, FRAME, WIDE).y).toBe(0.5);
	});

	it('moves down the picture once zoom has given it room', () => {
		const zoomedIn = { ...CENTRED, zoom: 2 };

		expect(dragged(zoomedIn, 0, 200, FRAME, WIDE).y).toBeCloseTo(0.25, 5);
	});

	it('keeps the zoom it was given', () => {
		expect(dragged({ ...CENTRED, zoom: 2.5 }, 10, 10, FRAME, WIDE).zoom).toBe(2.5);
	});
});

describe('zoomed', () => {
	it('multiplies, and holds the point it was looking at', () => {
		const closer = zoomed({ ...CENTRED, x: 0.2, y: 0.8 }, 1.5);

		expect(closer.zoom).toBe(1.5);
		expect(closer.x).toBe(0.2);
		expect(closer.y).toBe(0.8);
	});

	it('goes no further out than all of it and no closer than three', () => {
		expect(zoomed(CENTRED, 0.1).zoom).toBe(MIN_ZOOM);
		expect(zoomed({ ...CENTRED, zoom: 2 }, 10).zoom).toBe(MAX_ZOOM);
	});

	it('ignores a factor that is not one', () => {
		expect(zoomed({ ...CENTRED, zoom: 2 }, 0).zoom).toBe(2);
		expect(zoomed({ ...CENTRED, zoom: 2 }, Number.NaN).zoom).toBe(2);
	});
});

describe('sane', () => {
	it('takes what arrived and makes a crop that works', () => {
		expect(sane({ x: -3, y: 9, zoom: 100 })).toEqual({ ...CENTRED, x: 0, y: 1, zoom: MAX_ZOOM });
	});

	it('is the middle and all of it when there is nothing to go on', () => {
		expect(sane(null)).toEqual(CENTRED);
		expect(sane({})).toEqual(CENTRED);
		expect(sane({ x: Number.NaN, y: undefined, zoom: Number.POSITIVE_INFINITY })).toEqual(CENTRED);
	});
});

describe('isCentred', () => {
	it('knows the crop a photograph has when nobody has touched it', () => {
		expect(isCentred(CENTRED)).toBe(true);
		expect(isCentred({ ...CENTRED, zoom: 1.2 })).toBe(false);
		expect(isCentred({ ...CENTRED, x: 0.51 })).toBe(false);
		expect(isCentred({ ...CENTRED, fit: 'whole' })).toBe(false);
		// The mat is kept while a photograph fills, and changes nothing there.
		expect(isCentred({ ...CENTRED, mat: 'dusk' })).toBe(true);
	});
});

describe('spread', () => {
	it('is the distance between two fingers', () => {
		expect(spread({ x: 0, y: 0 }, { x: 3, y: 4 })).toBe(5);
		expect(spread({ x: 10, y: 10 }, { x: 10, y: 10 })).toBe(0);
	});
});

describe('a photograph shown whole (D81)', () => {
	const WHOLE = { ...CENTRED, fit: 'whole' as const };

	it('has room to spare down the frame, and none across it', () => {
		// Contain scales 2000x1000 to 400x200: flush sideways, 600 of mat down.
		expect(room(FRAME, WIDE, 1, 'whole')).toEqual({ width: 0, height: 600 });
	});

	it('is the overhang with a minus in front when it fills', () => {
		expect(room(FRAME, WIDE, 1, 'fill')).toEqual({ width: -1200, height: 0 });
	});

	it('moves with the finger, because it floats rather than hangs over', () => {
		// 150 px down is a quarter of the 600 px of room.
		const moved = dragged(WHOLE, 0, 150, FRAME, WIDE);

		expect(moved.y).toBeCloseTo(0.75);
		expect(moved.x).toBe(0.5);
	});

	it('runs against the finger again once the zoom has made it hang over', () => {
		// At 3× the picture is 1200x600: it overhangs sideways by 800 and still
		// floats in 200 px of room down the frame.
		const close = { ...WHOLE, zoom: 3 };
		const moved = dragged(close, 200, 50, FRAME, WIDE);

		expect(moved.x).toBeCloseTo(0.25);
		expect(moved.y).toBeCloseTo(0.75);
	});

	it('starts again from the middle when it is turned over', () => {
		const chosen = { ...CENTRED, x: 0.1, y: 0.9, zoom: 2.4, mat: 'umber' as const };

		expect(fitted(chosen, 'whole')).toEqual({ ...CENTRED, fit: 'whole', mat: 'umber' });
		expect(fitted(chosen, 'fill')).toBe(chosen);
	});

	it('keeps the fit and the mat through a pinch', () => {
		expect(zoomed({ ...WHOLE, mat: 'dusk' }, 2)).toEqual({ ...WHOLE, mat: 'dusk', zoom: 2 });
	});

	it('reads a fit and a mat that arrived from anywhere', () => {
		expect(sane({ fit: 'whole', mat: 'ember' })).toEqual({
			...CENTRED,
			fit: 'whole',
			mat: 'ember'
		});
		expect(sane({ fit: 'sideways' as never, mat: '#ff00aa' as never })).toEqual(CENTRED);
	});
});

describe('startsWhole', () => {
	it('is a picture wider than it is tall', () => {
		expect(startsWhole({ width: 2048, height: 1365 })).toBe(true);
		expect(startsWhole({ width: 1365, height: 2048 })).toBe(false);
	});

	it('leaves a square, and a picture with no size, filling', () => {
		expect(startsWhole({ width: 1000, height: 1000 })).toBe(false);
		expect(startsWhole({ width: 0, height: 0 })).toBe(false);
	});
});

describe('cellFocal (D85)', () => {
	const saved = { focusX: 0.2, focusY: 0.7, zoom: 1.6, mat: 'dusk' as const };

	it('is the crop as saved for a photograph that fills', () => {
		expect(cellFocal({ ...saved, fit: 'fill' })).toEqual({
			x: 0.2,
			y: 0.7,
			zoom: 1.6,
			fit: 'fill',
			mat: 'dusk'
		});
	});

	it('is the point alone, filling, for a photograph shown whole — as the cell shows it', () => {
		expect(cellFocal({ ...saved, fit: 'whole' })).toEqual({
			x: 0.2,
			y: 0.7,
			zoom: MIN_ZOOM,
			fit: 'fill',
			mat: 'dusk'
		});
	});

	it('is the middle for no photograph', () => {
		expect(cellFocal(null)).toEqual(CENTRED);
	});
});

describe('samePlace', () => {
	it('compares where the picture is, and not what it stands on', () => {
		expect(samePlace(CENTRED, { ...CENTRED, mat: 'ember' })).toBe(true);
		expect(samePlace(CENTRED, { ...CENTRED, x: 0.4 })).toBe(false);
		expect(samePlace(CENTRED, { ...CENTRED, zoom: 1.2 })).toBe(false);
		expect(samePlace(CENTRED, { ...CENTRED, fit: 'whole' })).toBe(false);
	});
});
