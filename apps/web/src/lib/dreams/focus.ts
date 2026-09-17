/**
 * Teď — the second reel, and which of the two the board opens on (D53).
 *
 * The main reel is everything still ahead, shuffled on every open so no dream
 * has a permanent place (D30). That is the right shape for a hundred dreams
 * and the wrong one for the handful somebody is actually working on this
 * month: those want a fixed order, because ten dreams are reached in ten
 * swipes and the order is then the point rather than a route learned by
 * heart. So there are two reels, and the board says which it is showing.
 *
 * A dream is on Teď when it has a rank. The rank is an ordering key, not a
 * position — the ten are numbered on screen by their place in the list — so a
 * gap left by one taken off costs nothing and there is no compaction here or
 * on the server to get wrong.
 */

import type { Dream } from '@aspire/contracts';
import { reelDreams } from './board';
import { FOCUS_MAX } from './rules';

/**
 * The two reels, in the order the segment shows them. A sideways swipe steps
 * through this list (D71), so the gesture and the pill can never disagree
 * about which one is next.
 */
export const REELS = ['all', 'focus'] as const;

/** Which reel the board is showing. */
export type Reel = (typeof REELS)[number];

/**
 * The dreams on Teď, in the person's own order.
 *
 * Built on `reelDreams`, so a dream marked splněno leaves Teď exactly as it
 * leaves the reel. The server clears the rank when that happens; this is the
 * same rule on the device, which a board read from the cache needs (D24).
 *
 * Ties go to `sortOrder`, as everywhere else: two dreams can share a rank
 * after a reorder that crossed with another device, and the order still has
 * to be the same one on every screen.
 */
export function focusDreams(dreams: Dream[]): Dream[] {
	return reelDreams(dreams)
		.filter((dream) => dream.focusRank !== null)
		.sort((a, b) => a.focusRank! - b.focusRank! || a.sortOrder - b.sortOrder);
}

/**
 * The Seznam narrowed by the board's own two words (D83). „Vše“ is the whole
 * list — the achieved dreams too, because the Seznam is the inventory and not
 * the reel (D44) — and „Teď“ is the dreams on the second reel.
 *
 * In the list's order, not Teď's. A line's number is its place in the whole
 * list (D48), and the ten read in rank order would be numbered 9, 2, 31, 4;
 * the order of the ten is what `/ted` and the reel are for.
 */
export function byReel(dreams: Dream[], reel: Reel): Dream[] {
	if (reel === 'all') return dreams;
	const on = new Set(focusDreams(dreams).map((dream) => dream.id));
	return dreams.filter((dream) => on.has(dream.id));
}

/** Whether Teď is full, so the pill can say so on the tap (D53). */
export function focusFull(dreams: Dream[]): boolean {
	return focusDreams(dreams).length >= FOCUS_MAX;
}

/** The sentence for a tap that would be the eleventh. Mirrors the server's. */
export function focusFullSentence(): string {
	return `Na teď máš už ${FOCUS_MAX} snů. Některý nejdřív odeber.`;
}

/**
 * Which reel this device opens on, remembered between opens.
 *
 * A week of focusing on the same ten should not cost a tap every morning, so
 * the choice is kept — per device, like the offline policy (D39), because it
 * is about this phone rather than about the board. „Vše“ is the default: it
 * is the reel the day's pick lives on, and a board with nothing on Teď has
 * nothing else to show.
 */
export const REEL_KEY = 'reel';

export function readReel(): Reel {
	if (typeof localStorage === 'undefined') return 'all';
	try {
		return localStorage.getItem(REEL_KEY) === 'focus' ? 'focus' : 'all';
	} catch {
		return 'all';
	}
}

export function saveReel(reel: Reel): void {
	try {
		localStorage.setItem(REEL_KEY, reel);
	} catch {
		/* private mode: this open only */
	}
}
