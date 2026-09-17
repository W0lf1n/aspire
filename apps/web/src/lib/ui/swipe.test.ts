import { describe, expect, it } from 'vitest';
import { gestureOf, slidTo, staysOpen } from './swipe';

describe('gestureOf', () => {
	it('is nothing yet inside the slop', () => {
		expect(gestureOf(5, -3)).toBeNull();
		expect(gestureOf(0, 0)).toBeNull();
	});

	it('is a swipe when the finger has clearly gone across', () => {
		expect(gestureOf(-14, 4)).toBe('swipe');
		expect(gestureOf(12, -2)).toBe('swipe');
	});

	it('is the list being scrolled when it has gone down as much as across', () => {
		expect(gestureOf(-9, 20)).toBe('scroll');
		expect(gestureOf(10, 10)).toBe('scroll');
	});
});

describe('slidTo', () => {
	const TRAY = 192;

	it('follows the finger between closed and open', () => {
		expect(slidTo(0, -80, TRAY)).toBe(-80);
		expect(slidTo(-TRAY, 60, TRAY)).toBe(-132);
	});

	it('gives a quarter past the tray', () => {
		expect(slidTo(0, -TRAY - 40, TRAY)).toBe(-TRAY - 10);
	});

	it('gives a quarter the wrong way from closed', () => {
		expect(slidTo(0, 40, TRAY)).toBe(10);
	});
});

describe('staysOpen', () => {
	const TRAY = 192;

	it('opens past halfway and closes short of it', () => {
		expect(staysOpen(-100, TRAY, 0)).toBe(true);
		expect(staysOpen(-90, TRAY, 0)).toBe(false);
	});

	it('lets a flick decide by its direction, wherever the row is', () => {
		expect(staysOpen(-30, TRAY, -0.8)).toBe(true);
		expect(staysOpen(-170, TRAY, 0.8)).toBe(false);
	});

	it('has nothing to open when there is no tray', () => {
		expect(staysOpen(-50, 0, -1)).toBe(false);
	});
});
