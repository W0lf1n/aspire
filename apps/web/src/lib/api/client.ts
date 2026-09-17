/**
 * The API, from the client's side. Same origin always: nginx proxies `/api/`
 * to the API in production and Vite proxies it in development, so there is
 * no server address to configure and no CORS to think about.
 *
 * Every request also tells the connection flag what it learned: nobody
 * answering, or the service worker answering from its cache, means offline;
 * anything else means the server is there.
 */

import type {
	BoardResponse,
	Dream,
	DreamImage,
	DreamImageKind,
	DreamInput,
	FocalInput,
	HealthResponse,
	LinkResponse,
	NudgeInput,
	NudgeOffsetInput,
	NudgeSettings,
	PushKeyResponse,
	PairRequest,
	PairResponse,
	PhotoView,
	ResetResponse,
	ViewInput
} from '@aspire/contracts';
import { connection } from '$lib/offline/status.svelte';
import { readToken } from './token';

/** The service worker sets it on a board it served from the cache. */
const FROM_CACHE = 'x-aspire-cache';

export class ApiError extends Error {
	constructor(
		public readonly status: number,
		message: string,
		/** The server's own sentence, when it sent one: a problem's `detail`. */
		public readonly detail: string | null = null
	) {
		super(message);
		this.name = 'ApiError';
	}
}

async function send(path: string, init: RequestInit = {}): Promise<Response> {
	const headers = new Headers(init.headers);
	const token = readToken();
	if (token) headers.set('Authorization', `Bearer ${token}`);
	// A FormData body writes its own boundary header; everything else is JSON.
	if (init.body && !(init.body instanceof FormData)) {
		headers.set('Content-Type', 'application/json');
	}

	let response: Response;
	try {
		response = await fetch(`/api/v1${path}`, { ...init, headers });
	} catch (e) {
		connection.set(false);
		throw e;
	}

	// A 5xx is the proxy answering for a server that is not there: on a
	// laptop Vite's, on the VPS nginx's. Either way, not a server.
	connection.set(response.headers.get(FROM_CACHE) !== 'hit' && response.status < 500);
	if (!response.ok)
		throw new ApiError(response.status, response.statusText, await detailOf(response));
	return response;
}

async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
	const response = await send(path, init);
	if (response.status === 204) return undefined as T;
	return (await response.json()) as T;
}

/** The API's sentences travel as a problem's `detail`; anything else is noise. */
async function detailOf(response: Response): Promise<string | null> {
	if (!(response.headers.get('content-type') ?? '').includes('json')) return null;
	try {
		const body: unknown = await response.json();
		const detail = (body as { detail?: unknown } | null)?.detail;
		return typeof detail === 'string' && detail.length > 0 ? detail : null;
	} catch {
		return null;
	}
}

export function health(): Promise<HealthResponse> {
	return call<HealthResponse>('/health');
}

export function pair(request: PairRequest): Promise<PairResponse> {
	return call<PairResponse>('/pair', { method: 'POST', body: JSON.stringify(request) });
}

export interface BoardSnapshot {
	dreams: Dream[];
	/** The last board the device saw, because the server could not be asked. */
	fromCache: boolean;
}

/** The board, in board order, and whether it is the server's or the cache's. */
export async function listBoard(): Promise<BoardSnapshot> {
	const response = await send('/dreams');
	return {
		dreams: (await response.json()) as Dream[],
		fromCache: response.headers.get(FROM_CACHE) === 'hit'
	};
}

export function getDream(id: string): Promise<Dream> {
	return call<Dream>(`/dreams/${id}`);
}

export function createDream(input: DreamInput): Promise<Dream> {
	return call<Dream>('/dreams', { method: 'POST', body: JSON.stringify(input) });
}

export function updateDream(id: string, input: DreamInput): Promise<Dream> {
	return call<Dream>(`/dreams/${id}`, { method: 'PUT', body: JSON.stringify(input) });
}

export function deleteDream(id: string, keepalive = false): Promise<void> {
	// `keepalive` is for the deletion held past the screen it was asked on
	// (D65): the person switched away, the window closed, and the request has
	// to outlive the document rather than die with it.
	return call<void>(`/dreams/${id}`, { method: 'DELETE', keepalive });
}

/**
 * This dream to that line of the Seznam, counted from one (D79). Nothing
 * comes back: the screen has already moved the row, and `dreams/order.ts`
 * numbers the board the same way the server does.
 */
