/**
 * Dragging a row of the Seznam to another line (D79).
 *
 * The rows of a list are one height — a title and a line under it, neither
 * of which wraps — so a drag is arithmetic on that one number: how many rows
 * the finger has passed is where the dream will land, and every row between
 * there and where it came from steps aside by exactly one row. The pointer
 * events are on the screen; this is what they work out, and what has the
 * test.
 */

/**
 * The line a row dragged `dy` pixels from line `from` would land on, as an
 * index. Half a row is the point at which it changes places with its
 * neighbour, which is where the eye expects it: when the dragged row covers
 * more of the next line than of its own.
 */
export function landsOn(from: number, dy: number, rowHeight: number, count: number): number {
	if (!(rowHeight > 0) || count <= 0) return from;
	const to = from + Math.round(dy / rowHeight);
	return Math.min(count - 1, Math.max(0, to));
}

/**
 * How far a row that is *not* being dragged steps aside, in pixels, while the
 * dragged one is headed from line `from` to line `to`. The rows it has passed
 * each move one row the other way to fill the line it left; every other row
 * stays put.
 */
export function stepsAside(index: number, from: number, to: number, rowHeight: number): number {
	if (to > from && index > from && index <= to) return -rowHeight;
	if (to < from && index >= to && index < from) return rowHeight;
	return 0;
}

/**
 * How far the dragged row may go, so it cannot leave the list: up to the
 * first line and down to the last, and no further than half a row past
 * either, which is enough to say „this is the end“.
 */
export function heldTo(dy: number, from: number, rowHeight: number, count: number): number {
	const slack = rowHeight / 2;
	const up = -from * rowHeight - slack;
	const down = (count - 1 - from) * rowHeight + slack;
	return Math.min(down, Math.max(up, dy));
}

/** How close to the scroll region's edge a finger has to be for the list to move. */
export const EDGE = 72;

/** The fastest the list scrolls under a drag, in pixels a frame. */
export const EDGE_SPEED = 14;

/**
 * How far the list scrolls this frame for a finger at `y`, between the scroll
 * region's `top` and `bottom`: nothing in the middle, faster the nearer the
 * edge, up when it is near the top and down near the bottom. A list of a
 * hundred dreams is ten screens, and a drag that cannot scroll can only move
 * a dream within the one it started on.
 */
export function edgeScroll(y: number, top: number, bottom: number): number {
	if (y < top + EDGE) return -EDGE_SPEED * Math.min(1, (top + EDGE - y) / EDGE);
	if (y > bottom - EDGE) return EDGE_SPEED * Math.min(1, (y - (bottom - EDGE)) / EDGE);
	return 0;
}
