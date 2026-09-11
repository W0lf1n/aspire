import { describe, expect, it } from 'vitest';
import { settingsRows } from './settings';

describe('settingsRows', () => {
	it('summarises the theme in Czech', () => {
		const rows = settingsRows({ theme: 'dark', paired: false, nudge: 'off' });
		expect(rows.find((r) => r.id === 'vzhled')?.sub).toBe('tmavý');
	});

	it('says whether the device is paired', () => {
		const sub = (paired: boolean) =>
			settingsRows({ theme: 'system', paired, nudge: 'off' }).find((r) => r.id === 'parovani')?.sub;

		expect(sub(false)).toBe('zatím nespárováno');
		expect(sub(true)).toBe('spárováno');
	});

	it('offers the wallpaper, and says it needs a board first', () => {
		const row = (paired: boolean) =>
			settingsRows({ theme: 'system', paired, nudge: 'off' }).find((r) => r.id === 'tapeta');

		expect(row(true)?.sub).toBe('sny na zámek telefonu');
		expect(row(false)?.sub).toBe('až bude spárováno');
	});

	it('links every row under /nastaveni', () => {
		for (const row of settingsRows({ theme: 'light', paired: true, nudge: 'off' })) {
			expect(row.href.startsWith('/nastaveni/')).toBe(true);
		}
	});
});
