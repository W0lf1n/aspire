import { describe, expect, it } from 'vitest';
import { AT_LEAST, dismisses, pulledBy } from './pull';

describe('pulledBy', () => {
	it('follows the finger on the way down', () => {
		expect(pulledBy(0)).toBe(0);
		expect(pulledBy(140)).toBe(140);
	});

	it('gives a sixth on the way up', () => {
		expect(pulledBy(-60)).toBe(-10);
	});

	it('stays put for a number that is not one', () => {
		expect(pulledBy(Number.NaN)).toBe(0);
	});
});

describe('dismisses', () => {
	it('lets go of a sheet pulled past a third of its height', () => {
		expect(dismisses(181, 900, 600)).toBe(true);
		expect(dismisses(179, 900, 600)).toBe(false);
	});

	it('asks a short sheet for the same 72 px as any other', () => {
		expect(dismisses(AT_LEAST, 900, 120)).toBe(false);
		expect(dismisses(AT_LEAST + 1, 900, 120)).toBe(true);
	});

	it('takes a flick, however short', () => {
		// 40 px in 60 ms is 0.67 px/ms.
		expect(dismisses(40, 60, 600)).toBe(true);
	});

	it('does not take a tap on the handle for a flick', () => {
		expect(dismisses(8, 4, 600)).toBe(false);
	});

	it('never lets go of a sheet pushed up', () => {
		expect(dismisses(-200, 50, 600)).toBe(false);
		expect(dismisses(0, 50, 600)).toBe(false);
	});

	it('survives a gesture with no time in it', () => {
		expect(dismisses(30, 0, 600)).toBe(true);
	});
});
