/**
 * Which photograph a screen shows.
 *
 * A dream has two: the one that was dreamt, which is the board's picture,
 * and the one taken when it came true. The reel and the dream's tile show
 * the dreamt one; the Síň slávy stands the pair side by side, which is the
 * whole of the proof that it happened (D28).
 *
 * A photograph counts only once its sizes are ready — the row exists from
 * the upload and the WebPs follow a moment later — so a screen that reads
 * these never points an `<img>` at a file that is not there yet.
 */

import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';

/** The first ready photograph of a kind, or nothing. */
export function photoOf(dream: Pick<Dream, 'images'>, kind: DreamImageKind): DreamImage | null {
	return dream.images.find((image) => image.kind === kind && image.ready) ?? null;
}

/** Both of them at once, for a screen that shows the pair. */
export function photosOf(dream: Pick<Dream, 'images'>): {
	dreamt: DreamImage | null;
	achieved: DreamImage | null;
} {
	return { dreamt: photoOf(dream, 'dreamt'), achieved: photoOf(dream, 'achieved') };
}

/**
 * A dream's photographs of one kind, ready or not. What the screen deletes
 * before it puts a new one in that place: replacing the dreamt photograph
 * must not touch the achieved one, and a row still being resized has to go
 * too or it comes back as a second picture a moment later.
 */
export function photosToReplace(dream: Pick<Dream, 'images'>, kind: DreamImageKind): DreamImage[] {
	return dream.images.filter((image) => image.kind === kind);
}

/**
 * How an `<img>` shows this photograph: where it is looked at, and how close
 * (D54). One helper, so the reel, the wall, the Seznam's circle and the
 * picker all crop a photograph in the same place without any of them knowing
 * the arithmetic.
 *
 * `object-position` is the browser's own focal crop and needs no help. Zoom is
 * a `transform` on top of it, with its origin at the same point so the thing
 * being looked at stays where it is while the picture grows around it.
 *
 * A photograph nobody has moved gets no style at all rather than a style that
 * says „the middle, all of it“: the default is what `object-fit: cover`
 * already does, and an identity transform on five full-screen images is five
 * compositing layers bought for nothing.
 */
export function photoStyle(image: Pick<DreamImage, 'focusX' | 'focusY' | 'zoom'> | null): string {
	if (!image) return '';

	const x = percent(image.focusX);
	const y = percent(image.focusY);
	const zoom = Number.isFinite(image.zoom) ? Math.max(1, image.zoom) : 1;

	const position = x === 50 && y === 50 ? '' : `object-position:${x}% ${y}%;`;
	const scale = zoom === 1 ? '' : `transform:scale(${round(zoom)});transform-origin:${x}% ${y}%;`;
	return position + scale;
}

function percent(value: number): number {
	const unit = Number.isFinite(value) ? Math.min(1, Math.max(0, value)) : 0.5;
	return round(unit * 100);
}

/** Two decimals is finer than any screen can show and shorter than a float. */
function round(value: number): number {
	return Math.round(value * 100) / 100;
}
