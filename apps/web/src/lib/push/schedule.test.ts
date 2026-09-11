import { describe, expect, it } from 'vitest';
import { DEFAULT_AT_MINUTES, fromClock, toClock } from './schedule';

describe('toClock', () => {
	it('is the time of day a field can show', () => {
		expect(toClock(7 * 60)).toBe('07:00');
		expect(toClock(0)).toBe('00:00');
		expect(toClock(23 * 60 + 59)).toBe('23:59');
		expect(toClock(6 * 60 + 5)).toBe('06:05');
	});

	it('holds a number that is not a time of day inside one', () => {
		expect(toClock(-30)).toBe('00:00');
		expect(toClock(48 * 60)).toBe('23:59');
		expect(toClock(Number.NaN)).toBe('07:00');
	});
});

describe('fromClock', () => {
	it('reads a field back', () => {
		expect(fromClock('07:00')).toBe(7 * 60);
		expect(fromClock('00:00')).toBe(0);
		expect(fromClock('23:59')).toBe(23 * 60 + 59);
		expect(fromClock(' 6:05 ')).toBe(6 * 60 + 5);
	});

	it('falls back to seven rather than refusing to save', () => {
		expect(fromClock('')).toBe(DEFAULT_AT_MINUTES);
		expect(fromClock('ráno')).toBe(DEFAULT_AT_MINUTES);
		expect(fromClock('24:00')).toBe(DEFAULT_AT_MINUTES);
		expect(fromClock('12:60')).toBe(DEFAULT_AT_MINUTES);
	});

	it('round-trips every minute of the day', () => {
		for (let minutes = 0; minutes < 24 * 60; minutes++) {
			expect(fromClock(toClock(minutes))).toBe(minutes);
		}
	});
});
