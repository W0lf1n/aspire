/// <reference types="@sveltejs/kit" />
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />

/**
 * Three caches, three rules (D24).
 *
 * The shell — everything the app needs to boot — is precached at install
 * under the build's own name and replaced whole on the next build. The
 * photographs live in a cache of their own that outlives builds: their URLs
 * are immutable, so a hit is the answer and the network is only for a miss.
 * The board's JSON is the third: the network first, always, because a cached
 * board must never pass for a fresh one; when the network fails, the last
 * board is handed over with a header that says so, and the app knows to
 * lock every write.
 *
 * Nothing else under `/api/` is cached, and no write ever is: offline, a
 * dream can be looked at and nothing more.
 */

import { build, files, prerendered, version } from '$service-worker';

const sw = self as unknown as ServiceWorkerGlobalScope;

const CACHE = `aspire-${version}`;
const MEDIA_CACHE = 'aspire-media';
const DATA_CACHE = 'aspire-board';
const KEPT = new Set([MEDIA_CACHE, DATA_CACHE]);
const PRECACHE = [...build, ...files, ...prerendered];

/** On a response served from the board cache, so the app can say "offline". */
const FROM_CACHE = 'x-aspire-cache';

sw.addEventListener('install', (event) => {
	event.waitUntil(
		caches
			.open(CACHE)
			.then((cache) => cache.addAll(PRECACHE))
			.then(() => sw.skipWaiting())
	);
});

sw.addEventListener('activate', (event) => {
	event.waitUntil(
		caches
			.keys()
			.then((keys) =>
				Promise.all(
					keys.filter((key) => key !== CACHE && !KEPT.has(key)).map((key) => caches.delete(key))
				)
			)
			.then(() => sw.clients.claim())
	);
});

sw.addEventListener('fetch', (event) => {
	const { request } = event;
	if (request.method !== 'GET') return;

	const url = new URL(request.url);
	if (url.origin !== sw.location.origin) return;

	if (url.pathname.startsWith('/media/')) {
		event.respondWith(respondMedia(request));
		return;
	}

	if (url.pathname.startsWith('/api/')) {
		// Only the board's reads have an offline answer; `/health` must never
		// bless a dead server, and every other call is the network's or nothing.
		if (url.pathname.startsWith('/api/v1/dreams')) event.respondWith(respondBoard(request));
		return;
	}

	event.respondWith(respond(request, url));
});

async function respond(request: Request, url: URL): Promise<Response> {
	const cache = await caches.open(CACHE);

	// Hashed build assets and static files never change under the same name.
	if (PRECACHE.includes(url.pathname)) {
		const hit = await cache.match(url.pathname);
		if (hit) return hit;
	}

	try {
		const response = await fetch(request);
		if (response.ok && response.type === 'basic') {
			cache.put(request, response.clone());
		}
		return response;
	} catch {
		const hit = await cache.match(request);
		if (hit) return hit;

		// A navigation with nothing cached for that exact URL still gets the app.
		if (request.mode === 'navigate') {
			const shell = await cache.match('/');
			if (shell) return shell;
		}

		return offline();
	}
}

/** A photograph's URL never changes its picture, so a hit is the answer. */
async function respondMedia(request: Request): Promise<Response> {
	const cache = await caches.open(MEDIA_CACHE);
	const hit = await cache.match(request);
	if (hit) return hit;

	try {
		const response = await fetch(request);
		if (response.ok) cache.put(request, response.clone());
		return response;
	} catch {
		return offline();
	}
}

/**
 * The network first. A 5xx counts as the network failing: on a laptop that
 * is Vite's proxy answering for an API that is not there, on the VPS it is
 * nginx answering for one. A 401 is the server saying this device is not
 * its any more, and the cached board goes with that.
 */
async function respondBoard(request: Request): Promise<Response> {
	const cache = await caches.open(DATA_CACHE);

	try {
		const response = await fetch(request);
		if (response.status >= 500) throw new Error(response.statusText);
		if (response.ok) cache.put(request, response.clone());
		else if (response.status === 401) await cache.delete(request);
		return response;
	} catch {
		const hit = await cache.match(request);
		if (hit) return marked(hit);
		return offline();
	}
}

function marked(hit: Response): Response {
	const headers = new Headers(hit.headers);
	headers.set(FROM_CACHE, 'hit');
	return new Response(hit.body, { status: hit.status, statusText: hit.statusText, headers });
}

function offline(): Response {
	return new Response('Offline', { status: 503, statusText: 'Offline' });
}
