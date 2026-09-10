import { describe, expect, it } from 'vitest';
import { MAX_EDGE, fitWithin } from './downscale';

describe('fitWithin', () => {
	it('shrinks the longest edge to the limit and keeps the ratio', () => {
		expect(fitWithin(4000, 3000, MAX_EDGE)).toEqual({ width: 2048, height: 1536 });
		expect(fitWithin(3000, 4000, MAX_EDGE)).toEqual({ width: 1536, height: 2048 });
	});

	it('never grows a small picture', () => {
		expect(fitWithin(800, 600, MAX_EDGE)).toEqual({ width: 800, height: 600 });
		expect(fitWithin(2048, 100, MAX_EDGE)).toEqual({ width: 2048, height: 100 });
	});

	it('keeps a sliver at least one pixel wide', () => {
		expect(fitWithin(100000, 10, 2048)).toEqual({ width: 2048, height: 1 });
	});
});
