/**
 * The photographs the board needs when there is no network: the screen size
 * of every picture a tile shows, fetched ahead while there is a signal and
 * pruned to what is still on the board. The service worker owns the cache
 * and answers from it; this side only fills and trims it.
 *
 * A few at a time, in the order the person will meet them (D37). A board is
 * meant to reach a hundred dreams (D30) and an achieved one has two
 * photographs (D28), so "all of them at once" is two hundred requests and
 * tens of megabytes started in the same millisecond, on whatever connection
 * the phone happens to be on.
 */

import type { Dream } from '@aspire/contracts';
import { achievedDreams, reelOrder } from '$lib/dreams/board';
import { photosOf } from '$lib/dreams/photos';

/** The service worker's names for them; deleting both is forgetting the board. */
export const MEDIA_CACHE = 'aspire-media';
export const DATA_CACHE = 'aspire-board';

/**
 * How many photographs are fetched at once.
 *
 * Four is a browser's own per-host limit for other requests to work around,
 * and enough that the first screenful is there in a moment; the rest arrive
 * behind it without the connection being handed two hundred requests to
 * sort out on its own.
 */
export const PREFETCH_AT_ONCE = 4;

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

/**
 * The board in the order its photographs are worth having: the reel as it
 * will actually be swiped — the day's pick first — and then the wall.
 *
 * It caches exactly what it always did; only the order changes. With four
 * fetches in flight that is the difference between the tile under the thumb
 * being ready at once and it being the hundredth request in the queue.
 */
export function prefetchOrder(dreams: Dream[], sequence: string[]): Dream[] {
	return [...reelOrder(dreams, sequence), ...achievedDreams(dreams)];
}

/** What is cached but no longer on the board. */
export function stale(cached: string[], wanted: string[]): string[] {
	const keep = new Set(wanted);
	return cached.filter((url) => !keep.has(url));
}

/**
 * Run tasks a few at a time, in order, never more than `limit` in flight.
 *
 * A task that fails does not take the pool with it: one photograph the
 * server will not give up is not a reason to stop fetching the rest, and
 * the board works with a signal either way.
 */
export async function pooled(tasks: (() => Promise<unknown>)[], limit: number): Promise<void> {
	let next = 0;

	const worker = async () => {
		while (next < tasks.length) {
			const task = tasks[next++];
			try {
				await task();
			} catch {
				/* the next one is still worth having */
			}
		}
	};

	await Promise.all(Array.from({ length: Math.min(limit, tasks.length) }, worker));
}

/**
 * Keep the board's photographs on the device, and nothing else.
 *
 * `dreams` is in the order they are wanted (`prefetchOrder`); what is cached
 * does not depend on that order, only when each arrives.
 */
export async function rememberBoard(dreams: Dream[]): Promise<void> {
	if (typeof caches === 'undefined') return;
	try {
		const cache = await caches.open(MEDIA_CACHE);
		const wanted = screenUrls(dreams);
		const have = new Set((await cache.keys()).map((request) => new URL(request.url).pathname));

		await pooled(
			wanted.filter((url) => !have.has(url)).map((url) => () => cache.add(url)),
			PREFETCH_AT_ONCE
		);
		await Promise.all(stale([...have], wanted).map((url) => cache.delete(url)));
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
