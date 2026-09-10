/**
 * Which dreams the board shows, which one it opens with, what order the
 * rest are in, what a tile says under the title, and whose anniversary
 * today is.
 *
 * Five rules live here. The reel is what is not yet achieved (PLAN §3.2): a
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
 * How many tiles the board puts in the document at once, and how many more
 * it adds when the end of them is neared.
 *
 * A tile is a whole screen with a photograph on it. A hundred of them is a
 * first paint you can feel, on the one screen whose promise is that it is
 * instant (PLAN.md §2). Five is the one being looked at and four swipes of
 * slack, which is more than a thumb gets ahead of an observer (D30).
 */
export const REEL_WINDOW = 5;

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

/**
 * The order one opening of the reel is in: the day's pick first, everything
 * else shuffled (D30).
 *
 * A board of a hundred dreams in a fixed order becomes a route you know by
 * heart, and a dream you always reach on the ninetieth swipe is a dream you
 * never see — which is the same habituation the daily pick exists to break,
 * one tile further down. So the tail is shuffled on every open, while the
 * head stays the day's pick: the board still opens on the dream two devices
 * agree about, and what follows is different every time.
 *
 * It is a sequence of ids rather than of dreams, because it has to survive
 * the board being fetched again — a heart tapped must not reshuffle the reel
 * under a thumb.
 */
export function reelSequence(
	dreams: Dream[],
	pickedId: string | null,
	random: () => number = Math.random
): string[] {
	const rows = reelDreams(dreams);
	const picked = rows.find((dream) => dream.id === pickedId);
	const rest = shuffle(
		rows.filter((dream) => dream !== picked).map((dream) => dream.id),
		random
	);
	return picked ? [picked.id, ...rest] : rest;
}

/**
 * The reel in a remembered sequence. What has left the reel since — a dream
 * marked splněno on another device, or deleted — falls out, and what has
 * arrived goes on the end rather than being dropped or reshuffling the rest.
 */
export function reelOrder(dreams: Dream[], sequence: string[]): Dream[] {
	const rows = reelDreams(dreams);
	const byId = new Map(rows.map((dream) => [dream.id, dream]));

	const ordered: Dream[] = [];
	for (const id of sequence) {
		const dream = byId.get(id);
		if (dream) {
			ordered.push(dream);
			byId.delete(id);
		}
	}

	// Whatever the sequence never knew about, in board order.
	return [...ordered, ...rows.filter((dream) => byId.has(dream.id))];
}

/** Fisher–Yates, on a copy. */
function shuffle<T>(items: T[], random: () => number): T[] {
	const out = [...items];
	for (let i = out.length - 1; i > 0; i--) {
		const j = Math.min(Math.floor(random() * (i + 1)), i);
		[out[i], out[j]] = [out[j], out[i]];
	}
	return out;
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
