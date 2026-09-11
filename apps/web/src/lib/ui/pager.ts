/**
 * The reel's paging: one dream per gesture, however long the gesture is.
 *
 * CSS snapping nearly does this on its own. `scroll-snap-stop: always` is the
 * standard's own promise that a scroll will not pass over a snap position,
 * and where it is kept the reel already lands one dream on from wherever it
 * started. It is kept unevenly. A drag the length of the screen carries as
 * far as the finger does before the fling is handed to the snapping; a
 * trackpad flick is dozens of wheel events and every one of them is its own
 * scrolling operation, so the promise is kept four times and the reel has
 * gone past four dreams.
 *
 * So the snapping stays — it is what makes a reel that is let go of rest on
 * a dream, it costs nothing, and it is what still works if this never runs —
 * and a fence goes around it:
 *
 * - **A touch may travel one page from where it began, and no further.** The
 *   fence is checked on every scroll event, and setting `scrollTop` is what
 *   ends a fling, so a swipe the length of the screen lands on the next
 *   dream and stops there. The length of the swipe decides nothing.
 * - **A wheel gesture asks for one page and then says nothing** until the
 *   wheel has been quiet, so a trackpad's momentum tail is not a second
 *   swipe.
 * - **The arrow and page keys move one dream**, Home and End the ends.
 *
 * What the reel does on its own — a flick that does not reach the fence, the
 * rubber band at either end, the momentum under a thumb — is left to the
 * browser, which does it better than an animation frame ever will.
 *
 * The arithmetic is here rather than in the component so that it has a test:
 * node has no scroll container, but it does have arithmetic.
 */

/**
 * How far a wheel gesture travels before it counts as a swipe, in pixels.
 * One notch of a mouse wheel is around 100 and a trackpad reports single
 * figures, so this is a nudge on the one and nothing at all on the other.
 */
export const WHEEL_REACH = 40;

/**
 * How long the wheel has to be quiet before a new gesture can begin.
 *
 * This is the line between a trackpad's momentum and a hand. A trackpad — and
 * a free-spinning wheel — reports every frame or two, 8 to 33 ms apart, and
 * keeps reporting for a second after the fingers have gone; a hand turning a
 * notched wheel as fast as it can manage leaves 60 ms between clicks. Fifty
 * separates them. Higher and a wheel turned steadily would be locked out of
 * the reel; lower and the tail of a flick reads as a second one.
 */
export const GESTURE_GAP = 50;

/**
 * How long the reel has to be still, after the finger has gone, before the
 * swipe counts as over and the fence comes down.
 *
 * Generous on purpose. A fling reports a scroll on every frame and each one
 * puts this back, so the only thing it measures is the gap between the finger
 * lifting and the momentum starting — and a fence that comes down inside that
 * gap is no fence at all. Nothing waits on it: the dream on the screen is
 * reported from the scroll itself, and a deliberate move goes through `to`.
 */
export const SETTLE_GAP = 300;

/** A line, in pixels, for the browsers that report wheels in lines. */
export const WHEEL_LINE = 16;

/** How long a glide takes when the stylesheet will not say (`--dur-slow`). */
export const GLIDE_MS = 360;

/** Which page a scroll offset is showing: the nearest one. */
export function pageAt(scrollTop: number, height: number): number {
	if (!(height > 0)) return 0;
	return Math.max(0, Math.round(scrollTop / height));
}

/** Where a page begins. */
export function offsetOf(index: number, height: number): number {
	return index * height;
}

/** A step from here, and never off either end. */
export function stepTo(index: number, step: number, pages: number): number {
	return Math.min(Math.max(index + step, 0), Math.max(pages - 1, 0));
}

/**
 * A scroll offset held to one page either side of where the gesture began.
 * This is the whole of "one dream per swipe": the reel moves freely inside
 * the fence and cannot leave it, so a long slide and a short flick ask for
 * exactly the same thing.
 */
export function held(scrollTop: number, anchor: number, height: number, pages: number): number {
	const first = offsetOf(stepTo(anchor, -1, pages), height);
	const last = offsetOf(stepTo(anchor, 1, pages), height);
	return Math.min(Math.max(scrollTop, first), last);
}

/**
 * A wheel event's travel in pixels. Firefox reports lines rather than pixels
 * and there is a third mode that reports pages; a reel that counted them raw
 * would never move on the one and jump on the other.
 */
export function wheelTravel(deltaY: number, mode: number, height: number): number {
	if (mode === 1) return deltaY * WHEEL_LINE;
	if (mode === 2) return deltaY * height;
	return deltaY;
}

