import { describe, expect, it } from 'vitest';
import {
	DEFAULT_AT_MINUTES,
	fromClock,
	outOfReach,
	savedSentence,
	tidyTimes,
	toClock,
	withAnotherTime,
	withTimeMoved,
	withoutTime
} from './schedule';

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

describe('tidyTimes', () => {
	it('puts them in order and drops repeats, as the server does', () => {
		expect(tidyTimes([18 * 60, 7 * 60, 12 * 60, 7 * 60])).toEqual([7 * 60, 12 * 60, 18 * 60]);
	});

	it('pulls anything that is not a time of day back to one', () => {
		expect(tidyTimes([-10, 24 * 60 + 5])).toEqual([0, 24 * 60 - 1]);
	});
});

describe('withAnotherTime', () => {
	it('adds an hour after the last one, so the button always adds something', () => {
		expect(withAnotherTime([7 * 60])).toEqual([7 * 60, 8 * 60]);
		expect(withAnotherTime([7 * 60, 8 * 60])).toEqual([7 * 60, 8 * 60, 9 * 60]);
	});

	it('walks past an hour that is taken rather than adding a repeat', () => {
		// A repeat is silently dropped by `tidyTimes`, which would make the
		// button look broken.
		expect(withAnotherTime([22 * 60, 23 * 60])).toEqual([0, 22 * 60, 23 * 60]);
	});

	it('adds nothing once there are five', () => {
		const five = [0, 60, 120, 180, 240];
		expect(withAnotherTime(five)).toEqual(five);
	});
});

describe('withoutTime', () => {
	it('takes one away', () => {
		expect(withoutTime([7 * 60, 12 * 60], 7 * 60)).toEqual([12 * 60]);
	});

	it('never takes the last one: that is what Vypnuto is for', () => {
		expect(withoutTime([7 * 60], 7 * 60)).toEqual([7 * 60]);
	});

	it('leaves a time that is not there alone', () => {
		expect(withoutTime([7 * 60, 12 * 60], 9 * 60)).toEqual([7 * 60, 12 * 60]);
	});
});

describe('withTimeMoved', () => {
	it('moves one and leaves the rest, in order', () => {
		expect(withTimeMoved([7 * 60, 12 * 60], 7 * 60, 20 * 60)).toEqual([12 * 60, 20 * 60]);
	});

	it('folds a move onto an hour that is taken into one reminder', () => {
		expect(withTimeMoved([7 * 60, 12 * 60], 7 * 60, 12 * 60)).toEqual([12 * 60]);
	});
});

describe('savedSentence', () => {
	it('says the hour when there is one, because that is the fact wanted back', () => {
		expect(savedSentence([7 * 60])).toBe('Sen ti přijde v 07:00');
	});

	it('says how many when there are several, because five times is not a sentence', () => {
		expect(savedSentence([7 * 60, 12 * 60, 18 * 60])).toBe('Sen ti přijde 3× denně');
	});
});
