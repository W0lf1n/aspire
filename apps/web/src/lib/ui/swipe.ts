/**
 * A row swiped to the left, and what is behind it (D78).
 *
 * The Seznam's rows slide aside to show a tray — the hearts, Sdílet, Smazat —
 * the way a row in a phone's own mail app does. The pointer events are in
 * `SwipeRow.svelte`; this is the arithmetic, which is the part that goes
 * wrong: when a finger's first few pixels are a swipe and when they are the
 * list being scrolled, where the row sits under the finger, and whether
 * letting go leaves the tray open.
 */

/** A finger has to travel this far before its gesture is anything at all. */
export const SLOP = 8;

/** Past the tray the row gives this much of what the finger asks for. */
const BEYOND = 0.25;

/** Pixels per millisecond. Past this, the direction of a flick decides. */
export const FLICK = 0.4;

/**
 * What a gesture has turned out to be: a swipe across the row, the list being
 * scrolled, or not enough of either to say yet.
 *
 * Across has to win clearly. A thumb scrolling a list drifts sideways all the
 * time, and a row that slides open under a scroll is a row that is in the
 * way — so the sideways travel must be past the slop *and* more than the
 * travel down. The first answer stands for the rest of the gesture.
 */
export function gestureOf(dx: number, dy: number): 'swipe' | 'scroll' | null {
	const across = Math.abs(dx);
	const down = Math.abs(dy);
	if (across < SLOP && down < SLOP) return null;
	return across > down ? 'swipe' : 'scroll';
}

/**
 * Where the row sits: `from` is where it was when the finger went down — 0
 * closed, `-tray` open — and `dx` how far the finger has gone since. Between
 * closed and open it follows the finger; past either it gives a quarter, so
 * both ends feel like ends rather than like a wall.
 */
export function slidTo(from: number, dx: number, tray: number): number {
	const at = from + dx;
	if (at > 0) return at * BEYOND;
	if (at < -tray) return -tray + (at + tray) * BEYOND;
	return at;
}

/**
 * Whether the tray is open once the finger lifts. A flick decides by its
 * direction — to the left opens, to the right closes — and anything slower
 * decides by whether the row is past the tray's halfway point.
 */
export function staysOpen(at: number, tray: number, speed: number): boolean {
	if (tray <= 0) return false;
	if (Math.abs(speed) > FLICK) return speed < 0;
	return at < -tray / 2;
}
