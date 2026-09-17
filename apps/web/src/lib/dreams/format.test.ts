import { describe, expect, it } from 'vitest';
import { formatAnniversary, formatDate, formatWhen } from './format';

describe('formatDate', () => {
	it('is the Czech long date', () => {
		expect(formatDate('2026-09-10T09:00:00Z')).toBe('10. září 2026');
	});
});

describe('formatAnniversary', () => {
	it('says rokem for one year and lety for the rest', () => {
		expect(formatAnniversary(1, 'Dům u lesa')).toBe('Před rokem se ti splnil sen „Dům u lesa“.');
		expect(formatAnniversary(2, 'Loď')).toBe('Před 2 lety se ti splnil sen „Loď“.');
		expect(formatAnniversary(11, 'Loď')).toBe('Před 11 lety se ti splnil sen „Loď“.');
	});
});

describe('formatWhen', () => {
	// Noon, so the day is the same one in any timezone a test runs in.
	const NOW = new Date('2026-09-17T12:00:00');

	it('says dnes and včera for the two days that have a name', () => {
		expect(formatWhen('2026-09-17T08:00:00', NOW)).toBe('dnes');
		expect(formatWhen('2026-09-16T23:30:00', NOW)).toBe('včera');
	});

	it('counts days on the calendar, not hours on the clock', () => {
		// Thirteen hours ago, and still yesterday.
		expect(formatWhen('2026-09-16T23:00:00', NOW)).toBe('včera');
	});

	it('is the long date for anything older', () => {
		expect(formatWhen('2026-09-10T12:00:00', NOW)).toBe('10. září 2026');
	});
});
