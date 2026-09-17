import { describe, expect, it } from 'vitest';
import { EDGE, EDGE_SPEED, edgeScroll, heldTo, landsOn, stepsAside } from './reorder';

const ROW = 64;

describe('landsOn', () => {
	it('stays on its own line until half a row has gone by', () => {
		expect(landsOn(2, 31, ROW, 10)).toBe(2);
		expect(landsOn(2, 33, ROW, 10)).toBe(3);
		expect(landsOn(2, -33, ROW, 10)).toBe(1);
	});

	it('counts whole rows past that', () => {
		expect(landsOn(2, ROW * 4, ROW, 10)).toBe(6);
		expect(landsOn(8, -ROW * 5, ROW, 10)).toBe(3);
	});

	it('stops at both ends of the list', () => {
		expect(landsOn(2, -9999, ROW, 10)).toBe(0);
		expect(landsOn(2, 9999, ROW, 10)).toBe(9);
	});

	it('goes nowhere in a list it cannot measure', () => {
		expect(landsOn(2, 200, 0, 10)).toBe(2);
		expect(landsOn(0, 200, ROW, 0)).toBe(0);
	});
});

describe('stepsAside', () => {
	it('moves the rows a dream is dragged down past up by one', () => {
		// From line 1 to line 3: lines 2 and 3 step up, the rest stay.
		expect([0, 1, 2, 3, 4].map((i) => stepsAside(i, 1, 3, ROW))).toEqual([0, 0, -ROW, -ROW, 0]);
	});

	it('moves the rows a dream is dragged up past down by one', () => {
		expect([0, 1, 2, 3, 4].map((i) => stepsAside(i, 3, 0, ROW))).toEqual([ROW, ROW, ROW, 0, 0]);
	});

	it('moves nothing while the dream is still over its own line', () => {
		expect([0, 1, 2].map((i) => stepsAside(i, 1, 1, ROW))).toEqual([0, 0, 0]);
	});
});

describe('heldTo', () => {
	it('lets the row go as far as the ends of the list and half a row more', () => {
		expect(heldTo(-9999, 2, ROW, 10)).toBe(-2 * ROW - ROW / 2);
		expect(heldTo(9999, 2, ROW, 10)).toBe(7 * ROW + ROW / 2);
	});

	it('leaves a drag inside the list alone', () => {
		expect(heldTo(100, 2, ROW, 10)).toBe(100);
	});
});

describe('edgeScroll', () => {
	const TOP = 100;
	const BOTTOM = 700;

	it('does nothing in the middle of the list', () => {
		expect(edgeScroll(400, TOP, BOTTOM)).toBe(0);
		expect(edgeScroll(TOP + EDGE, TOP, BOTTOM)).toBe(0);
	});

	it('scrolls up near the top and down near the bottom, faster the nearer', () => {
		expect(edgeScroll(TOP + EDGE / 2, TOP, BOTTOM)).toBe(-EDGE_SPEED / 2);
		expect(edgeScroll(BOTTOM - EDGE / 2, TOP, BOTTOM)).toBe(EDGE_SPEED / 2);
	});

	it('tops out at the edge and beyond it', () => {
		expect(edgeScroll(TOP - 50, TOP, BOTTOM)).toBe(-EDGE_SPEED);
		expect(edgeScroll(BOTTOM + 50, TOP, BOTTOM)).toBe(EDGE_SPEED);
	});
});
