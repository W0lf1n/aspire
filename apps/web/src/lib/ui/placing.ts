/**
 * The hands that place a photograph (D54): one finger drags it, two pinch it,
 * a wheel zooms it on a laptop and the arrow keys nudge it.
 *
 * Both editors use it — `CropEditor`, one photograph on the reel's page, and
 * `CollageEditor`, a cell of a collage on the same page (D85) — because a
 * cell has to move under a finger exactly the way a photograph on its own
 * does, or placing the second photograph is a different gesture from placing
 * the first. The arithmetic is `images/focal.ts`; this is only the fingers:
 * which ones are down, where they started, and what a lifted one leaves.
 *
 * The editor says what is being moved by handing over five getters, read at
 * the start of every gesture — so the collage can change which cell is in
 * hand between two gestures and the hands follow it.
 */

import { dragged, spread, zoomed, type Focal, type Size } from '$lib/images/focal';

export interface Placing {
	/** The crop being changed. */
	get: () => Focal;
	set: (focal: Focal) => void;
	/** The frame it is changed in, in screen pixels: the page, or one cell. */
	frame: () => Size;
	/** The picture's own pixels, which a drag divides by. */
	picture: () => Size;
	/** Whether the editor is saving, and the picture must not move under it. */
	resting: () => boolean;
}

/** What the hands read of a pointer event, so a test can hand them one. */
export interface Finger {
	pointerId: number;
	clientX: number;
	clientY: number;
	currentTarget?: EventTarget | null;
}

export interface Hands {
	down: (event: Finger) => void;
	move: (event: Finger) => void;
	up: (event: Finger) => void;
	wheel: (event: Pick<WheelEvent, 'deltaY' | 'preventDefault'>) => void;
	keys: (event: Pick<KeyboardEvent, 'key' | 'preventDefault'>) => void;
	/** Whether a finger is down, so a second one landing on another cell is still this gesture. */
	holding: () => boolean;
}

/** How far an arrow key moves the picture, in frame pixels. */
export const NUDGE = 24;

export function hands(placing: Placing): Hands {
	/** Where the gesture began, and the crop it began from. */
	let from: { x: number; y: number; focal: Focal; gap: number; zoom: number } | null = null;

	/**
	 * Every finger currently down, so two of them can be a pinch. A plain
	 * array rather than a reactive collection: nothing on the screen is drawn
	 * from it, and the crop it produces is what the editor holds.
	 */
	let touches: { id: number; x: number; y: number }[] = [];

	function put(event: Finger) {
		touches = [
			...touches.filter((one) => one.id !== event.pointerId),
			{ id: event.pointerId, x: event.clientX, y: event.clientY }
		];
	}

	function down(event: Finger) {
		if (placing.resting()) return;
		put(event);
		try {
			// Keeps the gesture on this element once a finger leaves it. A
			// pointer the browser has already let go of throws here, and a
			// drag that works without capture is better than one that stops.
			(event.currentTarget as Element | null)?.setPointerCapture?.(event.pointerId);
		} catch {
			/* no capture; the move and up handlers still fire on this element */
		}

		const now = placing.get();
		from = {
			x: event.clientX,
			y: event.clientY,
			focal: { ...now },
			gap: touches.length === 2 ? spread(touches[0], touches[1]) : 0,
			zoom: now.zoom
		};
	}

	function move(event: Finger) {
		if (!from || !touches.some((one) => one.id === event.pointerId)) return;
		put(event);

		if (touches.length === 2 && from.gap > 0) {
			// A pinch: the zoom is the distance between the fingers against the
			// distance they started at, so letting go and starting again does
			// not jump.
			placing.set(
				zoomed({ ...placing.get(), zoom: from.zoom }, spread(touches[0], touches[1]) / from.gap)
			);
			return;
		}

		// The whole drag from where the finger went down, not frame by frame,
		// so a gesture that hits an edge and comes back ends where it should.
		placing.set(
			dragged(
				from.focal,
				event.clientX - from.x,
				event.clientY - from.y,
				placing.frame(),
				placing.picture()
			)
		);
	}

	function up(event: Finger) {
		touches = touches.filter((one) => one.id !== event.pointerId);
		if (touches.length === 0) {
			from = null;
			return;
		}

		// A finger lifted off a pinch: whatever is left starts a new drag from
		// where it is, rather than sending the picture across the screen.
		const left = touches[0];
		const now = placing.get();
		from = { x: left.x, y: left.y, focal: { ...now }, gap: 0, zoom: now.zoom };
	}

	function wheel(event: Pick<WheelEvent, 'deltaY' | 'preventDefault'>) {
		if (placing.resting()) return;
		event.preventDefault();
		placing.set(zoomed(placing.get(), event.deltaY < 0 ? 1.08 : 1 / 1.08));
	}

	function keys(event: Pick<KeyboardEvent, 'key' | 'preventDefault'>) {
		const by: Record<string, [number, number]> = {
			ArrowLeft: [-NUDGE, 0],
			ArrowRight: [NUDGE, 0],
			ArrowUp: [0, -NUDGE],
			ArrowDown: [0, NUDGE]
		};
		const nudge = by[event.key];
		if (nudge) {
			event.preventDefault();
			placing.set(dragged(placing.get(), nudge[0], nudge[1], placing.frame(), placing.picture()));
			return;
		}
		if (event.key === '+' || event.key === '=') {
			event.preventDefault();
			placing.set(zoomed(placing.get(), 1.1));
		} else if (event.key === '-') {
			event.preventDefault();
			placing.set(zoomed(placing.get(), 1 / 1.1));
		}
	}

	return { down, move, up, wheel, keys, holding: () => touches.length > 0 };
}
