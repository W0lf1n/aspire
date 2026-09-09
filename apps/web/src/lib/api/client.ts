/**
 * The API, from the client's side. Same origin always: nginx proxies `/api/`
 * to the API in production and Vite proxies it in development, so there is
 * no server address to configure and no CORS to think about.
 */

import type { Dream, HealthResponse, PairRequest, PairResponse } from '@aspire/contracts';
import { readToken } from './token';

export class ApiError extends Error {
	constructor(
		public readonly status: number,
		message: string
	) {
		super(message);
		this.name = 'ApiError';
	}
}

async function call<T>(path: string, init: RequestInit = {}): Promise<T> {
	const headers = new Headers(init.headers);
	const token = readToken();
	if (token) headers.set('Authorization', `Bearer ${token}`);
	if (init.body) headers.set('Content-Type', 'application/json');

	const response = await fetch(`/api/v1${path}`, { ...init, headers });
	if (!response.ok) throw new ApiError(response.status, response.statusText);
	return (await response.json()) as T;
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
