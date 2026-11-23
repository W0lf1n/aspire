/**
 * The device's token, from pairing. Kept in localStorage: it has to survive
 * a closed tab and be readable before the first request, and there is no
 * IndexedDB layer in this app to keep it in. Revocation is deleting the row
 * on the server (D93); forgetting is `clearToken()`.
 */

import { store, stored } from '$lib/stored';

export const TOKEN_KEY = 'aspire.token';

export const readToken = (): string | null => stored(TOKEN_KEY);

export const writeToken = (token: string): void => store(TOKEN_KEY, token);

export const clearToken = (): void => store(TOKEN_KEY, null);
