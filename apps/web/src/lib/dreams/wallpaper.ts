/**
 * The lock-screen collage, from this side (PLAN.md §3.5): which dreams can
 * be on one, how many, and how big the canvas the phone asks for is.
 *
 * The picture itself is the server's — it has the full-size files and
 * ImageSharp, and the device has neither. What is here is everything the
 * screen needs to decide before it asks.
 */

import type { Dream } from '@aspire/contracts';
import { photoOf } from './photos';

/**
 * The most that fit. Mirrors `CollageLayout.MaxPhotographs`: above six, each
 * dream is too small on a phone screen to be the one you recognise, and the
 * server refuses anyway.
 */
export const MAX_ON_WALLPAPER = 6;

/** The canvas the server will draw on, mirroring `CollageRenderer`. */
export const MIN_CANVAS_EDGE = 200;
export const MAX_CANVAS_EDGE = 4096;

/**
 * The dreams that can be on one: any with a dreamt photograph whose sizes
 * are ready, achieved or not. A dream you have already lived is exactly the
 * kind you want on a lock screen, so the wall is not left out of this.
 */
export function wallpaperCandidates(dreams: Dream[]): Dream[] {
	return dreams.filter((dream) => photoOf(dream, 'dreamt') !== null);
}

/**
 * The canvas for the phone holding it: its own screen in real pixels, so the
 * wallpaper is not scaled up by the phone afterwards. Clamped to what the
 * server will draw, and rounded, because `devicePixelRatio` is not always a
 * whole number and a canvas is a whole number of pixels.
 */
export function canvasFor(
	width: number,
	height: number,
	ratio: number
): {
	width: number;
	height: number;
} {
	return { width: edge(width * ratio), height: edge(height * ratio) };
}

function edge(value: number): number {
	if (!Number.isFinite(value) || value <= 0) return MIN_CANVAS_EDGE;
	return Math.min(MAX_CANVAS_EDGE, Math.max(MIN_CANVAS_EDGE, Math.round(value)));
}

/**
 * A dream added to the choice or taken out of it, in the order it was
 * chosen — which is the order it appears on the collage, so the person is
 * arranging it as they tap. Adding past the limit changes nothing: the
 * screen says how many are left rather than silently dropping the first.
 */
export function toggleChosen(chosen: string[], id: string): string[] {
	if (chosen.includes(id)) return chosen.filter((other) => other !== id);
	return chosen.length >= MAX_ON_WALLPAPER ? chosen : [...chosen, id];
}