export function placeDream(id: string, place: number): Promise<void> {
	return call<void>(`/dreams/${id}/place`, { method: 'PUT', body: JSON.stringify({ place }) });
}

/** One more on the heart; the dream comes back with its new count. */
export function likeDream(id: string): Promise<Dream> {
	return call<Dream>(`/dreams/${id}/likes`, { method: 'POST' });
}

/**
 * This dream was the board's first tile today. Nothing comes back: the stamp
 * is for tomorrow's pick and for the board's other devices (D25).
 */
// ── Teď, the second reel (D53) ──────────────────────────────────────────────

/**
 * Put this dream on Teď, behind the ones already there. 409 when Teď is
 * full, which carries the sentence the screen says.
 */
export function addToFocus(id: string): Promise<Dream> {
	return call<Dream>(`/dreams/${id}/focus`, { method: 'POST' });
}

/** Take it off. Taking off what is already off is not a failure. */
export function removeFromFocus(id: string): Promise<void> {
	return call<void>(`/dreams/${id}/focus`, { method: 'DELETE' });
}

/**
 * The whole of Teď at once: these dreams, in this order, and nothing else on
 * it. One request rather than a move per arrow, so the ten are never
 * half-ordered on the server.
 */
export function saveFocus(dreamIds: string[]): Promise<Dream[]> {
	return call<Dream[]>('/focus', { method: 'PUT', body: JSON.stringify({ dreamIds }) });
}

export function markShown(id: string): Promise<void> {
	return call<void>(`/dreams/${id}/shown`, { method: 'POST' });
}

/**
 * The photograph, already downscaled on the device. 202: the sizes follow.
 * The kind says which of the two it is — the dreamt one by default, the
 * achieved one when the dream came true (D28).
 */
/**
 * The photograph, and where it is looked at (D54). The crop rides in the
 * query because a picture is positioned before it is sent: on the add screen
 * there is no dream yet to hang a second request on.
 */
export function uploadImage(
	dreamId: string,
	photo: Blob,
	kind: DreamImageKind = 'dreamt',
	focal?: FocalInput
): Promise<DreamImage> {
	const body = new FormData();
	body.append('file', photo, 'photo.jpg');

	const query = new URLSearchParams({ kind });
	if (focal?.focusX !== undefined) query.set('focusX', String(focal.focusX));
	if (focal?.focusY !== undefined) query.set('focusY', String(focal.focusY));
	if (focal?.zoom !== undefined) query.set('zoom', String(focal.zoom));
	if (focal?.fit !== undefined) query.set('fit', focal.fit);
	if (focal?.mat !== undefined) query.set('mat', focal.mat);

	return call<DreamImage>(`/dreams/${dreamId}/images?${query}`, { method: 'POST', body });
}

/**
 * The picture behind a link, as a JPEG this device can treat exactly like a
 * file somebody picked (D56).
 *
 * The server fetches it because the phone cannot: an image on another origin
 * is not readable by script, and a Pinterest page is not readable at all. It
 * comes back here rather than going straight onto a dream, so it takes the
 * same road as a picked file — the preview, the crop editor, the same upload
 * — and so that pasting a link before the dream exists works at all.
 */
export async function fetchImageFromUrl(url: string): Promise<Blob> {
	const response = await send(`/images/fetch?url=${encodeURIComponent(url.trim())}`);
	return response.blob();
}

/**
 * Where an existing photograph is looked at. The files on disk are untouched
 * — the crop is metadata, so this is instant and costs no resize.
 */
export function moveImage(
	dreamId: string,
	imageId: string,
	focal: FocalInput
): Promise<DreamImage> {
	return call<DreamImage>(`/dreams/${dreamId}/images/${imageId}`, {
		method: 'PUT',
		body: JSON.stringify(focal)
	});
}

/** The dreamt photographs in a new order; the first is the cover (D82). */
export function orderImages(dreamId: string, imageIds: string[]): Promise<Dream> {
	return call<Dream>(`/dreams/${dreamId}/images/order`, {
		method: 'PUT',
		body: JSON.stringify({ imageIds })
	});
}

/** Which of the three collage templates the dream's tile uses (D82). */
export function saveLayout(dreamId: string, layout: number): Promise<Dream> {
	return call<Dream>(`/dreams/${dreamId}/layout`, {
		method: 'PUT',
		body: JSON.stringify({ layout })
	});
}

