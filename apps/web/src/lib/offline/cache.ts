/**
 * The photographs the board needs when there is no network: the screen size
 * of every picture a tile shows, fetched ahead while there is a signal and
 * pruned to what is still on the board. The service worker owns the cache
 * and answers from it; this side only fills and trims it.
 */

import type { Dream } from '@aspire/contracts';
import { photosOf } from '$lib/dreams/photos';

/** The service worker's names for them; deleting both is forgetting the board. */
export const MEDIA_CACHE = 'aspire-media';
export const DATA_CACHE = 'aspire-board';

/**
 * Every URL a tile shows, at screen size: the dreamt photograph, which is
 * the reel's, and the achieved one, because the Síň slávy stands the two
 * side by side and half a pair is not proof of anything (D28).
 */
export function screenUrls(dreams: Dream[]): string[] {
	const urls: string[] = [];
	for (const dream of dreams) {
		const { dreamt, achieved } = photosOf(dream);
		if (dreamt) urls.push(dreamt.screenUrl);
		if (achieved) urls.push(achieved.screenUrl);
	}
	return urls;
}

/** What is cached but no longer on the board. */
export function stale(cached: string[], wanted: string[]): string[] {
	const keep = new Set(wanted);
	return cached.filter((url) => !keep.has(url));
}

/** Keep the board's photographs on the device, and nothing else. */
export async function rememberBoard(dreams: Dream[]): Promise<void> {
	if (typeof caches === 'undefined') return;
	try {
		const cache = await caches.open(MEDIA_CACHE);
		const wanted = screenUrls(dreams);
		const have = (await cache.keys()).map((request) => new URL(request.url).pathname);

		await Promise.all(
			wanted
				.filter((url) => !have.includes(url))
				.map((url) => cache.add(url).catch(() => undefined))
		);
		await Promise.all(stale(have, wanted).map((url) => cache.delete(url)));
	} catch {
		/* private mode or an old browser: the board still works with a signal */
	}
}

/** The board and its photographs, gone from the device. Unpairing does this. */
export async function forgetBoard(): Promise<void> {
	if (typeof caches === 'undefined') return;
	try {
		await caches.delete(MEDIA_CACHE);
		await caches.delete(DATA_CACHE);
	} catch {
		/* nothing to forget */
	}
}
