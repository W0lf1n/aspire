import { describe, expect, it } from 'vitest';
import type { Dream, DreamStatus } from '@aspire/contracts';
import { FOCUS_MAX } from './rules';
import { focusDreams, focusFull, focusFullSentence, readReel, saveReel } from './focus';

function dream(
	id: string,
	focusRank: number | null,
	status: DreamStatus = 'dreaming',
	sortOrder = 0
): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status,
		category: null,
		sortOrder,
		focusRank,
		targetYear: null,
		layout: 0,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-12T08:00:00.000Z',
		updatedAt: '2026-09-12T08:00:00.000Z',
		images: []
	};
}

describe('focusDreams', () => {
	it('is the dreams with a rank, in the order of their ranks', () => {
		const board = [dream('c', 3), dream('a', 1), dream('b', 2), dream('loose', null)];

		expect(focusDreams(board).map((d) => d.id)).toEqual(['a', 'b', 'c']);
	});

	it('is empty when nothing has been put on it', () => {
		expect(focusDreams([dream('a', null), dream('b', null)])).toEqual([]);
	});

	it('drops a dream that has been achieved', () => {
		// It leaves Teď the way it leaves the reel. The server clears the rank;
		// this is the same rule on a board read from the cache.
		const board = [dream('done', 1, 'achieved'), dream('going', 2, 'in-progress')];

		expect(focusDreams(board).map((d) => d.id)).toEqual(['going']);
	});

	it('breaks a tie on the board order, so every device agrees', () => {
		const board = [dream('second', 1, 'dreaming', 9), dream('first', 1, 'dreaming', 4)];

		expect(focusDreams(board).map((d) => d.id)).toEqual(['first', 'second']);
	});

	it('does not care that the ranks have gaps in them', () => {
		// A rank is an ordering key, not a position: one taken off the middle
		// leaves a gap and the screen still numbers 1, 2, 3.
		const board = [dream('a', 1), dream('c', 7), dream('b', 4)];

		expect(focusDreams(board).map((d) => d.id)).toEqual(['a', 'b', 'c']);
	});
});

describe('focusFull', () => {
	it('is true at ten and false below it', () => {
		const ten = Array.from({ length: FOCUS_MAX }, (_, i) => dream(`d${i}`, i + 1));

		expect(focusFull(ten)).toBe(true);
		expect(focusFull(ten.slice(1))).toBe(false);
	});

	it('does not count a dream that has left Teď by being achieved', () => {
		const ten = Array.from({ length: FOCUS_MAX }, (_, i) => dream(`d${i}`, i + 1));
		ten[0] = dream('d0', 1, 'achieved');

		expect(focusFull(ten)).toBe(false);
	});

	it('says the same sentence the server would', () => {
		expect(focusFullSentence()).toBe('Na teď máš už 10 snů. Některý nejdřív odeber.');
	});
});

describe('the remembered reel', () => {
	// The tests run in node, where there is no localStorage — as
	// `offline/policy.test.ts` leaves its own two alone for the same reason.
	// What is checked here is that a device with nowhere to remember still
	// opens on a reel rather than on nothing.
	it('is everything where there is nothing to remember it in', () => {
		expect(readReel()).toBe('all');
		expect(() => saveReel('focus')).not.toThrow();
	});
});
