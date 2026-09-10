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
	Dream,
	DreamImage,
	DreamImageKind,
	DreamInput,
	HealthResponse,
	PairRequest,
	PairResponse
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

export function deleteDream(id: string): Promise<void> {
	return call<void>(`/dreams/${id}`, { method: 'DELETE' });
}

/** One more on the heart; the dream comes back with its new count. */
export function likeDream(id: string): Promise<Dream> {
	return call<Dream>(`/dreams/${id}/likes`, { method: 'POST' });
}

/**
 * This dream was the board's first tile today. Nothing comes back: the stamp
 * is for tomorrow's pick and for the board's other devices (D25).
 */
export function markShown(id: string): Promise<void> {
	return call<void>(`/dreams/${id}/shown`, { method: 'POST' });
}

/**
 * The photograph, already downscaled on the device. 202: the sizes follow.
 * The kind says which of the two it is — the dreamt one by default, the
 * achieved one when the dream came true (D28).
 */
export function uploadImage(
	dreamId: string,
	photo: Blob,
	kind: DreamImageKind = 'dreamt'
): Promise<DreamImage> {
	const body = new FormData();
	body.append('file', photo, 'photo.jpg');
	return call<DreamImage>(`/dreams/${dreamId}/images?kind=${kind}`, { method: 'POST', body });
}

export function deleteImage(dreamId: string, imageId: string): Promise<void> {
	return call<void>(`/dreams/${dreamId}/images/${imageId}`, { method: 'DELETE' });
}
