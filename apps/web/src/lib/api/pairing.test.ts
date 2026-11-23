import { describe, expect, it, vi } from 'vitest';
import { ApiError } from './client';
import {
	DEFAULT_DEVICE_NAME,
	pairingError,
	pairingRequest,
	unpairDevice,
	wasDisconnected
} from './pairing';

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

describe('unpairDevice', () => {
	it('asks the server first and forgets only once it has answered', async () => {
		const order: string[] = [];

		await unpairDevice(
			async () => {
				order.push('server');
			},
			() => order.push('forget')
		);

		expect(order).toEqual(['server', 'forget']);
	});

	it('keeps the token when the server cannot be reached', async () => {
		const forget = vi.fn();

		await expect(
			unpairDevice(() => Promise.reject(new TypeError('Failed to fetch')), forget)
		).rejects.toThrow('Failed to fetch');
		expect(forget).not.toHaveBeenCalled();
	});

	it('forgets a device the server had already let go of', async () => {
		const forget = vi.fn();

		await unpairDevice(() => Promise.reject(new ApiError(401, 'Unauthorized')), forget);

		expect(forget).toHaveBeenCalledOnce();
	});
});

describe('wasDisconnected', () => {
	it('is a 401 to a device that still holds a token', () => {
		expect(wasDisconnected(new ApiError(401, 'Unauthorized'), 't')).toBe(true);
		expect(wasDisconnected(new ApiError(401, 'Unauthorized'), null)).toBe(false);
		expect(wasDisconnected(new ApiError(404, 'Not Found'), 't')).toBe(false);
		expect(wasDisconnected(new TypeError('Failed to fetch'), 't')).toBe(false);
	});
});
