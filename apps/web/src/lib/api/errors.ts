/**
 * The sentence for a failed request, for any screen that is not pairing.
 * Only a status or a sentence written here reaches the screen: a browser's
 * own message is English and stays out.
 */

import { ApiError } from './client';

export function describeError(e: unknown): string {
	if (e instanceof ApiError) {
		if (e.detail) return e.detail;
		switch (e.status) {
			case 401:
				return 'Zařízení není spárované.';
			case 404:
				return 'Tenhle sen už na nástěnce není.';
			default:
				return `Server odpověděl ${e.status}.`;
		}
	}
	// `fetch` rejects with a TypeError when nothing answered at all.
	if (e instanceof TypeError) return 'Server neodpovídá. Zkontroluj připojení.';
	return 'Nepodařilo se to.';
}
