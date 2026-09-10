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
