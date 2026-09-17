/**
 * A dream's photographs one at a time, swiped across like a feed's post
 * (D86, D87): the slides `dreams/slides.ts` says, on the dream's tile and on
 * the reel.
 *
 * The browser does the swiping — a row that scrolls across and snaps to
 * every slide — so what is left to work out is which slide is showing, for
 * the dots, where a dot or an arrow key scrolls to, and — on the reel —
 * whether the row can still move the way a swipe went, because once it
 * cannot, the same swipe is the other reel's (D71, D87). All of it is here so
 * it has a test; the Browser pane cannot scroll a hidden page to check it.
 */

/**
 * Which slide is showing, from how far the row has scrolled. The nearest one:
 * a swipe let go of more than halfway across is the next slide, which is where
 * the snapping will put it anyway. A right-to-left page scrolls the other way
 * and reports a negative distance, which is the same slide.
 */
export function slideAt(scrollLeft: number, width: number, count: number): number {
	if (width <= 0 || count <= 0) return 0;
	const at = Math.round(Math.abs(scrollLeft) / width);
	return Math.min(count - 1, Math.max(0, at));
}

/** Where the row scrolls to for this slide, kept inside the ones there are. */
export function offsetOf(slide: number, width: number, count: number): number {
	if (count <= 0) return 0;
	return Math.min(count - 1, Math.max(0, Math.trunc(slide))) * Math.max(0, width);
}

/** Whether a row of slides can still move back, and on. */
export interface Room {
	back: boolean;
	on: boolean;
}

/**
 * How far a row can still go either way, from where it is scrolled. A pixel
 * of slack either side, because a row snapped to its last slide can rest a
 * fraction short of the end on a screen with a fractional pixel ratio.
 */
export function roomAcross(scrollLeft: number, scrollWidth: number, clientWidth: number): Room {
	const at = Math.abs(scrollLeft);
	const end = Math.max(0, scrollWidth - clientWidth);
	return { back: at > 1, on: at < end - 1 };
}

/**
 * Whether a swipe the row was able to take was the row's: +1 is on — a finger
 * moving right to left, the next slide — and −1 back, as `sideways.ts` counts
 * them. No row, or a row already at that end, and the swipe is the reel's.
 */
export function canGo(room: Room | null, direction: -1 | 1): boolean {
	if (!room) return false;
	return direction > 0 ? room.on : room.back;
}

/** The slide one step from the one showing, for an arrow key. */
export function stepOffset(
	scrollLeft: number,
	width: number,
	scrollWidth: number,
	direction: -1 | 1
): number {
	const count = width > 0 ? Math.round(scrollWidth / width) : 0;
	return offsetOf(slideAt(scrollLeft, width, count) + direction, width, count);
}