/** What a wheel gesture asks for once it has travelled far enough. */
export function wheelStep(travel: number, reach = WHEEL_REACH): -1 | 0 | 1 {
	if (travel >= reach) return 1;
	if (travel <= -reach) return -1;
	return 0;
}

/** Where a key takes the reel, or null when the key is not the reel's. */
export function keyTarget(key: string, index: number, pages: number): number | null {
	switch (key) {
		case 'ArrowDown':
		case 'PageDown':
			return stepTo(index, 1, pages);
		case 'ArrowUp':
		case 'PageUp':
			return stepTo(index, -1, pages);
		case 'Home':
			return 0;
		case 'End':
			return Math.max(pages - 1, 0);
		default:
			return null;
	}
}

/**
 * `--ease-out`'s twin in JavaScript. The token is cubic-bezier(0.16, 1, 0.3,
 * 1), which is the curve everyone else calls easeOutExpo: most of the
 * distance in the first third, then a long quiet landing. A scroll offset is
 * not a property CSS can animate for us, so the curve is written out here —
 * and it is the only place in the app where that is true, which is why this
 * is not a second motion system.
 */
export function easeOut(t: number): number {
	return t >= 1 ? 1 : 1 - Math.pow(2, -10 * t);
}

/** A CSS duration in milliseconds, or null when the string is not one. */
export function durationOf(value: string): number | null {
	const text = value.trim();
	const ms = text.endsWith('ms')
		? Number(text.slice(0, -2))
		: text.endsWith('s')
			? Number(text.slice(0, -1)) * 1000
			: Number.NaN;
	return Number.isFinite(ms) && ms > 0 ? ms : null;
}

/** Whether a key belongs to something being typed into rather than the reel. */
function typing(target: EventTarget | null): boolean {
	if (!(target instanceof HTMLElement)) return false;
	if (target.isContentEditable) return true;
	return /^(input|textarea|select)$/i.test(target.tagName);
}

/** What the pager needs to know about the reel it is driving. */
export interface Paged {
	/** How many dreams are in the document right now. */
	pages: () => number;
	/** Told whenever the dream on the screen changes. */
	onPage: (index: number) => void;
}

/** The reel, once the pager has hold of it. */
export interface Pager {
	/**
	 * Put a dream on the screen, now and without a glide: what a screen that
	 * has become a different reel wants, rather than a swipe through the one
	 * it used to be. It also lets go of whatever fence a gesture left behind,
	 * which is the one thing that can hold a deliberate move back.
	 */
	to: (index: number) => void;
	/** Let go of the reel. */
	destroy: () => void;
}

