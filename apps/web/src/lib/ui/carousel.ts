/**
 * A dream's photographs one at a time, swiped across like a feed's post
 * (D86): the collage first, then each photograph on its own.
 *
 * The browser does the swiping — a row that scrolls across and snaps to
 * every slide — so what is left to work out is which slide is showing, for
 * the dots under it, and where a tapped dot scrolls to. Both are here so they
 * have a test; the Browser pane cannot scroll a hidden page to check them.
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
