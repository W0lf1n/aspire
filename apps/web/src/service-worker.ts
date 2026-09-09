/// <reference types="@sveltejs/kit" />
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />

/**
 * App shell cache.
 *
 * Everything the app needs to boot is precached at install; the network is
 * only ever consulted for something the cache does not already hold. M2 adds
 * the second cache, the one for the screen-size images of every active dream,
 * which is what makes the board usable with the network permanently down.
 */

import { build, files, prerendered, version } from '$service-worker';

const sw = self as unknown as ServiceWorkerGlobalScope;

const CACHE = `aspire-${version}`;
const PRECACHE = [...build, ...files, ...prerendered];

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
				Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key)))
			)
			.then(() => sw.clients.claim())
	);
});

sw.addEventListener('fetch', (event) => {
	const { request } = event;
	if (request.method !== 'GET') return;

	const url = new URL(request.url);
	if (url.origin !== sw.location.origin) return;

	/**
	 * The API answers on this origin — that is the whole point of the
	 * deployment in `deploy/` — so "same origin" does not mean "an app asset".
	 * A cached `/api/v1/dreams` would show a stale board as a fresh one, and a
	 * cached `/api/v1/health` would bless a dead server. `/media/` is the
	 * images, and they get their own cache with their own rules in M2, not
	 * this one.
	 */
	if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/media/')) return;

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

		return new Response('Offline', { status: 503, statusText: 'Offline' });
	}
}
