import { describe, expect, it } from 'vitest';
import {
	WHEEL_LINE,
	WHEEL_REACH,
	durationOf,
	easeOut,
	held,
	keyTarget,
	offsetOf,
	pageAt,
	stepTo,
	wheelStep,
	wheelTravel
} from './pager';

/** A phone-sized page, so the numbers read like the thing they describe. */
const H = 800;

describe('pageAt', () => {
	it('answers the page a scroll offset is showing', () => {
		expect(pageAt(0, H)).toBe(0);
		expect(pageAt(800, H)).toBe(1);
		expect(pageAt(2400, H)).toBe(3);
	});

	it('rounds to the nearer of the two it is between', () => {
		expect(pageAt(399, H)).toBe(0);
		expect(pageAt(401, H)).toBe(1);
	});

	it('survives a reel that has not been laid out yet', () => {
		expect(pageAt(0, 0)).toBe(0);
		expect(pageAt(120, Number.NaN)).toBe(0);
	});

	it('never answers with a page above the reel', () => {
		expect(pageAt(-40, H)).toBe(0);
	});
});

describe('stepTo', () => {
	it('moves one page', () => {
		expect(stepTo(3, 1, 10)).toBe(4);
		expect(stepTo(3, -1, 10)).toBe(2);
	});

	it('stops at both ends', () => {
		expect(stepTo(0, -1, 10)).toBe(0);
		expect(stepTo(9, 1, 10)).toBe(9);
	});

	it('has somewhere to be on a reel of one, and of none', () => {
		expect(stepTo(0, 1, 1)).toBe(0);
		expect(stepTo(0, 1, 0)).toBe(0);
	});
});

describe('held', () => {
	it('leaves a gesture alone inside its own page', () => {
		expect(held(1200, 1, H, 10)).toBe(1200);
		expect(held(800, 1, H, 10)).toBe(800);
	});

	it('holds a long slide to the next dream and no further', () => {
		// The finger carried five screens; the reel is allowed one.
		expect(held(5600, 1, H, 10)).toBe(1600);
	});

	it('holds a long slide back the same way', () => {
		expect(held(-2000, 3, H, 10)).toBe(1600);
	});

	it('does not let either end of the reel be passed', () => {
		expect(held(4000, 0, H, 3)).toBe(800);
		expect(held(4000, 2, H, 3)).toBe(1600);
		expect(held(-500, 0, H, 3)).toBe(0);
	});

	it('pins a reel of one dream where it is', () => {
		expect(held(900, 0, H, 1)).toBe(0);
	});
});

describe('wheelTravel', () => {
	it('takes pixels as they come', () => {
		expect(wheelTravel(120, 0, H)).toBe(120);
	});

	it('reads a browser that reports lines', () => {
		expect(wheelTravel(3, 1, H)).toBe(3 * WHEEL_LINE);
	});

	it('reads one that reports pages', () => {
		expect(wheelTravel(1, 2, H)).toBe(H);
	});
});

describe('wheelStep', () => {
	it('says nothing until the gesture has gone far enough', () => {
		expect(wheelStep(WHEEL_REACH - 1)).toBe(0);
		expect(wheelStep(-(WHEEL_REACH - 1))).toBe(0);
		expect(wheelStep(0)).toBe(0);
	});

	it('asks for one page, whatever the gesture is worth', () => {
		expect(wheelStep(WHEEL_REACH)).toBe(1);
		expect(wheelStep(4000)).toBe(1);
		expect(wheelStep(-4000)).toBe(-1);
	});
});

describe('keyTarget', () => {
	it('moves one dream on the arrow and page keys', () => {
		expect(keyTarget('ArrowDown', 2, 10)).toBe(3);
		expect(keyTarget('PageDown', 2, 10)).toBe(3);
		expect(keyTarget('ArrowUp', 2, 10)).toBe(1);
		expect(keyTarget('PageUp', 2, 10)).toBe(1);
	});

	it('goes to the ends', () => {
		expect(keyTarget('Home', 5, 10)).toBe(0);
		expect(keyTarget('End', 5, 10)).toBe(9);
		expect(keyTarget('End', 0, 0)).toBe(0);
	});

	it('leaves every other key alone', () => {
		expect(keyTarget(' ', 2, 10)).toBeNull();
		expect(keyTarget('Enter', 2, 10)).toBeNull();
		expect(keyTarget('a', 2, 10)).toBeNull();
	});
});

describe('easeOut', () => {
	it('starts where it starts and ends where it ends', () => {
		expect(easeOut(0)).toBe(0);
		expect(easeOut(1)).toBe(1);
	});

	it('spends most of the distance early, as the token does', () => {
		expect(easeOut(0.3)).toBeGreaterThan(0.85);
		expect(easeOut(0.5)).toBeGreaterThan(0.96);
	});

	it('never goes backwards', () => {
		let last = -1;
		for (let t = 0; t <= 1; t += 0.05) {
			const now = easeOut(t);
			expect(now).toBeGreaterThanOrEqual(last);
			last = now;
		}
	});
});

describe('durationOf', () => {
	it('reads the token the stylesheet answers with', () => {
		expect(durationOf('360ms')).toBe(360);
		expect(durationOf(' 220ms ')).toBe(220);
		expect(durationOf('0.4s')).toBe(400);
	});

	it('refuses anything that is not a duration, so the fallback stands', () => {
		expect(durationOf('')).toBeNull();
		expect(durationOf('0ms')).toBeNull();
		expect(durationOf('fast')).toBeNull();
		expect(durationOf('360')).toBeNull();
	});
});

describe('offsetOf', () => {
	it('is where a page begins', () => {
		expect(offsetOf(0, H)).toBe(0);
		expect(offsetOf(4, H)).toBe(3200);
	});

	it('keeps a fractional page height, so the snap positions still line up', () => {
		expect(offsetOf(3, 745.6)).toBeCloseTo(2236.8, 5);
	});
});
