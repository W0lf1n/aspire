import { describe, expect, it } from 'vitest';
import { RESET_PHRASE, boardHolds, matchesResetPhrase, resetDone } from './reset';

describe('matchesResetPhrase', () => {
	it('takes the phrase as it is printed', () => {
		expect(matchesResetPhrase(RESET_PHRASE)).toBe(true);
	});

	it('forgives case, accents and stray spaces', () => {
		for (const typed of [
			'Začínám znovu',
			'ZAČÍNÁM ZNOVU',
			'zacinam znovu',
			'  začínám   znovu  '
		]) {
			expect(matchesResetPhrase(typed)).toBe(true);
		}
	});

	it('refuses everything else', () => {
		for (const typed of ['', 'začínám', 'znovu začínám', 'začínámznovu', 'začínám znovu.', 'ano']) {
			expect(matchesResetPhrase(typed)).toBe(false);
		}
	});
});

describe('resetDone', () => {
	it('counts dreams the three ways Czech does', () => {
		expect(resetDone(1)).toBe('Smazán 1 sen. Začínáš znovu.');
		expect(resetDone(3)).toBe('Smazány 3 sny. Začínáš znovu.');
		expect(resetDone(5)).toBe('Smazáno 5 snů. Začínáš znovu.');
		expect(resetDone(112)).toBe('Smazáno 112 snů. Začínáš znovu.');
	});

	it('says so when there was nothing to take', () => {
		expect(resetDone(0)).toBe('Nástěnka už prázdná byla.');
	});
});

describe('boardHolds', () => {
	it('moves the verb with the noun', () => {
		expect(boardHolds(1)).toBe('Teď na ní je 1 sen.');
		expect(boardHolds(4)).toBe('Teď na ní jsou 4 sny.');
		expect(boardHolds(10)).toBe('Teď na ní je 10 snů.');
		expect(boardHolds(0)).toBe('Teď na ní nic není.');
	});
});
