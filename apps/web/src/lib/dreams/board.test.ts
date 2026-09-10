import { describe, expect, it } from 'vitest';
import type { Dream } from '@aspire/contracts';
import {
	achievedDreams,
	anniversaryToday,
	pickDaily,
	reelDreams,
	reelOrder,
	shownToday,
	tileLine
} from './board';

/** Every stamp is a whole number of days from this, so no timezone changes it. */
const NOW = new Date('2026-09-10T09:00:00Z');
const TODAY = '2026-09-10T09:00:00Z';
const YESTERDAY = '2026-09-09T09:00:00Z';
const A_WEEK_AGO = '2026-09-03T09:00:00Z';

function dream(id: string, over: Partial<Dream> = {}): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		sortOrder: 0,
		targetYear: null,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
		images: [],
		...over
	};
}

describe('anniversaryToday', () => {
	/** Achieved on 10 September, the day NOW falls on, `years` years back. */
	function achieved(id: string, yearsAgo: number, day = '09-10') {
		return dream(id, {
			status: 'achieved',
			achievedAt: `${2026 - yearsAgo}-${day}T12:00:00Z`
		});
	}

	it('finds the dream that came true on this day in an earlier year', () => {
		const found = anniversaryToday([achieved('a', 1)], NOW);

		expect(found).toEqual({ dream: expect.objectContaining({ id: 'a' }), years: 1 });
	});

	it('counts the whole years', () => {
		expect(anniversaryToday([achieved('a', 4)], NOW)?.years).toBe(4);
	});

	it('is nothing on any other day, and nothing on the day itself', () => {
		expect(anniversaryToday([achieved('a', 1, '09-11')], NOW)).toBeNull();
		expect(anniversaryToday([achieved('a', 1, '10-10')], NOW)).toBeNull();
		// Achieved this morning is not an anniversary; it is today.
		expect(anniversaryToday([achieved('a', 0)], NOW)).toBeNull();
	});

	it('ignores a dream that is not achieved', () => {
		const still = dream('a', { status: 'in-progress', achievedAt: null });

		expect(anniversaryToday([still], NOW)).toBeNull();
		expect(anniversaryToday([], NOW)).toBeNull();
	});

	it('takes the most recent when two fell on the same day', () => {
		const found = anniversaryToday([achieved('old', 5), achieved('recent', 2)], NOW);

		expect(found?.dream.id).toBe('recent');
		expect(found?.years).toBe(2);
	});
});

describe('tileLine', () => {
	it('says the affirmation when there is one', () => {
		const line = tileLine(dream('a', { why: 'Ticho a les.', affirmation: 'Bydlím u lesa.' }));

		expect(line).toEqual({ text: 'Bydlím u lesa.', said: true });
	});

	it('falls back to the why, and to nothing at all', () => {
		expect(tileLine(dream('a', { why: 'Ticho a les.' }))).toEqual({
			text: 'Ticho a les.',
			said: false
		});
		expect(tileLine(dream('b'))).toEqual({ text: '', said: false });
	});

	it('does not count an affirmation that is only spaces', () => {
		const line = tileLine(dream('a', { why: 'Ticho a les.', affirmation: '   ' }));

		expect(line).toEqual({ text: 'Ticho a les.', said: false });
	});
});

describe('reelDreams', () => {
	it('keeps what is still ahead, in board order', () => {
		const rows = [
			dream('a'),
			dream('b', { status: 'achieved', achievedAt: YESTERDAY }),
			dream('c', { status: 'in-progress' })
		];

		expect(reelDreams(rows).map((d) => d.id)).toEqual(['a', 'c']);
	});
});

describe('achievedDreams', () => {
	it('keeps only the achieved, the most recent first', () => {
		const rows = [
			dream('old', { status: 'achieved', achievedAt: A_WEEK_AGO }),
			dream('dreaming'),
			dream('new', { status: 'achieved', achievedAt: YESTERDAY })
		];

		expect(achievedDreams(rows).map((d) => d.id)).toEqual(['new', 'old']);
	});

	it('breaks a tie on board order, and leaves the board alone', () => {
		const rows = [
			dream('second', { status: 'achieved', achievedAt: YESTERDAY, sortOrder: 1 }),
			dream('first', { status: 'achieved', achievedAt: YESTERDAY, sortOrder: 0 })
		];

		expect(achievedDreams(rows).map((d) => d.id)).toEqual(['first', 'second']);
		expect(rows.map((d) => d.id)).toEqual(['second', 'first']);
	});
});

describe('shownToday', () => {
	it('is a day, not a duration', () => {
		expect(shownToday(null, NOW)).toBe(false);
		expect(shownToday(TODAY, NOW)).toBe(true);
		expect(shownToday(YESTERDAY, NOW)).toBe(false);
	});
});

describe('pickDaily', () => {
	const first = () => 0;

	it('has nothing to pick from an empty board', () => {
		expect(pickDaily([], NOW, first)).toBeNull();
	});

	it('has nothing to pick when every dream is achieved', () => {
		const rows = [dream('a', { status: 'achieved', achievedAt: YESTERDAY })];

		expect(pickDaily(rows, NOW, first)).toBeNull();
	});

	it('holds all day: the dream already shown today stays the pick', () => {
		const rows = [dream('never'), dream('today', { lastShownAt: TODAY })];

		expect(pickDaily(rows, NOW, first)?.id).toBe('today');
	});

	it('takes one never shown at all before one shown a week ago', () => {
		const rows = [dream('week', { lastShownAt: A_WEEK_AGO }), dream('never')];

		expect(pickDaily(rows, NOW, first)?.id).toBe('never');
	});

	it('picks among those never shown at random', () => {
		const rows = [dream('a'), dream('b'), dream('c')];

		expect(pickDaily(rows, NOW, () => 0)?.id).toBe('a');
		expect(pickDaily(rows, NOW, () => 0.5)?.id).toBe('b');
		expect(pickDaily(rows, NOW, () => 0.99)?.id).toBe('c');
	});

	it('takes the least recently shown once every dream has had its turn', () => {
		const rows = [
			dream('yesterday', { lastShownAt: YESTERDAY }),
			dream('week', { lastShownAt: A_WEEK_AGO })
		];

		expect(pickDaily(rows, NOW, first)?.id).toBe('week');
	});

	it('never picks an achieved dream, however long it has been', () => {
		const rows = [
			dream('done', { status: 'achieved', achievedAt: YESTERDAY }),
			dream('yesterday', { lastShownAt: YESTERDAY })
		];

		expect(pickDaily(rows, NOW, first)?.id).toBe('yesterday');
	});
});

describe('reelOrder', () => {
	it('puts the pick in front and leaves the rest in board order', () => {
		const rows = [dream('a'), dream('b'), dream('c')];

		expect(reelOrder(rows, 'c').map((d) => d.id)).toEqual(['c', 'a', 'b']);
	});

	it('is the board order when there is no pick, or the pick has gone', () => {
		const rows = [dream('a'), dream('b')];

		expect(reelOrder(rows, null).map((d) => d.id)).toEqual(['a', 'b']);
		expect(reelOrder(rows, 'deleted').map((d) => d.id)).toEqual(['a', 'b']);
	});

	it('does not bring an achieved dream back by picking it', () => {
		const rows = [dream('a'), dream('done', { status: 'achieved', achievedAt: YESTERDAY })];

		expect(reelOrder(rows, 'done').map((d) => d.id)).toEqual(['a']);
	});
});
