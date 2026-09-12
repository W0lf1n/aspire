/**
 * Where a photograph is looked at, and how close (D54).
 *
 * Every surface in this app crops: the reel to the shape of a phone screen,
 * the Síň slávy to 4:5, the Seznam to a 40 px circle, the wallpaper to a cell.
 * Until now every one of them cropped from the centre, so a portrait with a
 * face near the top lost the face and there was nothing to be done about it.
 *
 * A photograph now carries three numbers instead of a cropped file: a point
 * to keep in view and how far in to go. They are metadata, so changing them
 * is instant, costs no resize, and is right for *every* shape at once — a
 * crop baked into the pixels would be made for one of those shapes and wrong
 * for the other three.
 *
 * `x` and `y` are `object-position` percentages, 0 to 1: 0 shows the left or
 * top edge, 1 the right or bottom, 0.5 the middle. That is the browser's own
 * meaning, so the client sets two custom properties and does no arithmetic at
 * render time, and `FocalCrop` on the server computes the same window from
 * the same two numbers.
 */

/** The frame something is shown in, or the picture being shown in it. */
export interface Size {
	width: number;
	height: number;
}

export interface Focal {
	/** 0 is the left edge, 1 the right. */
	x: number;
	/** 0 is the top edge, 1 the bottom. */
	y: number;
	/** 1 is as much of the picture as the frame can hold. */
	zoom: number;
}

export const MIN_ZOOM = 1;

/**
 * As close as it goes. Past three the `screen` photograph — 1600 px on its
 * longest edge — is being stretched on a phone, and the point of this is a
 * better crop rather than a worse picture.
 */
export const MAX_ZOOM = 3;

/** What every photograph is until somebody moves it: the middle, all of it. */
export const CENTRED: Focal = { x: 0.5, y: 0.5, zoom: MIN_ZOOM };

/** Whether this is the crop a photograph has when nobody has touched it. */
export function isCentred(focal: Focal): boolean {
	return focal.x === CENTRED.x && focal.y === CENTRED.y && focal.zoom === CENTRED.zoom;
}

/** Anything that arrived from anywhere, made into a focal point that works. */
export function sane(focal: Partial<Focal> | null | undefined): Focal {
	return {
		x: unit(focal?.x),
		y: unit(focal?.y),
		zoom: clamp(number(focal?.zoom, MIN_ZOOM), MIN_ZOOM, MAX_ZOOM)
	};
}

/**
 * How far the picture hangs over the frame, in frame pixels, on each axis.
 *
 * The picture is scaled to cover the frame and then by the zoom, so at zoom 1
 * exactly one axis overflows — the other is flush, and moving along it does
 * nothing. That zero is why `dragged` has to guard: dividing by it would send
 * the point to infinity on the axis there is nothing to see more of.
 */
export function overflow(frame: Size, picture: Size, zoom: number): Size {
	if (picture.width <= 0 || picture.height <= 0) return { width: 0, height: 0 };

	const cover = Math.max(frame.width / picture.width, frame.height / picture.height);
	const scale = cover * Math.max(MIN_ZOOM, zoom);
	return {
		width: Math.max(0, picture.width * scale - frame.width),
		height: Math.max(0, picture.height * scale - frame.height)
	};
}

/**
 * The point after the picture has been dragged by this many frame pixels.
 *
 * Dragging the picture to the right brings its left side into view, so `x`
 * goes down: the number is where in the picture the frame is looking, not
 * where the picture is. The whole drag is measured from where the finger went
 * down rather than added up frame by frame, so a gesture that hits an edge
 * and comes back ends where it should.
 */
export function dragged(from: Focal, dx: number, dy: number, frame: Size, picture: Size): Focal {
	const room = overflow(frame, picture, from.zoom);
	return {
		...from,
		x: room.width === 0 ? from.x : unit(from.x - dx / room.width),
		y: room.height === 0 ? from.y : unit(from.y - dy / room.height)
	};
}

/** Closer or further out, within the range, around the point already chosen. */
export function zoomed(from: Focal, by: number): Focal {
	const zoom = clamp(from.zoom * (Number.isFinite(by) && by > 0 ? by : 1), MIN_ZOOM, MAX_ZOOM);
	return { ...from, zoom };
}

/** The distance between two fingers, which is the whole of a pinch. */
export function spread(a: { x: number; y: number }, b: { x: number; y: number }): number {
	return Math.hypot(a.x - b.x, a.y - b.y);
}

function unit(value: unknown): number {
	return clamp(number(value, 0.5), 0, 1);
}

function number(value: unknown, fallback: number): number {
	return typeof value === 'number' && Number.isFinite(value) ? value : fallback;
}

function clamp(value: number, low: number, high: number): number {
	return Math.min(high, Math.max(low, value));
}
