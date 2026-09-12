/**
 * Putting a photograph on a dream: the old one of its kind out, the new one
 * in, and the wait while the server makes its three sizes.
 *
 * It lives here rather than on a screen because two screens do it now — a
 * dream's own screen, where a photograph is replaced, and the reel, where a
 * dream that has none asks for one on the tile itself (D52). The order is the
 * part worth keeping in one place: the old photograph goes *first*, so an
 * upload that fails leaves the sky rather than the wrong picture, and only
 * its own kind goes, so changing the dreamt photograph never takes the proof
 * with it (D28).
 *
 * The API calls arrive as an argument. Not for the indirection — there is one
 * caller shape — but so the sequence has a test: what is ordered before what,
 * and what happens when the sizes never come, are the things that go wrong
 * here, and neither needs a browser to check.
 */

import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import { photoOf, photosToReplace } from './photos';

/**
 * How long to keep asking whether the sizes are ready. The worker takes about
 * a second; eight is a slow box under a queue of uploads, and past that the
 * dream is handed back as it stands — the row is there, the tile shows the
 * sky, and the next open of the board has the picture.
 */
export const READY_ATTEMPTS = 10;
export const READY_EVERY_MS = 800;

/** The three calls this needs, so the sequence can be tested without them. */
export interface PhotoApi {
	deleteImage(dreamId: string, imageId: string): Promise<void>;
	uploadImage(dreamId: string, photo: Blob, kind: DreamImageKind): Promise<DreamImage>;
	getDream(id: string): Promise<Dream>;
}

/** What the toast says when it worked, which depends on which picture it was. */
export function photographDone(kind: DreamImageKind): string {
	return kind === 'dreamt' ? 'Fotka je na nástěnce' : 'Skutečná fotka je u snu';
}

function sleep(ms: number): Promise<void> {
	return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * The dream with its new photograph, once the server has made the sizes — or
 * as it stands if they never arrived, so the caller always has something
 * truthful to put back in its state.
 *
 * Throws what the API threw: the screens turn that into a sentence.
 */
export async function replacePhotograph(
	dream: Dream,
	photo: Blob,
	kind: DreamImageKind,
	api: PhotoApi,
	wait: (ms: number) => Promise<void> = sleep
): Promise<Dream> {
	// Out before in, and only this kind (D28). A row still being resized goes
	// too, or it comes back as a second picture a moment later.
	for (const image of photosToReplace(dream, kind)) {
		await api.deleteImage(dream.id, image.id);
	}

	await api.uploadImage(dream.id, photo, kind);

	// The row exists from the upload and the WebPs follow (D23), so the dream
	// is asked for again until one of them is ready to point an `<img>` at.
	let latest = dream;
	for (let attempt = 0; attempt < READY_ATTEMPTS; attempt++) {
		await wait(READY_EVERY_MS);
		latest = await api.getDream(dream.id);
		if (photoOf(latest, kind)) break;
	}

	return latest;
}
