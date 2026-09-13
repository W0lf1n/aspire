import { describe, expect, it } from 'vitest';
import { REACH, stepWithin, swipeOf } from './sideways';

describe('swipeOf', () => {
	it('is nothing for a finger that barely moved', () => {
		expect(swipeOf(0, 0)).toBe(0);
		expect(swipeOf(REACH - 1, 0)).toBe(0);
		expect(swipeOf(-(REACH - 1), 0)).toBe(0);
	});

	it('carries the next reel in when the finger goes right to left', () => {
		expect(swipeOf(-REACH, 0)).toBe(1);
		expect(swipeOf(-200, 10)).toBe(1);
	});

	it('goes back when the finger goes left to right', () => {
		expect(swipeOf(REACH, 0)).toBe(-1);
		expect(swipeOf(200, -10)).toBe(-1);
	});

	it('leaves a swipe up the reel alone, however far across it wandered', () => {
		// The doubt is always the pager's: this is a dream, not another reel.
		expect(swipeOf(80, 300)).toBe(0);
		expect(swipeOf(-80, -300)).toBe(0);
		// Further across than down is not enough on its own: 100 against 80 is
		// a drag at 39 degrees, which is an unsteady swipe up the reel.
		expect(swipeOf(100, 80)).toBe(0);
	});

	it('takes one that is clearly further across than down', () => {
		// 100 against 70 is 35 degrees, and past the bias.
		expect(swipeOf(-100, 70)).toBe(1);
		expect(swipeOf(100, 70)).toBe(-1);
	});

	it('is nothing for numbers that are not numbers', () => {
		expect(swipeOf(Number.NaN, 0)).toBe(0);
		expect(swipeOf(-200, Number.POSITIVE_INFINITY)).toBe(0);
	});

	it('takes its reach and bias from the caller when asked', () => {
		expect(swipeOf(-20, 0, 10, 1)).toBe(1);
		expect(swipeOf(-20, 0, 30, 1)).toBe(0);
	});
});

describe('stepWithin', () => {
	const reels = ['all', 'focus'] as const;

	it('steps on and back through the list', () => {
		expect(stepWithin(reels, 'all', 1)).toBe('focus');
		expect(stepWithin(reels, 'focus', -1)).toBe('all');
	});

	it('stops at either end rather than wrapping round', () => {
		// A reel that came back to the first one from the last would make a
		// swipe unguessable: the same gesture would go two ways.
		expect(stepWithin(reels, 'focus', 1)).toBe('focus');
		expect(stepWithin(reels, 'all', -1)).toBe('all');
	});

	it('leaves something that is not in the list where it is', () => {
		expect(stepWithin(reels, 'nowhere' as never, 1)).toBe('nowhere');
	});
});
