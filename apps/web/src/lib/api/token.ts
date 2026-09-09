/**
 * The device's token, from pairing. Kept in localStorage: it has to survive
 * a closed tab and be readable before the first request, and there is no
 * IndexedDB layer in this app to keep it in. Revocation is deleting the row
 * on the server; forgetting is `clearToken()`.
 */

export const TOKEN_KEY = 'aspire.token';

export function readToken(): string | null {
	if (typeof localStorage === 'undefined') return null;
	try {
		return localStorage.getItem(TOKEN_KEY);
	} catch {
		return null;
	}
}

export function writeToken(token: string): void {
	try {
		localStorage.setItem(TOKEN_KEY, token);
	} catch {
		/* private mode: paired for the session only */
	}
}

export function clearToken(): void {
	try {
		localStorage.removeItem(TOKEN_KEY);
	} catch {
		/* nothing to forget */
	}
}
