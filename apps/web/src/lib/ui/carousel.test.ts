import { describe, expect, it } from 'vitest';
import { offsetOf, slideAt } from './carousel';

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
