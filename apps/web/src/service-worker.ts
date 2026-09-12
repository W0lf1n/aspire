/// <reference types="@sveltejs/kit" />
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />

/**
 * Three caches, three rules (D24).
 *
 * The shell — everything the app needs to boot — is precached at install
 * under the build's own name and replaced whole on the next build, which
 * keeps the one before it as well (D42). The
 * photographs live in a cache of their own that outlives builds: their URLs
 * are immutable, so a hit is the answer and the network is only for a miss.
 * The board's JSON is the third: the network first, always, because a cached
 * board must never pass for a fresh one; when the network fails, the last
 * board is handed over with a header that says so, and the app knows to
 * lock every write.
 *
 * Nothing else under `/api/` is cached, and no write ever is: offline, a
 * dream can be looked at and nothing more.
 *
 * It also shows the morning nudge (PLAN.md §3.6) and opens the dream it was
 * about when it is tapped — a push arrives when no page of this app is
 * running at all, so the notification is the worker's to show (D35).
 */

import { build, files, prerendered, version } from '$service-worker';
import { shellsToForget } from '$lib/offline/shell';

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

/**
 * The new shell takes over, and the build before it is kept one generation
 * longer (`shellsToForget`, D42): a page still running the old bundle is
 * served from that cache, and deleting it leaves the page unable to load a
 * route chunk — and so unable to reach the navigation it would have reloaded
 * on.
 */
sw.addEventListener('activate', (event) => {
	event.waitUntil(
		caches
			.keys()
			.then((keys) =>
				Promise.all(shellsToForget(keys, CACHE, KEPT).map((key) => caches.delete(key)))
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

	// A shared dream is the server's page, not this app (D61). Left to the
	// network entirely: caching it would hold a dream that has since been
	// unshared, and the navigation fallback below would answer somebody
	// else's link with the app's own shell.
	if (url.pathname === '/s' || url.pathname.startsWith('/s/')) return;

	if (url.pathname.startsWith('/api/')) {
		// Only the board's reads have an offline answer; `/health` must never
		// bless a dead server, and every other call is the network's or nothing.
		if (url.pathname.startsWith('/api/v1/dreams')) event.respondWith(respondBoard(request));
		return;
	}

	event.respondWith(respond(request, url));
});

/**
 * The morning nudge. The payload is a heading, a line under it, a photograph
 * and the dream to open — either today's dream with its affirmation, or an
 * anniversary, „Před rokem“ over „Splnil se ti sen …“ (D57). A push with no
 * body, or one this does not understand, still shows something rather than
 * nothing: a silent failed notification is worse than a plain one, because
 * the browser will show its own if we show none.
 */
sw.addEventListener('push', (event) => {
	event.waitUntil(show(event.data?.json()));
});

interface Nudge {
	/** The notification's own heading. */
	title?: string;
	/**
	 * The dream's title, which is where the heading came from until D57 — the
	 * server put a fixed word in `title` and nothing ever showed it. Read
	 * first, and absent from every payload since, so a nudge already encrypted
	 * by the old server when a deploy lands still says the dream's name rather
	 * than the word.
	 */
	dream?: string;
	line?: string | null;
	id?: string;
	image?: string | null;
}

async function show(payload: unknown): Promise<void> {
	const nudge = (payload ?? {}) as Nudge;

	await sw.registration.showNotification(nudge.dream ?? nudge.title ?? 'Aspire', {
		body: nudge.line ?? undefined,
		icon: '/icon-192.png',
		badge: '/icon-192.png',
		// The photograph itself where the platform shows one; where it does
		// not, nothing is lost but the picture.
		image: nudge.image ?? undefined,
		// One nudge a morning: a second replaces the first rather than
		// stacking under it.
		tag: 'aspire-nudge',
		data: { path: nudge.id ? `/sen/${nudge.id}` : '/' }
	} as NotificationOptions);
}

/**
 * Tapping it opens the dream it was about — in the tab that is already
 * there when one is, because a second copy of the app is not what anybody
 * wanted from a notification.
 */
sw.addEventListener('notificationclick', (event) => {
	event.notification.close();
	const path = (event.notification.data as { path?: string } | null)?.path ?? '/';

	event.waitUntil(
		(async () => {
			const open = await sw.clients.matchAll({ type: 'window', includeUncontrolled: true });
			for (const client of open) {
				if (new URL(client.url).origin !== sw.location.origin) continue;
				await client.navigate(path).catch(() => undefined);
				return client.focus().then(() => undefined);
			}

			await sw.clients.openWindow(path);
		})()
	);
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
