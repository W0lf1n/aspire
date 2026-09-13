/**
 * The sideways swipe: across the reel to change which reel it is (D71).
 *
 * The board has two — Vše and Teď — and until now the only way between them
 * was the segment in the floating chrome, which is a small target at the top
 * of a screen held in one hand. A reel is a thing you move with your thumb,
 * so moving sideways on it should move sideways through it, the way every
 * app with two feeds works.
 *
 * It sits beside `pager.ts` rather than inside it. The pager owns the
 * vertical: the fence, the wheel, the snapping, and rule 14's promise that
 * one gesture is one dream. This owns one gesture the pager deliberately
 * ignores, and the two never touch the same numbers.
 *
 * **It cannot take a swipe the reel wanted.** `touch-action: pan-y
 * pinch-zoom` means the browser keeps every vertical pan for itself and
 * hands this the rest, and a gesture only counts when it has gone clearly
 * further across than down — so a thumb flicking up the reel at a slight
 * angle is still a dream, not a different reel. Nothing here calls
 * `preventDefault`, so a gesture that turns out to be vertical after all was
 * never interrupted.
 *
 * The arithmetic is a pure function with a test, as the pager's is: node has
 * no finger, but it does have triangles.
 */

/**
 * How far across a finger must travel before it is a swipe, in pixels.
 *
 * A thumb's idle wobble on a photograph is a few pixels; a deliberate sweep
 * across a phone is a hundred and more. Fifty-six is a third of the narrow
 * way across the smallest screen this runs on, which is far enough to mean
 * it and near enough to do one-handed.
 */
export const REACH = 56;

/**
 * How much further across than down, for the gesture to be a sideways one at
 * all.
 *
 * The reel is a vertical pager, so the doubt is always in its favour: a drag
 * that is merely more across than down is an unsteady swipe up, not a swipe
 * sideways. At 1.4 the finger has to be travelling below about 36 degrees
 * off the horizontal, which is a movement nobody makes by accident while
 * paging a reel.
 */
export const BIAS = 1.4;

/** Which way a gesture went: −1 back, +1 on, 0 not a sideways swipe at all. */
export type Direction = -1 | 0 | 1;

/**
 * What a finger's travel amounts to. Positive is **on** — a finger moving
 * right to left, which carries the next reel in from the right, the way a
 * page turns.
 */
export function swipeOf(dx: number, dy: number, reach = REACH, bias = BIAS): Direction {
	if (!Number.isFinite(dx) || !Number.isFinite(dy)) return 0;

	const across = Math.abs(dx);
	if (across < reach) return 0;
	if (across < Math.abs(dy) * bias) return 0;

	return dx < 0 ? 1 : -1;
}

/** Where a step from here lands, and never off either end. */
export function stepWithin<T>(list: readonly T[], current: T, step: number): T {
	const at = list.indexOf(current);
	if (at < 0) return current;
	return list[Math.min(Math.max(at + step, 0), list.length - 1)];
}

export interface Swept {
	/** A sideways swipe happened: −1 back, +1 on. */
	onSwipe: (direction: -1 | 1) => void;
}

export interface Sideways {
	destroy: () => void;
}

/**
 * Watch an element for sideways swipes, and the arrow keys for the same
 * thing — the segment they move is a choice of one from two, which is what
 * left and right are for, and the pager has already taken up and down.
 */
export function sideways(el: HTMLElement, swept: Swept): Sideways {
	let fromX = 0;
	let fromY = 0;
	/** A single finger that has not been joined by a second. */
	let tracking = false;

	const onTouchStart = (event: TouchEvent) => {
		// One finger only: a second is a pinch, and a pinch that ends wide is
		// not a swipe however far the first finger travelled.
		if (event.touches.length !== 1) {
			tracking = false;
			return;
		}

		tracking = true;
		fromX = event.touches[0].clientX;
		fromY = event.touches[0].clientY;
	};

	const onTouchMove = (event: TouchEvent) => {
		if (event.touches.length > 1) tracking = false;
	};

	const onTouchEnd = (event: TouchEvent) => {
		if (!tracking) return;
		tracking = false;

		const gone = event.changedTouches[0];
		if (!gone) return;

		const direction = swipeOf(gone.clientX - fromX, gone.clientY - fromY);
		if (direction !== 0) swept.onSwipe(direction);
	};

	const onKey = (event: KeyboardEvent) => {
		if (event.metaKey || event.ctrlKey || event.altKey) return;

		// Not while somebody is typing, which on this screen is nothing today
		// and could be a search field tomorrow.
		const on = document.activeElement;
		if (on instanceof HTMLInputElement || on instanceof HTMLTextAreaElement) return;

		if (event.key === 'ArrowRight') swept.onSwipe(1);
		else if (event.key === 'ArrowLeft') swept.onSwipe(-1);
		else return;

		event.preventDefault();
	};

	el.addEventListener('touchstart', onTouchStart, { passive: true });
	el.addEventListener('touchmove', onTouchMove, { passive: true });
	el.addEventListener('touchend', onTouchEnd, { passive: true });
	el.addEventListener('touchcancel', onTouchEnd, { passive: true });
	window.addEventListener('keydown', onKey);

	return {
		destroy() {
			el.removeEventListener('touchstart', onTouchStart);
			el.removeEventListener('touchmove', onTouchMove);
			el.removeEventListener('touchend', onTouchEnd);
			el.removeEventListener('touchcancel', onTouchEnd);
			window.removeEventListener('keydown', onKey);
		}
	};
}