/** The collage and each photograph, or the photographs alone (D87). */
export function saveView(dreamId: string, view: PhotoView): Promise<Dream> {
	return call<Dream>(`/dreams/${dreamId}/view`, {
		method: 'PUT',
		body: JSON.stringify({ view } satisfies ViewInput)
	});
}

/**
 * The lock-screen collage, as a JPEG (PLAN.md §3.5). The ids go in the order
 * they were chosen, which is the order they appear on it. Nothing is stored
 * on the server: the image is made from the files already there and handed
 * straight back, and where it goes next is the phone's business.
 *
 * With no ids at all the server answers with today's six, by the same rule
 * this device works out for itself (D59).
 */
export async function wallpaper(
	dreamIds: string[],
	canvas: { width: number; height: number }
): Promise<Blob> {
	const query = new URLSearchParams({
		dreams: dreamIds.join(','),
		width: String(canvas.width),
		height: String(canvas.height)
	});
	const response = await send(`/wallpaper?${query}`);
	return response.blob();
}

// ── one dream, shared with somebody who has nothing (D61) ───────────────────

/** This dream's share link, or `{ path: null }` when it has never been shared. */
export function dreamLink(id: string): Promise<LinkResponse> {
	return call<LinkResponse>(`/dreams/${id}/link`);
}

/** A new key, which also stops the old link opening anything. */
export function makeDreamLink(id: string): Promise<LinkResponse> {
	return call<LinkResponse>(`/dreams/${id}/link`, { method: 'POST' });
}

/** Unshared. Revoking a link that is not there is not a failure. */
export function revokeDreamLink(id: string): Promise<void> {
	return call<void>(`/dreams/${id}/link`, { method: 'DELETE' });
}

// ── the lock screen that refreshes itself (D60) ─────────────────────────────

/** The board's link, or `{ path: null }` when it has never made one. */
export function boardLink(): Promise<LinkResponse> {
	return call<LinkResponse>('/board/link');
}

/** A new key, which also stops the old link opening anything. */
export function makeBoardLink(): Promise<LinkResponse> {
	return call<LinkResponse>('/board/link', { method: 'POST' });
}

/** No link at all. Revoking one that is not there is not a failure. */
export function revokeBoardLink(): Promise<void> {
	return call<void>('/board/link', { method: 'DELETE' });
}

// ── what the board weighs (D64) ─────────────────────────────────────────────

/** How many photographs the board holds on the server, and what they weigh against the ceiling. */
export function boardUsage(): Promise<BoardResponse> {
	return call<BoardResponse>('/board');
}

/**
 * Starting over: every dream and photograph on the board (D80). The phrase
 * goes with it, because the server asks for the sentence as well as the URL.
 */
export function resetBoard(phrase: string): Promise<ResetResponse> {
	return call<ResetResponse>('/board/reset', { method: 'POST', body: JSON.stringify({ phrase }) });
}

// ── the morning nudge (PLAN.md §3.6) ────────────────────────────

/** The VAPID public key a browser needs before it can subscribe. */
export function nudgeKey(): Promise<PushKeyResponse> {
	return call<PushKeyResponse>('/nudge/key');
}

/** What this device's nudge is set to; off when it has never subscribed. */
export function readNudge(endpoint: string | null): Promise<NudgeSettings> {
	const query = endpoint ? `?endpoint=${encodeURIComponent(endpoint)}` : '';
	return call<NudgeSettings>(`/nudge${query}`);
}

/** Set it, or turn it off — `off` deletes the subscription entirely. */
export function saveNudge(input: NudgeInput): Promise<NudgeSettings> {
	return call<NudgeSettings>('/nudge', { method: 'PUT', body: JSON.stringify(input) });
}

/**
 * Where this device is, on a subscription it already has (D51). Its own verb,
 * so an open of the app cannot move the hour the person chose.
 */
export function saveNudgeOffset(input: NudgeOffsetInput): Promise<void> {
	return call<void>('/nudge/offset', { method: 'POST', body: JSON.stringify(input) });
}

export function deleteImage(dreamId: string, imageId: string): Promise<void> {
	return call<void>(`/dreams/${dreamId}/images/${imageId}`, { method: 'DELETE' });
}
