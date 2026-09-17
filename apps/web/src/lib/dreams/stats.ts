/**
 * The board counted: how many dreams there are, how many in each state, and
 * when any of them was last changed (D77).
 *
 * The Seznam is the one screen on which the board is all of itself (D44), so
 * it is the one screen that can say how big that is. Four numbers and a date,
 * and nothing worked out from them — no share done, no streak, no pace. A
 * count says what is on the board; a percentage would start saying how he is
 * doing, and this is not a tracker.
 */

import type { Dream, DreamStatus } from '@aspire/contracts';

export interface BoardStats {
	total: number;
	/** How many dreams are in each state; the three add up to `total`. */
	by: Record<DreamStatus, number>;
	/** ISO datetime of the most recent change to any dream, or null on an empty board. */
	lastChange: string | null;
}

/**
 * When this dream was last changed. A board remembered from before the field
 * existed has no `updatedAt` on it, and then the day it was written is the
 * last thing known to have happened to it.
 */
export function changedAt(dream: Pick<Dream, 'updatedAt' | 'createdAt'>): string {
	return dream.updatedAt || dream.createdAt;
}

export function boardStats(dreams: Dream[]): BoardStats {
	const by: Record<DreamStatus, number> = { dreaming: 0, 'in-progress': 0, achieved: 0 };
	let lastChange: string | null = null;

	for (const dream of dreams) {
		by[dream.status] += 1;

		const at = changedAt(dream);
		if (lastChange === null || Date.parse(at) > Date.parse(lastChange)) lastChange = at;
	}

	return { total: dreams.length, by, lastChange };
}

/**
 * What the stats bar is narrowing the list to: one state, or nothing. Tapping
 * a number shows the dreams it counted, and tapping it again — or „celkem“ —
 * gives the whole list back.
 */
export type StatusFilter = DreamStatus | null;

export function byStatus(dreams: Dream[], filter: StatusFilter): Dream[] {
	return filter === null ? dreams : dreams.filter((dream) => dream.status === filter);
}
