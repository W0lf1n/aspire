import { describe, expect, it } from 'vitest';
import { settingsRows } from './settings';

describe('settingsRows', () => {
	it('summarises the theme in Czech', () => {
		const rows = settingsRows({ theme: 'dark', paired: false });
		expect(rows.find((r) => r.id === 'vzhled')?.sub).toBe('tmavý');
	});

	it('says whether the device is paired', () => {
		expect(settingsRows({ theme: 'system', paired: false })[1].sub).toBe('zatím nespárováno');
		expect(settingsRows({ theme: 'system', paired: true })[1].sub).toBe('spárováno');
	});

	it('links every row under /nastaveni', () => {
		for (const row of settingsRows({ theme: 'light', paired: true })) {
			expect(row.href.startsWith('/nastaveni/')).toBe(true);
		}
	});
});
