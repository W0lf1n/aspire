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
 *
 * **And whether it fills the frame at all** (D81). A photograph shown whole
 * is scaled to fit inside the frame rather than to cover it, stands on a mat,
 * and its zoom scales up from there. The point means the same thing in both:
 * along each axis the picture is placed at `p × (frame − picture)` — which is
 * `object-position`'s own rule, and is as true of room to spare as it is of
 * overhang. So one line of arithmetic moves a picture that hangs over its
 * frame and a picture that floats inside it; only the sign of the room
 * differs, and with it which way the point runs under a finger.
 */

import {
	PHOTO_FITS,
	PHOTO_MATS,
	type DreamImage,
	type FocalInput,
	type PhotoFit,
	type PhotoMat
} from '@aspire/contracts';

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
	/**
	 * 1 is as much of the picture as the frame can hold when it fills it, and
	 * all of the picture when it is shown whole.
	 */
	zoom: number;
	/** Filling the frame, or whole inside it (D81). */
	fit: PhotoFit;
	/** What is around it when it is whole. Kept while it fills, for next time. */
	mat: PhotoMat;
}

export const MIN_ZOOM = 1;

/**
 * As close as it goes. Past three the `screen` photograph — 1600 px on its
 * longest edge — is being stretched on a phone, and the point of this is a
 * better crop rather than a worse picture.
 */
export const MAX_ZOOM = 3;

/** What every photograph is until somebody moves it: the middle, all of it. */
export const CENTRED: Focal = { x: 0.5, y: 0.5, zoom: MIN_ZOOM, fit: 'fill', mat: 'night' };

/** Whether this is the crop a photograph has when nobody has touched it. */
export function isCentred(focal: Focal): boolean {
	return (
		focal.x === CENTRED.x &&
		focal.y === CENTRED.y &&
		focal.zoom === CENTRED.zoom &&
		focal.fit === CENTRED.fit
	);
}

/** Anything that arrived from anywhere, made into a focal point that works. */
export function sane(focal: Partial<Focal> | null | undefined): Focal {
	return {
		x: unit(focal?.x),
		y: unit(focal?.y),
		zoom: clamp(number(focal?.zoom, MIN_ZOOM), MIN_ZOOM, MAX_ZOOM),
		fit: oneOf(PHOTO_FITS, focal?.fit, CENTRED.fit),
		mat: oneOf(PHOTO_MATS, focal?.mat, CENTRED.mat)
	};
}

/** A saved photograph's crop, as the editor holds one. */
export function focalOf(
	image: Pick<DreamImage, 'focusX' | 'focusY' | 'zoom' | 'fit' | 'mat'> | null
): Focal {
	if (!image) return { ...CENTRED };
	return sane({
		x: image.focusX,
		y: image.focusY,
		zoom: image.zoom,
		fit: image.fit,
		mat: image.mat
	});
}

/**
 * A saved photograph's crop as a collage's cell shows it, which is how the
 * collage editor holds one (D85). A cell always fills (D82), and a photograph
 * shown whole is its point alone there — its zoom was chosen against all of
 * the picture (D81) — so that is what the editor starts from: what is on the
 * tile, not what the photograph would be on its own.
 */
export function cellFocal(
	image: Pick<DreamImage, 'focusX' | 'focusY' | 'zoom' | 'fit' | 'mat'> | null
): Focal {
	const focal = focalOf(image);
	return focal.fit === 'whole' ? { ...focal, fit: 'fill', zoom: MIN_ZOOM } : focal;
}

/** Whether two crops put the picture in the same place; the mat is not a place. */
export function samePlace(a: Focal, b: Focal): boolean {
	return a.x === b.x && a.y === b.y && a.zoom === b.zoom && a.fit === b.fit;
}

/** A crop as it is sent: all five, so the server never has to guess at one. */
export function toInput(focal: Focal): Required<FocalInput> {
	return { focusX: focal.x, focusY: focal.y, zoom: focal.zoom, fit: focal.fit, mat: focal.mat };
}

/**
 * How much room the frame has left over on each axis once the picture is in
 * it, in frame pixels — and it is signed. Filling, it is never above nought:
 * the picture hangs over, and this is `overflow` with a minus in front.
 * Whole, it is never below nought until the zoom makes it so: the picture
 * floats, and the room is the mat showing either side of it.
 */
export function room(frame: Size, picture: Size, zoom: number, fit: PhotoFit): Size {
	if (picture.width <= 0 || picture.height <= 0) return { width: 0, height: 0 };

	const across = frame.width / picture.width;
	const down = frame.height / picture.height;
	const base = fit === 'whole' ? Math.min(across, down) : Math.max(across, down);
	const scale = base * Math.max(MIN_ZOOM, zoom);

	return {
		width: tidy(frame.width - picture.width * scale),
		height: tidy(frame.height - picture.height * scale)
	};
}

/** A length that floating point left a hair off nought is nought. */
function tidy(value: number): number {
	return Math.abs(value) < 1e-6 ? 0 : value;
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
	// The picture sits at `p × room`, so a finger that moves it `d` pixels has
	// moved the point by `d / room`. Hanging over, the room is negative and
	// the point runs against the finger, as described above; floating on its
	// mat, the room is positive and the point runs with it. Nought is the
	// axis there is nothing to do along.
	const left = room(frame, picture, from.zoom, from.fit);
	return {
		...from,
		x: left.width === 0 ? from.x : unit(from.x + dx / left.width),
		y: left.height === 0 ? from.y : unit(from.y + dy / left.height)
	};
}

/**
 * Filling or whole, from the other. The point goes back to the middle and the
 * zoom back to one: both were chosen against a picture scaled some other way,
 * and a point that framed a face in the crop is nowhere in particular once
 * all of the picture is showing.
 */
export function fitted(from: Focal, fit: PhotoFit): Focal {
	return from.fit === fit ? from : { ...from, x: CENTRED.x, y: CENTRED.y, zoom: MIN_ZOOM, fit };
}

/**
 * Whether a photograph picked a moment ago should start out whole: when it is
 * wider than it is tall. Filling the reel — a phone screen — with a landscape
 * picture keeps a third of it and draws that at twice its pixels, which is
 * the soft, zoomed-in tile D81 exists to stop. A portrait fills well enough
 * to stay the default, and either can be turned over in the editor.
 */
export function startsWhole(picture: Size): boolean {
	return picture.height > 0 && picture.width / picture.height > 1.05;
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

function oneOf<T extends string>(all: readonly T[], value: unknown, fallback: T): T {
	return all.includes(value as T) ? (value as T) : fallback;
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