/** Drive a scroll region as a pager. */
export function pager(el: HTMLElement, reel: Paged): Pager {
	/** The page height, kept rather than measured on every frame. */
	let height = el.getBoundingClientRect().height;

	/** The page a touch began on, for as long as one is in progress. */
	let anchor: number | null = null;

	/** The running glide's cancel, or null when the reel is the browser's. */
	let stop: (() => void) | null = null;

	/** The page last reported, so the same number is never reported twice. */
	let reported = -1;

	/** Whether a finger is down, which is what the fence is built for. */
	let touching = false;

	let travel = 0;
	let wheeledAt = 0;
	let spent = false;
	let quiet: ReturnType<typeof setTimeout> | undefined;

	const still = matchMedia('(prefers-reduced-motion: reduce)');
	const index = () => pageAt(el.scrollTop, height);

	const report = (at: number) => {
		if (at === reported) return;
		reported = at;
		reel.onPage(at);
	};

	const cancel = () => {
		if (!stop) return;
		stop();
		stop = null;
		el.style.scrollSnapType = '';
	};

	/** Take the reel to a page, on the house curve. */
	const glide = (to: number) => {
		cancel();
		// A move that was asked for is not a gesture, and the fence belongs to
		// the gesture that put it up.
		anchor = null;

		const target = offsetOf(to, height);
		if (still.matches || Math.abs(target - el.scrollTop) < 1) {
			el.scrollTop = target;
			report(to);
			return;
		}

		// Snapping and a tween disagree by definition: every frame of the
		// tween is a place the reel is not allowed to rest. It is off for the
		// length of the glide and back on at the end, which lands on a snap
		// position anyway, so restoring it moves nothing.
		el.style.scrollSnapType = 'none';

		const from = el.scrollTop;
		const span = target - from;
		const began = performance.now();
		const ms = durationOf(getComputedStyle(el).getPropertyValue('--dur-slow')) ?? GLIDE_MS;

		let frame = requestAnimationFrame(function tick(now) {
			const t = Math.min(1, (now - began) / ms);
			el.scrollTop = from + span * easeOut(t);
			if (t < 1) {
				frame = requestAnimationFrame(tick);
				return;
			}
			stop = null;
			el.style.scrollSnapType = '';
			report(to);
		});

		stop = () => cancelAnimationFrame(frame);
	};

	/**
	 * The swipe is over: the reel has stopped moving and the finger has gone,
	 * so the fence comes down and wherever it came to rest is the dream.
	 */
	const settled = () => {
		if (touching) return;
		anchor = null;
		report(index());
	};

	const onScroll = () => {
		// The fence, from the gesture until it has stopped moving. Setting
		// `scrollTop` is what ends a fling, and the half pixel of slack is
		// what stops this handler from answering its own assignment for ever.
		if (anchor !== null && stop === null) {
			const bound = held(el.scrollTop, anchor, height, reel.pages());
			if (Math.abs(bound - el.scrollTop) > 0.5) el.scrollTop = bound;
		}
		report(index());
		clearTimeout(quiet);
		quiet = setTimeout(settled, SETTLE_GAP);
	};

	const onTouchStart = () => {
		// A second finger is part of the swipe that is already happening, not
		// a new one; re-anchoring on it would move the fence mid-drag.
		if (touching) return;
		cancel();
		touching = true;
		anchor = index();
	};

	const onTouchEnd = (event: TouchEvent) => {
		if (event.touches.length > 0) return;
		touching = false;
		clearTimeout(quiet);
		quiet = setTimeout(settled, SETTLE_GAP);
	};

	const onWheel = (event: WheelEvent) => {
		// The areas rail is scrolled sideways; a gesture that is mostly
		// horizontal is its, not the reel's.
		if (Math.abs(event.deltaX) > Math.abs(event.deltaY)) return;
		event.preventDefault();

		const now = performance.now();
		const gap = now - wheeledAt;
		wheeledAt = now;
		if (gap > GESTURE_GAP) {
			travel = 0;
			spent = false;
		}

		// A trackpad's momentum keeps arriving for a second after the fingers
		// have gone. It is the tail of the swipe that has already been
		// answered, not a second swipe.
		if (spent || stop !== null) {
			travel = 0;
			return;
		}

		travel += wheelTravel(event.deltaY, event.deltaMode, height);
		const step = wheelStep(travel);
		if (step === 0) return;

		travel = 0;
		spent = true;
		glide(stepTo(index(), step, reel.pages()));
	};

	const onKey = (event: KeyboardEvent) => {
		if (event.metaKey || event.ctrlKey || event.altKey || event.shiftKey) return;
		if (typing(event.target)) return;

		const to = keyTarget(event.key, index(), reel.pages());
		if (to === null) return;

		event.preventDefault();
		glide(to);
	};

	/** A window that changes shape must not change which dream is on it. */
	const sizes = new ResizeObserver(() => {
		const now = el.getBoundingClientRect().height;
		if (now === height || !(now > 0)) return;

		const at = reported < 0 ? index() : reported;
		height = now;
		if (anchor === null && stop === null) el.scrollTop = offsetOf(at, height);
	});

	sizes.observe(el);
	el.addEventListener('scroll', onScroll, { passive: true });
	el.addEventListener('touchstart', onTouchStart, { passive: true });
	el.addEventListener('touchend', onTouchEnd, { passive: true });
	el.addEventListener('touchcancel', onTouchEnd, { passive: true });
	// On the window rather than the reel: the chrome floats over the
	// photograph, and a wheel over a chip is still a wheel over the reel.
	window.addEventListener('wheel', onWheel, { passive: false });
	window.addEventListener('keydown', onKey);

	const destroy = () => {
		cancel();
		clearTimeout(quiet);
		sizes.disconnect();
		el.removeEventListener('scroll', onScroll);
		el.removeEventListener('touchstart', onTouchStart);
		el.removeEventListener('touchend', onTouchEnd);
		el.removeEventListener('touchcancel', onTouchEnd);
		window.removeEventListener('wheel', onWheel);
		window.removeEventListener('keydown', onKey);
	};

	return {
		to: (index) => {
			cancel();
			anchor = null;
			el.scrollTop = offsetOf(index, height);
			report(index);
		},
		destroy
	};
}
