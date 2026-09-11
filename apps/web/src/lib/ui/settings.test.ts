import { describe, expect, it } from 'vitest';
import { settingsRows, type SettingsFacts } from './settings';

/** A device with nothing interesting about it; each test says its own part. */
function facts(over: Partial<SettingsFacts> = {}): SettingsFacts {
	return { theme: 'system', paired: false, nudge: 'off', offline: 'wifi', ...over };
}

describe('settingsRows', () => {
	it('summarises the theme in Czech', () => {
		const rows = settingsRows(facts({ theme: 'dark' }));
		expect(rows.find((r) => r.id === 'vzhled')?.sub).toBe('tmavý');
	});

	it('says whether the device is paired', () => {
		const sub = (paired: boolean) =>
			settingsRows(facts({ paired })).find((r) => r.id === 'parovani')?.sub;

		expect(sub(false)).toBe('zatím nespárováno');
		expect(sub(true)).toBe('spárováno');
	});

	it('offers the wallpaper, and says it needs a board first', () => {
		const row = (paired: boolean) => settingsRows(facts({ paired })).find((r) => r.id === 'tapeta');

		expect(row(true)?.sub).toBe('sny na zámek telefonu');
		expect(row(false)?.sub).toBe('až bude spárováno');
	});

	it('says how much of the board this device keeps (D39)', () => {
		const sub = (offline: SettingsFacts['offline']) =>
			settingsRows(facts({ offline })).find((r) => r.id === 'stahovani')?.sub;

		expect(sub('window')).toBe('jen co prolistuješ');
		expect(sub('wifi')).toBe('celá nástěnka na wifi');
		expect(sub('all')).toBe('vždy celá nástěnka');
	});

	it('links every row under /nastaveni', () => {
		for (const row of settingsRows(facts({ theme: 'light', paired: true }))) {
			expect(row.href.startsWith('/nastaveni/')).toBe(true);
		}
	});
});
