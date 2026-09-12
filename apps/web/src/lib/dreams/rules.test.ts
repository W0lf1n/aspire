import { describe, expect, it } from 'vitest';
import { AFFIRMATION_MAX, TITLE_MAX, WHY_MAX, listLine, toFields, toInput } from './rules';

describe('toInput', () => {
	it('trims and reads a year', () => {
		expect(
			toInput({
				title: ' Dům u lesa ',
				why: ' Ticho. ',
				affirmation: ' Bydlím u lesa. ',
				status: 'dreaming',
				category: null,
				year: ' 2030 '
			})
		).toEqual({
			input: {
				title: 'Dům u lesa',
				why: 'Ticho.',
				affirmation: 'Bydlím u lesa.',
				status: 'dreaming',
				category: null,
				targetYear: 2030
			}
		});
	});

	it('lets the year and the affirmation be empty', () => {
		const read = toInput({
			title: 'Loď',
			why: '',
			affirmation: '',
			status: 'in-progress',
			category: null,
			year: ''
		});
		expect(read).toEqual({
			input: {
				title: 'Loď',
				why: '',
				affirmation: '',
				status: 'in-progress',
				category: null,
				targetYear: null
			}
		});
	});

	it('asks for a title', () => {
		expect(
			toInput({
				title: '   ',
				why: '',
				affirmation: '',
				status: 'dreaming',
				category: null,
				year: ''
			})
		).toEqual({
			problem: 'Napiš název.'
		});
	});

	it('holds the lengths the columns hold', () => {
		expect(
			toInput({
				title: 'a'.repeat(TITLE_MAX),
				why: 'b'.repeat(WHY_MAX),
				affirmation: 'c'.repeat(AFFIRMATION_MAX),
				status: 'dreaming',
				category: null,
				year: ''
			})
		).toHaveProperty('input');
		expect(
			toInput({
				title: 'a'.repeat(TITLE_MAX + 1),
				why: '',
				affirmation: '',
				status: 'dreaming',
				category: null,
				year: ''
			})
		).toEqual({
			problem: 'Název má nejvýš 120 znaků.'
		});
		expect(
			toInput({
				title: 'x',
				why: 'b'.repeat(WHY_MAX + 1),
				affirmation: '',
				status: 'dreaming',
				category: null,
				year: ''
			})
		).toEqual({
			problem: 'Proč má nejvýš 500 znaků.'
		});
		expect(
			toInput({
				title: 'x',
				why: '',
				affirmation: 'c'.repeat(AFFIRMATION_MAX + 1),
				status: 'dreaming',
				category: null,
				year: ''
			})
		).toEqual({
			problem: 'Afirmace má nejvýš 120 znaků.'
		});
	});

	it('wants a four-digit year in range', () => {
		expect(
			toInput({
				title: 'x',
				why: '',
				affirmation: '',
				status: 'dreaming',
				category: null,
				year: '30'
			})
		).toEqual({
			problem: 'Rok napiš čtyřmi číslicemi.'
		});
		expect(
			toInput({
				title: 'x',
				why: '',
				affirmation: '',
				status: 'dreaming',
				category: null,
				year: '1999'
			})
		).toEqual({
			problem: 'Rok napiš mezi 2000 a 2100.'
		});
	});
});

describe('toInput, the area', () => {
	it('carries one of the three, and none at all', () => {
		const fields = {
			title: 'Kjóto',
			why: '',
			affirmation: '',
			status: 'dreaming',
			year: ''
		} as const;

		expect(toInput({ ...fields, category: 'want' })).toEqual({
			input: {
				title: 'Kjóto',
				why: '',
				affirmation: '',
				status: 'dreaming',
				category: 'want',
				targetYear: null
			}
		});
		expect(toInput({ ...fields, category: null })).toHaveProperty('input.category', null);
	});
});

describe('toFields', () => {
	it('round-trips a saved dream', () => {
		const fields = toFields({
			title: 'Loď',
			why: 'Moře.',
			affirmation: 'Vyplouvám.',
			status: 'achieved',
			category: null,
			targetYear: null
		});
		expect(fields).toEqual({
			title: 'Loď',
			why: 'Moře.',
			affirmation: 'Vyplouvám.',
			status: 'achieved',
			category: null,
			year: ''
		});
		expect(toInput(fields)).toEqual({
			input: {
				title: 'Loď',
				why: 'Moře.',
				affirmation: 'Vyplouvám.',
				status: 'achieved',
				category: null,
				targetYear: null
			}
		});
	});
});

describe('listLine', () => {
	it('says the state, the area and the year, in that order', () => {
		expect(
			listLine({ status: 'in-progress', category: 'do', targetYear: 2030, focusRank: null })
		).toBe('plním · Dělat · 2030');
	});

	it('leaves out what a dream was never asked for', () => {
		expect(
			listLine({ status: 'dreaming', category: null, targetYear: null, focusRank: null })
		).toBe('sním');
		expect(
			listLine({ status: 'achieved', category: 'be', targetYear: null, focusRank: null })
		).toBe('splněno · Být');
		expect(
			listLine({ status: 'dreaming', category: null, targetYear: 2031, focusRank: null })
		).toBe('sním · 2031');
	});

	it('says „teď“ first when the dream is on the second reel', () => {
		// It leads because it is the only part of the line that says what is
		// being done about the dream now rather than what the dream is (D53).
		expect(
			listLine({ status: 'in-progress', category: 'do', targetYear: 2030, focusRank: 2 })
		).toBe('teď · plním · Dělat · 2030');
		expect(listLine({ status: 'dreaming', category: null, targetYear: null, focusRank: 1 })).toBe(
			'teď · sním'
		);
	});
});
