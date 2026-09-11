import { describe, expect, it } from 'vitest';
import {
	WINDOW_KEEP,
	capFor,
	effectivePolicy,
	formatUsage,
	wantsWholeBoard,
	type OfflinePolicy
} from './policy';

describe('effectivePolicy', () => {
	it('keeps what the person chose, whatever the browser can see', () => {
		const every: OfflinePolicy[] = ['window', 'wifi', 'all'];
		for (const chosen of every) {
			expect(effectivePolicy(chosen, true)).toBe(chosen);
			expect(effectivePolicy(chosen, false)).toBe(chosen);
		}
	});

	it('defaults to the whole board on wifi where the connection can be seen', () => {
		expect(effectivePolicy(null, true)).toBe('wifi');
	});

	it('defaults to the window where it cannot, rather than guessing with data', () => {
		// Safari has no Network Information API, so this is every iPhone.
		expect(effectivePolicy(null, false)).toBe('window');
	});
});

describe('wantsWholeBoard', () => {
	it('never fetches the board beyond the window when that is the choice', () => {
		expect(wantsWholeBoard('window', false)).toBe(false);
		expect(wantsWholeBoard('window', true)).toBe(false);
		expect(wantsWholeBoard('window', null)).toBe(false);
	});

	it('always fetches it when that is the choice, metered or not', () => {
		expect(wantsWholeBoard('all', true)).toBe(true);
		expect(wantsWholeBoard('all', null)).toBe(true);
	});

	it('fetches it on wifi and not on mobile data', () => {
		expect(wantsWholeBoard('wifi', false)).toBe(true);
		expect(wantsWholeBoard('wifi', true)).toBe(false);
	});

	it('treats a connection it cannot read as one to be careful with', () => {
		// The wrong guess one way is a few megabytes of cache; the other way
		// is somebody's data plan.
		expect(wantsWholeBoard('wifi', null)).toBe(false);
	});
});

describe('capFor', () => {
	it('bounds the window, because a shuffled reel would otherwise reach the whole board', () => {
		expect(capFor('window')).toBe(WINDOW_KEEP);
	});

	it('lets the board be its own ceiling when the board is the promise', () => {
		expect(capFor('wifi')).toBeNull();
		expect(capFor('all')).toBeNull();
	});
});

describe('formatUsage', () => {
	it('says megabytes with a Czech comma', () => {
		expect(formatUsage(12.44 * 1024 * 1024)).toBe('12,4 MB');
	});

	it('rounds to one place, so the number does not move while you read it', () => {
		expect(formatUsage(1024 * 1024)).toBe('1 MB');
	});

	it('says there is nothing rather than 0 MB', () => {
		expect(formatUsage(0)).toBe('nic');
		expect(formatUsage(2048)).toBe('nic');
	});
});
