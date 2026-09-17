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
 *
 * And not all of them at all, unless the connection is free: the board asks
 * for a window of the reel and grows it as it is swiped, which is what
 * `ahead` and `cap` are for. `policy.ts` decides; this only does as it is
 * told (D39).
 */

import type { Dream } from '@aspire/contracts';
import { REEL_WINDOW, achievedDreams, reelOrder } from '$lib/dreams/board';
import {
	cellUrl,
	dreamtPhotos,
	photosOf,
	reelUrl,
	thisScreen,
	type Screen
} from '$lib/dreams/photos';

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
 * Every URL a tile shows, in the order it is worth having: for the dreamt
 * photograph, which is the reel's, the thumb that goes under the picture and
 * then the picture at the rung this screen reads (D62, D63) — the thumb
 * first, so the shape of a tile arrives before its pixels do; and the
 * achieved one at screen size, because the Síň slávy stands the two side by
 * side and half a pair is not proof of anything (D28).
 */
export function tileUrls(dreams: Dream[], screen: Screen = thisScreen()): string[] {
	const urls: string[] = [];
	for (const dream of dreams) {
		const { dreamt, achieved } = photosOf(dream);
		const cells = dreamtPhotos(dream);
		if (cells.length > 1) {
			// A collage: the cover's thumb, which the Seznam's circle shows, and
			// every cell at the one rung a cell reads (D82).
			urls.push(cells[0].thumbUrl, ...cells.map(cellUrl));
		} else if (dreamt) {
			urls.push(dreamt.thumbUrl, reelUrl(dreamt, screen)!);
		}
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

/**
 * The photographs worth having now: the tiles the reel has put in the
 * document, plus one screenful ahead of them, so the tile under the thumb is
 * never the one still arriving.
 *
 * It is the reel's own window (D30) and not a second idea of one — the board
 * grows `windowed` by `REEL_WINDOW` as it is swiped, and this grows with it:
 * five tiles on open asks for ten photographs, ten tiles asks for fifteen,
 * and a morning of ten swipes never pays for a hundred dreams (D39).
 */
export function aheadOf(reel: Dream[], windowed: number): Dream[] {
	return reel.slice(0, windowed + REEL_WINDOW);
}

/** What is cached but no longer on the board. */
export function stale(cached: string[], wanted: string[]): string[] {
	const keep = new Set(wanted);
	return cached.filter((url) => !keep.has(url));
}

/**
 * What to drop so the cache holds no more than `cap` photographs, oldest
 * first. `null` is no ceiling — the board is the ceiling, and `stale` is the
 * only thing that deletes.
 *
 * Oldest is oldest *fetched*: the Cache API hands back its keys in insertion
 * order, which is the only clock it has. For a reel that is shuffled every
 * open (D30) that is close enough to least-recently-wanted, and the window
 * fetched a moment ago is at the far end of the list, so a tile about to be
 * swiped to is never the one thrown away. A photograph dropped and met again
 * is fetched again; that is what a ceiling costs.
 */
export function overCap(cached: string[], cap: number | null): string[] {
	if (cap === null || cached.length <= cap) return [];
	return cached.slice(0, cached.length - cap);
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
 * Keep photographs on the device: fetch the ones asked for, forget the ones
 * the board no longer has, and stay under the ceiling.
 *
 * `ahead` is what to have now, in the order it is wanted — a window of the
 * reel, or the whole board when the connection is free (D39). `board` is
 * every dream there is, and it decides only what is *kept*: a photograph
 * already on the device is not deleted for being outside today's window,
 * or a shuffled reel would re-fetch the same pictures every morning.
 * `cap` bounds what windowing accumulates; `null` lets the board bound it.
 */
export async function rememberBoard(
	ahead: Dream[],
	board: Dream[],
	cap: number | null = null
): Promise<void> {
	if (typeof caches === 'undefined') return;
	try {
		const cache = await caches.open(MEDIA_CACHE);
		const screen = thisScreen();
		const wanted = tileUrls(ahead, screen);
		const belongs = tileUrls(board, screen);
		const paths = async () => (await cache.keys()).map((request) => new URL(request.url).pathname);
		const have = new Set(await paths());

		await pooled(
			wanted.filter((url) => !have.has(url)).map((url) => () => cache.add(url)),
			PREFETCH_AT_ONCE
		);

		// Asked again, in insertion order: what just arrived is at the end of
		// it, which is what makes the ceiling drop the oldest rather than the
		// tile about to be swiped to.
		const held = await paths();
		const forgotten = stale(held, belongs);
		const dropped = new Set(forgotten);
		const extra = overCap(
			held.filter((url) => !dropped.has(url)),
			cap
		);
		await Promise.all([...forgotten, ...extra].map((url) => cache.delete(url)));
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
