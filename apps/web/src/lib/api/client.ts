/**
 * The API, from the client's side. Same origin always: nginx proxies `/api/`
 * to the API in production and Vite proxies it in development, so there is
 * no server address to configure and no CORS to think about.
 */

import type {
	Dream,
	DreamImage,
	DreamInput,
	HealthResponse,
	PairRequest,
	PairResponse
} from '@aspire/contracts';
import { readToken } from './token';

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

async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
	const headers = new Headers(init.headers);
	const token = readToken();
	if (token) headers.set('Authorization', `Bearer ${token}`);
	// A FormData body writes its own boundary header; everything else is JSON.
	if (init.body && !(init.body instanceof FormData)) {
		headers.set('Content-Type', 'application/json');
	}

	const response = await fetch(`/api/v1${path}`, { ...init, headers });
	if (!response.ok)
		throw new ApiError(response.status, response.statusText, await detailOf(response));
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

/** The board, in board order. */
export function listDreams(): Promise<Dream[]> {
	return call<Dream[]>('/dreams');
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

/** The photograph, already downscaled on the device. 202: the sizes follow. */
export function uploadImage(dreamId: string, photo: Blob): Promise<DreamImage> {
	const body = new FormData();
	body.append('file', photo, 'photo.jpg');
	return call<DreamImage>(`/dreams/${dreamId}/images`, { method: 'POST', body });
}

export function deleteImage(dreamId: string, imageId: string): Promise<void> {
	return call<void>(`/dreams/${dreamId}/images/${imageId}`, { method: 'DELETE' });
}
