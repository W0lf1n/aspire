/**
 * Putting a photograph on a dream: the old one of its kind out, the new one
 * in, and the wait while the server makes its three sizes.
 *
 * It lives here rather than on a screen because two screens do it now — a
 * dream's own screen, where a photograph is replaced or up to five are added
 * (D82, D84), and the reel, where a dream that has none asks for them on the
 * tile itself (D52). The order is the part worth keeping in one place: when a
 * photograph is replaced the new one goes in *first* and the old one comes out
 * only once it has, so an upload that fails costs nothing (D92), and only its
 * own kind comes out, so changing the dreamt photograph never takes the proof
 * with it (D28).
 *
 * The API calls arrive as an argument. Not for the indirection — there is one
 * caller shape — but so the sequence has a test: what is ordered before what,
 * and what happens when the sizes never come, are the things that go wrong
 * here, and neither needs a browser to check.
 */

import type { Dream, DreamImage, DreamImageKind, FocalInput } from '@aspire/contracts';
import { PHOTOS_MAX } from './collage';
import { photosToReplace } from './photos';

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
	uploadImage(
		dreamId: string,
		photo: Blob,
		kind: DreamImageKind,
		focal?: FocalInput
	): Promise<DreamImage>;
	getDream(id: string): Promise<Dream>;
}

/** What adding several photographs came to. */
export interface PhotographsAdded {
	/** The dream as it now stands, with every photograph that was sent. */
	dream: Dream;
	/** How many were sent before one was refused, or all of them. */
	sent: number;
	/** What the API threw at the one it refused; nothing after that one was sent. Null when none was. */
	failed: unknown;
}

/**
 * Photographs put on a dream in the order they were picked — one, or up to
 * five picked together (D82, D84). Nothing goes out first, which is the whole
 * difference from replacing, and the wait is for *these* photographs' sizes
 * rather than for any of their kind, because the dream may have had a ready
 * one before the upload began.
 *
 * Each is sent behind the last: the server stands a new photograph behind the
 * ones already there, so the first picked is the cover. Then there is one
 * wait for all of them rather than one each.
 *
 * `focal` is the first photograph's: the one a picker shows, and the only one
 * anybody could have placed before it was sent.
 *
 * A refusal halfway does not throw. What was sent is on the server and the
 * dream is handed back with it, so a screen never shows two photographs fewer
 * than the board has; `failed` is the reason the rest were not sent.
 */
export async function addPhotographs(
	dream: Dream,
	photos: Blob[],
	api: Pick<PhotoApi, 'uploadImage' | 'getDream'>,
	focal?: FocalInput,
	wait: (ms: number) => Promise<void> = sleep
): Promise<PhotographsAdded> {
	const added: string[] = [];
	let failed: unknown = null;

	for (const photo of photos) {
		try {
			const image = await api.uploadImage(
				dream.id,
				photo,
				'dreamt',
				added.length === 0 ? focal : undefined
			);
			added.push(image.id);
		} catch (e) {
			failed = e;
			break;
		}
	}

	if (added.length === 0) return { dream, sent: 0, failed };

	let latest = dream;
	for (let attempt = 0; attempt < READY_ATTEMPTS; attempt++) {
		await wait(READY_EVERY_MS);
		latest = await api.getDream(dream.id);
		const ready = new Set(latest.images.filter((image) => image.ready).map((image) => image.id));
		if (added.every((id) => ready.has(id))) break;
	}

	return { dream: latest, sent: added.length, failed };
}

/**
 * The files picked at once that a dream has room for, in the order they were
 * picked, and a sentence when some did not fit (D84). A phone's picker cannot
 * be told a number: seven can be chosen for a dream with five places, and the
 * first five of them is the answer rather than none.
 *
 * `count` is the dream's dreamt photographs, rows still being resized
 * included, because the server counts those against the five.
 */
export function roomFor<T>(picked: T[], count: number): { taken: T[]; note: string | null } {
	const room = Math.max(0, PHOTOS_MAX - count);
	const taken = picked.slice(0, room);
	return { taken, note: picked.length > room ? leftOver(room) : null };
}

/** Czech counts photographs three ways: one *fotka*, two to four *fotky*, five *fotek*. */
function leftOver(room: number): string {
	if (room === 0) return 'Ke snu se už žádná další fotka nevejde.';
	if (room === 1) return 'Ke snu se vejde ještě jedna fotka, beru první.';
	if (room < 5) return `Ke snu se vejdou ještě ${room} fotky, beru první ${room}.`;
	return `Ke snu se vejde nejvýš ${room} fotek, beru prvních ${room}.`;
}

/** What the toast says once photographs were added to a dream: one, or several. */
export function photographsAdded(sent: number): string {
	return sent > 1 ? 'Fotky jsou u snu' : 'Fotka je u snu';
}

/** What the toast says when it worked, which depends on which picture it was, and how many. */
export function photographDone(kind: DreamImageKind, count = 1): string {
	if (kind === 'achieved') return 'Skutečná fotka je u snu';
	return count > 1 ? 'Fotky jsou na nástěnce' : 'Fotka je na nástěnce';
}

function sleep(ms: number): Promise<void> {
	return new Promise((resolve) => setTimeout(resolve, ms));
}

/** What replacing a photograph came to. */
export interface PhotographReplaced {
	/** The dream as the server now has it. */
	dream: Dream;
	/**
	 * What the API threw when an old photograph would not come out. The new
	 * one is in regardless, so the dream has both until one is deleted by hand.
	 */
	failed: unknown;
}

/**
 * The dream with its new photograph, once the server has made the sizes — or
 * as it stands if they never arrived, so the caller always has something
 * truthful to put back in its state.
 *
 * In before out (D92): the old photographs of this kind are deleted only once
 * the new one is on the server, so a refused or interrupted upload leaves the
 * dream exactly as it was. Throws what the upload threw. A delete that fails
 * after it does not throw — the new photograph is there and the dream is handed
 * back with it — and `failed` is the reason the old one is still there.
 */
export async function replacePhotograph(
	dream: Dream,
	photo: Blob,
	kind: DreamImageKind,
	api: PhotoApi,
	/** Where the new photograph is looked at, chosen before it was sent (D54). */
	focal?: FocalInput,
	wait: (ms: number) => Promise<void> = sleep
): Promise<PhotographReplaced> {
	// Only this kind (D28). A row still being resized goes too, or it comes
	// back as a second picture a moment later.
	const old = photosToReplace(dream, kind);

	const added = await api.uploadImage(dream.id, photo, kind, focal);

	let failed: unknown = null;
	for (const image of old) {
		try {
			await api.deleteImage(dream.id, image.id);
		} catch (e) {
			failed = e;
			break;
		}
	}

	// The row exists from the upload and the WebPs follow (D23), so the dream
	// is asked for again until this photograph is ready to point an `<img>` at.
	let latest = dream;
	for (let attempt = 0; attempt < READY_ATTEMPTS; attempt++) {
		await wait(READY_EVERY_MS);
		latest = await api.getDream(dream.id);
		if (latest.images.some((image) => image.id === added.id && image.ready)) break;
	}

	return { dream: latest, failed };
}
