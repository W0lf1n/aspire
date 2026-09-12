/**
 * Which dreams the board shows, which one it opens with, what order the
 * rest are in, which area it is asking for, what order the Seznam is in,
 * what a tile says under the title, and whose anniversary today is.
 *
 * Seven rules live here. The reel is what is not yet achieved (PLAN §3.2): a
 * dream marked splněno leaves the swipe and turns up in the Síň slávy, so
 * the board stays what is still ahead. And the daily pick is the tile the
 * reel opens on — the dream with the most fuel, which is how long it has
 * waited weighted by its hearts (D58) — so the board is a different one each
 * morning and the wall never becomes wallpaper.
 *
 * The pick is worked out here rather than on the server: the board already
 * carries `lastShownAt` for every dream, so all the server is asked for is
 * the stamp, and a board read from the cache still opens on a dream (D25).
 */

import type { Dream, DreamCategory } from '@aspire/contracts';

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

/**
 * What the board is asking for: one of the three areas, or all of them
 * (PLAN.md §3.2). Not a category itself, because „everything“ is a state of
 * the filter and never a thing a dream can belong to.
 */
export type BoardFilter = DreamCategory | 'all';

/**
 * The reel, narrowed to one area. „Everything“ is the default and gives the
 * reel back untouched; a dream with no category belongs to no area and so
 * appears only under „everything“ — which is where it already was.
 *
 * It runs after the shuffle, on the ordered reel, so choosing an area lifts
 * dreams out of an order that is already fixed rather than making a new one:
 * the tiles that stay do not move.
 */
export function byCategory(dreams: Dream[], filter: BoardFilter): Dream[] {
	return filter === 'all' ? dreams : dreams.filter((dream) => dream.category === filter);
}

/** The areas the board has anything in, in the order the three are listed. */
export function categoriesOnBoard(dreams: Dream[], all: readonly DreamCategory[]): DreamCategory[] {
	const present = new Set(
		reelDreams(dreams)
			.map((dream) => dream.category)
			.filter((category): category is DreamCategory => category !== null)
	);
	return all.filter((category) => present.has(category));
}

/** The reel: everything still ahead of you, in board order. */
export function reelDreams(dreams: Dream[]): Dream[] {
	return dreams.filter((dream) => dream.status !== 'achieved');
}

/**
 * The list: every dream there is, the most recently written first (D44).
 *
 * Not the reel and not the wall — both of those are a selection, and this is
 * the board itself, the one place a dream that is achieved and a dream that
 * is not stand in the same column. Newest first because the list is where
 * dreams are written: what you just added is at the top, where you are
 * looking.
 *
 * Ties go to the later `sortOrder`, so two dreams written in the same second
 * — an import, a fast thumb — still come out in a fixed order rather than
 * swapping places between renders.
 */
export function listOrder(dreams: Dream[]): Dream[] {
	return [...dreams].sort(
		(a, b) => stamp(b.createdAt) - stamp(a.createdAt) || b.sortOrder - a.sortOrder
	);
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
 * The most hearts that count. Above ten the number keeps going up and the
 * pick stops listening, and that cap is the point rather than a detail: with
 * no ceiling, ten loved dreams would take every morning between them and the
 * rest of the board would never come back — which is D25's habituation with
 * extra steps (D58).
 */
export const LIKES_CAP = 10;

/**
 * How overdue a dream is, as the heart weighs it: whole days since it was
 * last in front of somebody, times what the hearts add (D58).
 *
 *     days since shown × (1 + min(likes, 10) / 10)
 *
 * Ten hearts double it, so a loved dream comes round twice as often as one
 * with none and the eleventh heart does nothing. Never shown at all is
 * `Infinity`, which is the truth — nothing is more overdue than a dream
 * nobody has seen — though the daily pick answers that case itself, at
 * random, before it gets here (D25).
 *
 * Whole days, counted as the device counts days, rather than hours elapsed:
 * two phones on the same board must agree about which dream is the day's, and
 * a difference of seconds between their clocks must not be able to decide it.
 */
export function fuel(dream: Pick<Dream, 'lastShownAt' | 'likes'>, now: Date = new Date()): number {
	if (dream.lastShownAt === null) return Infinity;
	return daysSince(dream.lastShownAt, now) * (1 + counted(dream.likes) / LIKES_CAP);
}

/** Hearts as the pick reads them: none below zero, none above the cap. */
function counted(likes: number): number {
	return Number.isFinite(likes) ? Math.min(Math.max(likes, 0), LIKES_CAP) : 0;
}

/** Whole days between a stamp and now, as the device counts days. */
function daysSince(iso: string, now: Date): number {
	const days = (midnight(now) - midnight(new Date(iso))) / 86_400_000;
	return Math.max(0, days);
}

/** The day a stamp falls in, as a number of days. `Date.UTC` has no summer time. */
function midnight(date: Date): number {
	return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
}

/**
 * The dream the board opens with: the one already shown today when there is
 * one — so the pick holds all day, and two devices on the same board agree
 * on it — otherwise one never shown at all, at random, and otherwise the one
 * with the most fuel (PLAN §5, D58).
 *
 * Fuel rather than the oldest stamp is the heart's one job: a dream you keep
 * tapping is a dream you want in front of you more often, and this is the
 * only rule on the board that reads the count. Ties fall to board order, so
 * the answer is the same on every device without anybody storing it.
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

	return candidates.reduce((best, dream) => (fuel(dream, now) > fuel(best, now) ? dream : best));
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
