import { describe, expect, it } from 'vitest';
import type { Dream } from '@aspire/contracts';
import { boardStats, byStatus, changedAt } from './stats';

function dream(id: string, over: Partial<Dream> = {}): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder: 0,
		focusRank: null,
		targetYear: null,
		layout: 0,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
		updatedAt: '2026-09-01T00:00:00Z',
		images: [],
		...over
	};
}

describe('boardStats', () => {
	it('counts the board and each state on it', () => {
		const stats = boardStats([
			dream('a'),
			dream('b', { status: 'in-progress' }),
			dream('c', { status: 'in-progress' }),
			dream('d', { status: 'achieved' })
		]);

		expect(stats.total).toBe(4);
		expect(stats.by).toEqual({ dreaming: 1, 'in-progress': 2, achieved: 1 });
	});

	it('says when any dream was last changed', () => {
		const stats = boardStats([
			dream('a', { updatedAt: '2026-09-03T10:00:00Z' }),
			dream('b', { updatedAt: '2026-09-14T18:30:00Z' }),
			dream('c', { updatedAt: '2026-09-05T08:00:00Z' })
		]);

		expect(stats.lastChange).toBe('2026-09-14T18:30:00Z');
	});

	it('has nothing to say about an empty board', () => {
		expect(boardStats([])).toEqual({
			total: 0,
			by: { dreaming: 0, 'in-progress': 0, achieved: 0 },
			lastChange: null
		});
	});
});

describe('changedAt', () => {
	it('falls back to the day it was written on a board remembered from before the field', () => {
		const old = { createdAt: '2026-09-01T00:00:00Z' } as Dream;
		expect(changedAt(old)).toBe('2026-09-01T00:00:00Z');
	});
});

describe('byStatus', () => {
	const board = [dream('a'), dream('b', { status: 'achieved' }), dream('c')];

	it('gives the whole list back when nothing is chosen', () => {
		expect(byStatus(board, null)).toBe(board);
	});

	it('keeps one state, in the order the list was in', () => {
		expect(byStatus(board, 'dreaming').map((one) => one.id)).toEqual(['a', 'c']);
		expect(byStatus(board, 'in-progress')).toEqual([]);
	});
});
