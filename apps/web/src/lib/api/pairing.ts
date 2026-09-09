/**
 * Pairing — `POST /api/v1/pair`, and the token into storage.
 *
 * Prosper's flow without its address field. The client is always the same
 * origin as the API (D8), so there is nothing to type wrong but the code, and
 * no probe of `/health` to tell a wrong address from a wrong code: every way
 * this can fail arrives as a status, and each status has its sentence.
 *
 * There is no account, no password and no e-mail: one board, its own devices,
 * and a code that is typed once. Unpairing forgets the token here; the row on
 * the server stays until it is deleted there, which is what revocation is.
 */

import type { PairRequest } from '@aspire/contracts';
import { ApiError, pair } from './client';
import { clearToken, writeToken } from './token';

/** What a device is called when the name is left blank. */
export const DEFAULT_DEVICE_NAME = 'Telefon';

/** A failure with its sentence already written. */
class PairingError extends Error {}

/** The request as the server wants it: trimmed, and never nameless. */
export function pairingRequest(code: string, deviceName: string): PairRequest {
	return { code: code.trim(), deviceName: deviceName.trim() || DEFAULT_DEVICE_NAME };
}

/**
 * The sentence for a failed attempt. Only a status or a sentence written
 * here reaches the screen; a browser's own message is English and stays out.
 */
export function pairingError(e: unknown): string {
	if (e instanceof ApiError) {
		switch (e.status) {
			case 401:
			case 403:
				return 'Kód nesedí.';
			case 429:
				return 'Příliš mnoho pokusů za sebou. Chvíli počkej a zkus to znovu.';
			case 503:
				return 'Párování není na serveru nastavené.';
			default:
				return `Server odpověděl ${e.status}.`;
		}
	}
	// `fetch` rejects with a TypeError when nothing answered at all.
	if (e instanceof TypeError) return 'Server neodpovídá. Zkontroluj připojení.';
	if (e instanceof PairingError) return e.message;
	return 'Spárovat se nepodařilo.';
}

/** Trade the code for this device's token, and keep it. */
export async function pairDevice(code: string, deviceName: string): Promise<void> {
	const { token } = await pair(pairingRequest(code, deviceName));
	if (!token) throw new PairingError('Server nevrátil klíč.');
	writeToken(token);
}

/** Forget the token. The device row stays on the server until it is deleted there. */
export function unpairDevice(): void {
	clearToken();
}
