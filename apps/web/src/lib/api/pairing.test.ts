import { describe, expect, it } from 'vitest';
import { ApiError } from './client';
import { DEFAULT_DEVICE_NAME, pairingError, pairingRequest } from './pairing';

describe('pairingRequest', () => {
	it('trims the code and the name', () => {
		expect(pairingRequest(' 000000 ', ' Notebook ')).toEqual({
			code: '000000',
			deviceName: 'Notebook'
		});
	});

	it('names a nameless device', () => {
		expect(pairingRequest('000000', '   ').deviceName).toBe(DEFAULT_DEVICE_NAME);
	});
});

describe('pairingError', () => {
	it('says the code is wrong on 401', () => {
		expect(pairingError(new ApiError(401, 'Unauthorized'))).toBe('Kód nesedí.');
	});

	it('names the limiter on 429', () => {
		expect(pairingError(new ApiError(429, 'Too Many Requests'))).toMatch(/pokusů/);
	});

	it('repeats the server on an unset code', () => {
		expect(pairingError(new ApiError(503, 'Service Unavailable'))).toBe(
			'Párování není na serveru nastavené.'
		);
	});

	it('quotes any other status', () => {
		expect(pairingError(new ApiError(500, 'Internal Server Error'))).toBe('Server odpověděl 500.');
	});

	it('blames the connection when nothing answered', () => {
		expect(pairingError(new TypeError('Failed to fetch'))).toBe(
			'Server neodpovídá. Zkontroluj připojení.'
		);
	});

	it('never lets an English message through', () => {
		expect(pairingError(new SyntaxError('Unexpected token'))).toBe('Spárovat se nepodařilo.');
	});
});
