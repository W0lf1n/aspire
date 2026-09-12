/**
 * The lock-screen collage, from this side (PLAN.md §3.5): which dreams can
 * be on one, how many, and how big the canvas the phone asks for is.
 *
 * The picture itself is the server's — it has the full-size files and
 * ImageSharp, and the device has neither. What is here is everything the
 * screen needs to decide before it asks.
 */

import type { Dream } from '@aspire/contracts';
import { fuel, reelDreams } from './board';
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
 * The six the screen starts with: the day's dream first when it has a
 * photograph, then the dreams with the most fuel — how long each has waited,
 * weighted by its hearts, which is the same rule the daily pick reads (D58).
 *
 * A screen that opens on an empty choice asks a question nobody wants at that
 * moment: a lock screen is a thing you want to already be right. These six
 * are the board's own answer, and every one of them is one tap from being
 * swapped for another.
 *
 * The reel only, and only photographs whose sizes are ready. The wall is left
 * out here, though `wallpaperCandidates` keeps it for the choice by hand: what
 * is automatic should be what is still ahead, because that is what a lock
 * screen is for. The order is fixed — no shuffle — so the choice does not
 * rearrange itself under a thumb between two taps.
 */
export function wallpaperPick(
	dreams: Dream[],
	pickedId: string | null = null,
	count: number = MAX_ON_WALLPAPER,
	now: Date = new Date()
): Dream[] {
	const candidates = reelDreams(dreams).filter((dream) => photoOf(dream, 'dreamt') !== null);
	const picked = candidates.find((dream) => dream.id === pickedId) ?? null;

	const rest = candidates
		.filter((dream) => dream !== picked)
		.map((dream) => ({ dream, fuel: fuel(dream, now) }))
		// Equal first, so two dreams never shown — both of them `Infinity` —
		// fall to board order rather than to arithmetic that has no answer.
		.sort((a, b) => (a.fuel === b.fuel ? a.dream.sortOrder - b.dream.sortOrder : b.fuel - a.fuel))
		.map((ranked) => ranked.dream);

	return (picked ? [picked, ...rest] : rest).slice(0, Math.max(0, count));
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
 * The whole link the morning automation is given (D60): the board's path, and
 * this phone's canvas and day baked into it.
 *
 * Baked in because the link is static — whatever it carries is what will be
 * asked for every morning from now on, and there is nobody awake at 6:55 to
 * supply a screen size. The origin is the browser's own, which it knows for
 * certain; the server behind nginx would be reading its own scheme out of a
 * header somebody else can set.
 */
export function wallpaperLink(
	origin: string,
	path: string,
	canvas: { width: number; height: number },
	utcOffsetMinutes: number
): string {
	const query = new URLSearchParams({
		width: String(canvas.width),
		height: String(canvas.height),
		offset: String(Math.trunc(utcOffsetMinutes))
	});
	return `${origin}${path}?${query}`;
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
