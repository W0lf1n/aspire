import { describe, expect, it } from 'vitest';
import { formatAnniversary, formatDate } from './format';

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
