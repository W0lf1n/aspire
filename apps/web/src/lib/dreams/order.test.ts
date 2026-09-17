import { describe, expect, it } from 'vitest';
import type { Dream } from '@aspire/contracts';
import { parsePlace, placed, serverPlace } from './order';

function dream(id: string, sortOrder: number): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder,
		focusRank: null,
		targetYear: null,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
		updatedAt: '2026-09-01T00:00:00Z',
		images: []
	};
}

const BOARD = [dream('one', 0), dream('two', 1), dream('three', 2), dream('four', 3)];
const ids = (rows: Dream[]) => rows.map((row) => row.id);

describe('placed', () => {
	it('makes the third line the first, and the two it passed move down one', () => {
		expect(ids(placed(BOARD, 'three', 1))).toEqual(['three', 'one', 'two', 'four']);
	});

	it('moves a dream down past its neighbours, which move up one', () => {
		expect(ids(placed(BOARD, 'one', 3))).toEqual(['two', 'three', 'one', 'four']);
	});

	it('treats a place past either end as the end', () => {
		expect(ids(placed(BOARD, 'two', 99))).toEqual(['one', 'three', 'four', 'two']);
		expect(ids(placed(BOARD, 'two', -5))).toEqual(['two', 'one', 'three', 'four']);
	});

	it('counts the board off from nought, whatever the keys were', () => {
		// A new dream goes in front with a key below zero; a move tidies it.
		const board = [dream('new', -2), dream('a', 0), dream('b', 7)];

		expect(placed(board, 'b', 2).map((row) => [row.id, row.sortOrder])).toEqual([
			['new', 0],
			['b', 1],
			['a', 2]
		]);
	});

	it('reads the board in list order first, not in the order it was handed', () => {
		const shuffled = [BOARD[2], BOARD[0], BOARD[3], BOARD[1]];

		expect(ids(placed(shuffled, 'four', 1))).toEqual(['four', 'one', 'two', 'three']);
	});

	it('keeps the very object of a dream whose key did not change', () => {
		const after = placed(BOARD, 'two', 1);

		expect(after[3]).toBe(BOARD[3]);
		expect(after[0]).not.toBe(BOARD[1]);
	});

	it('leaves the order alone for a dream that is not there', () => {
		expect(ids(placed(BOARD, 'nobody', 1))).toEqual(ids(BOARD));
	});
});

describe('serverPlace', () => {
	it('is the same number when nothing is hidden', () => {
		expect(serverPlace(BOARD, BOARD, 'four', 2)).toBe(2);
		expect(serverPlace(BOARD, BOARD, 'one', 4)).toBe(4);
	});

	it('counts a dream inside its undo window, which the server still has', () => {
		// „two“ is hidden: the screen shows one, three, four.
		const seen = [BOARD[0], BOARD[2], BOARD[3]];

		// Four to the screen's second line is above three — the server's third.
		expect(serverPlace(seen, BOARD, 'four', 2)).toBe(3);
		expect(ids(placed(BOARD, 'four', 3))).toEqual(['one', 'two', 'four', 'three']);
	});

	it('is the end of the list on the server when it is dropped at the end on the screen', () => {
		const seen = [BOARD[0], BOARD[1], BOARD[2]];

		expect(serverPlace(seen, BOARD, 'one', 3)).toBe(4);
	});
});

describe('parsePlace', () => {
	it('reads a number, however it was typed', () => {
		expect(parsePlace('1', 3, 10)).toBe(1);
		expect(parsePlace('  7 ', 3, 10)).toBe(7);
		expect(parsePlace('2.', 3, 10)).toBe(2);
	});

	it('takes a number past the end for the end', () => {
		expect(parsePlace('250', 3, 10)).toBe(10);
	});

	it('is nothing when it is the line the dream is already on', () => {
		expect(parsePlace('3', 3, 10)).toBeNull();
		expect(parsePlace('99', 10, 10)).toBeNull();
	});

	it('is nothing when it is not a place', () => {
		for (const typed of ['', '  ', '0', '-2', 'tři', '1.5', '1e3']) {
			expect(parsePlace(typed, 3, 10)).toBeNull();
		}
	});
});
