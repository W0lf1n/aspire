import { describe, expect, it } from 'vitest';
import { DEFAULT_AT_MINUTES, fromClock, outOfReach, toClock } from './schedule';

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

describe('outOfReach', () => {
	/** Everything working: the case the switch is actually for. */
	const reachable = { browser: true, permission: 'default', sends: true } as const;

	it('offers the nudge when all three are in order', () => {
		expect(outOfReach(reachable)).toBeNull();
		expect(outOfReach({ ...reachable, permission: 'granted' })).toBeNull();
	});

	it('says so when the browser cannot do notifications', () => {
		expect(outOfReach({ ...reachable, browser: false })).toMatch(/prohlížeč/);
	});

	it('says so when the server has no keys to send with', () => {
		expect(outOfReach({ ...reachable, sends: false })).toMatch(/Server/);
	});

	it('says so when this origin has already been refused', () => {
		expect(outOfReach({ ...reachable, permission: 'denied' })).toMatch(/zakázaná/);
	});

	it('answers with the one that makes the others beside the point', () => {
		// No browser support at all: the server's keys cannot matter.
		expect(outOfReach({ browser: false, permission: 'denied', sends: false })).toMatch(/prohlížeč/);
		// A server with nothing to send: a permission the person could fix
		// would still get them nothing.
		expect(outOfReach({ browser: true, permission: 'denied', sends: false })).toMatch(/Server/);
	});

	it('does not hold a server nobody could reach against it', () => {
		// Offline is not „the server cannot send“; the screen says offline in
		// its own words, and the switch works again the moment there is signal.
		expect(outOfReach({ ...reachable, sends: null })).toBeNull();
		expect(outOfReach({ browser: true, permission: 'denied', sends: null })).toMatch(/zakázaná/);
	});
});
