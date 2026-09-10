/**
 * Which dreams the board shows, which one it opens with, what a tile says
 * under the title, and whose anniversary today is.
 *
 * Four rules live here. The reel is what is not yet achieved (PLAN §3.2): a
 * dream marked splněno leaves the swipe and turns up in the Síň slávy, so
 * the board stays what is still ahead. And the daily pick is the tile the
 * reel opens on — the dream shown least recently — so the board is a
 * different one each morning and the wall never becomes wallpaper.
 *
 * The pick is worked out here rather than on the server: the board already
 * carries `lastShownAt` for every dream, so all the server is asked for is
 * the stamp, and a board read from the cache still opens on a dream (D25).
 */

import type { Dream } from '@aspire/contracts';

/**
 * The line under a dream's title on a tile: the affirmation when there is
 * one, the why otherwise, and nothing when there is neither.
 *
 * Two blocks of type on a photograph is the limit, so the two do not both
 * fit: the why explains the dream to you, the affirmation states it, and on
 * a board you swipe every morning the one that states it wins (D27). Both
 * are on the dream's own screen, where there is room to read.
 */
export function tileLine(dream: Pick<Dream, 'affirmation' | 'why'>): {
	text: string;
	/** True when it is the affirmation, which the tile sets a shade louder. */
	said: boolean;
} {
	const said = dream.affirmation.trim();
	return said.length > 0 ? { text: said, said: true } : { text: dream.why, said: false };
}

/** The reel: everything still ahead of you, in board order. */
export function reelDreams(dreams: Dream[]): Dream[] {
	return dreams.filter((dream) => dream.status !== 'achieved');
}

/** The wall: what is behind you, the most recently achieved first. */
export function achievedDreams(dreams: Dream[]): Dream[] {
	return dreams
		.filter((dream) => dream.status === 'achieved')
		.sort((a, b) => stamp(b.achievedAt) - stamp(a.achievedAt) || a.sortOrder - b.sortOrder);
}

/** Was this stamp made on the day `now` falls in, as the device counts days? */
export function shownToday(iso: string | null, now: Date = new Date()): boolean {
	if (iso === null) return false;
	const then = new Date(iso);
	return (
		then.getFullYear() === now.getFullYear() &&
		then.getMonth() === now.getMonth() &&
		then.getDate() === now.getDate()
	);
}

/**
 * The dream the board opens with: the one already shown today when there is
 * one — so the pick holds all day, and two devices on the same board agree
 * on it — and otherwise the least recently shown, at random among those
 * never shown at all (PLAN §5).
 */
export function pickDaily(
	dreams: Dream[],
	now: Date = new Date(),
	random: () => number = Math.random
): Dream | null {
	const candidates = reelDreams(dreams);
	if (candidates.length === 0) return null;

	const already = candidates.find((dream) => shownToday(dream.lastShownAt, now));
	if (already) return already;

	const never = candidates.filter((dream) => dream.lastShownAt === null);
	if (never.length > 0)
		return never[Math.min(Math.floor(random() * never.length), never.length - 1)];

	return candidates.reduce((oldest, dream) =>
		stamp(dream.lastShownAt) < stamp(oldest.lastShownAt) ? dream : oldest
	);
}

/** The reel, with the day's pick moved to the front. */
export function reelOrder(dreams: Dream[], pickedId: string | null): Dream[] {
	const rows = reelDreams(dreams);
	const picked = rows.find((dream) => dream.id === pickedId);
	return picked ? [picked, ...rows.filter((dream) => dream !== picked)] : rows;
}

/** A dream that came true on this day in an earlier year, and how long ago. */
export interface Anniversary {
	dream: Dream;
	/** Whole years since, always one or more. */
	years: number;
}

/**
 * Today's anniversary: the dream achieved on this day of this month in an
 * earlier year (PLAN.md §3.3). The proof that the system works is the wall;
 * this is the wall reaching the board on the one morning it has a reason to.
 *
 * The day is the device's own, as the daily pick's is (`shownToday`), so an
 * anniversary lands on the day the person is living rather than on UTC's.
 * A dream achieved on 29 February has its anniversary on 29 February; the
 * alternative is inventing a date it did not happen on.
 *
 * When two fell on the same day, the one achieved most recently wins, which
 * is the order the wall is in — one line on the board, never a list.
 */
export function anniversaryToday(dreams: Dream[], now: Date = new Date()): Anniversary | null {
	for (const dream of achievedDreams(dreams)) {
		if (dream.achievedAt === null) continue;

		const then = new Date(dream.achievedAt);
		if (then.getMonth() !== now.getMonth() || then.getDate() !== now.getDate()) continue;

		const years = now.getFullYear() - then.getFullYear();
		if (years >= 1) return { dream, years };
	}

	return null;
}

/** A stamp as a number, with never counting as the beginning of time. */
function stamp(iso: string | null): number {
	return iso === null ? 0 : Date.parse(iso);
}
