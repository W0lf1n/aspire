import { describe, expect, it } from 'vitest';
import { canGo, offsetOf, roomAcross, slideAt, stepOffset } from './carousel';

describe('slideAt', () => {
	it('is the slide the row has scrolled to', () => {
		expect(slideAt(0, 360, 6)).toBe(0);
		expect(slideAt(720, 360, 6)).toBe(2);
	});

	it('is the nearest slide while a swipe is still between two', () => {
		expect(slideAt(170, 360, 6)).toBe(0);
		expect(slideAt(190, 360, 6)).toBe(1);
	});

	it('never names a slide there is not', () => {
		expect(slideAt(5000, 360, 3)).toBe(2);
		expect(slideAt(100, 0, 3)).toBe(0);
		expect(slideAt(100, 360, 0)).toBe(0);
	});

	it('reads a right-to-left row, which scrolls the other way', () => {
		expect(slideAt(-720, 360, 6)).toBe(2);
	});
});

describe('offsetOf', () => {
	it('is a whole number of slides across', () => {
		expect(offsetOf(3, 360, 6)).toBe(1080);
	});

	it('stays inside the row', () => {
		expect(offsetOf(9, 360, 6)).toBe(1800);
		expect(offsetOf(-1, 360, 6)).toBe(0);
		expect(offsetOf(2, 360, 0)).toBe(0);
	});
});

describe('roomAcross (D87)', () => {
	it('is room on and none back on the first slide', () => {
		expect(roomAcross(0, 1500, 375)).toEqual({ back: false, on: true });
	});

	it('is room both ways in the middle', () => {
		expect(roomAcross(750, 1500, 375)).toEqual({ back: true, on: true });
	});

	it('is none on at the last slide, even a fraction short of the end', () => {
		expect(roomAcross(1125, 1500, 375)).toEqual({ back: true, on: false });
		expect(roomAcross(1124.5, 1500, 375)).toEqual({ back: true, on: false });
	});

	it('is none either way for a row one slide long', () => {
		expect(roomAcross(0, 375, 375)).toEqual({ back: false, on: false });
	});
});

describe('canGo', () => {
	it('gives a swipe to the row while it can still move that way', () => {
		expect(canGo({ back: false, on: true }, 1)).toBe(true);
		expect(canGo({ back: true, on: false }, -1)).toBe(true);
	});

	it('gives it to the reel at the end the swipe went past, or with no row', () => {
		expect(canGo({ back: false, on: true }, -1)).toBe(false);
		expect(canGo({ back: true, on: false }, 1)).toBe(false);
		expect(canGo(null, 1)).toBe(false);
	});
});

describe('stepOffset', () => {
	it('is the next slide on and the one before back', () => {
		expect(stepOffset(375, 375, 1500, 1)).toBe(750);
		expect(stepOffset(375, 375, 1500, -1)).toBe(0);
	});

	it('stays inside the row', () => {
		expect(stepOffset(1125, 375, 1500, 1)).toBe(1125);
	});
});
