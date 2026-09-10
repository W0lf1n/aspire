import { describe, expect, it } from 'vitest';
import { TITLE_MAX, WHY_MAX, toFields, toInput } from './rules';

describe('toInput', () => {
	it('trims and reads a year', () => {
		expect(
			toInput({ title: ' Dům u lesa ', why: ' Ticho. ', status: 'dreaming', year: ' 2030 ' })
		).toEqual({
			input: { title: 'Dům u lesa', why: 'Ticho.', status: 'dreaming', targetYear: 2030 }
		});
	});

	it('lets the year be empty', () => {
		const read = toInput({ title: 'Loď', why: '', status: 'in-progress', year: '' });
		expect(read).toEqual({
			input: { title: 'Loď', why: '', status: 'in-progress', targetYear: null }
		});
	});

	it('asks for a title', () => {
		expect(toInput({ title: '   ', why: '', status: 'dreaming', year: '' })).toEqual({
			problem: 'Napiš název.'
		});
	});

	it('holds the lengths the columns hold', () => {
		expect(
			toInput({
				title: 'a'.repeat(TITLE_MAX),
				why: 'b'.repeat(WHY_MAX),
				status: 'dreaming',
				year: ''
			})
		).toHaveProperty('input');
		expect(
			toInput({ title: 'a'.repeat(TITLE_MAX + 1), why: '', status: 'dreaming', year: '' })
		).toEqual({
			problem: 'Název má nejvýš 120 znaků.'
		});
		expect(
			toInput({ title: 'x', why: 'b'.repeat(WHY_MAX + 1), status: 'dreaming', year: '' })
		).toEqual({
			problem: 'Proč má nejvýš 500 znaků.'
		});
	});

	it('wants a four-digit year in range', () => {
		expect(toInput({ title: 'x', why: '', status: 'dreaming', year: '30' })).toEqual({
			problem: 'Rok napiš čtyřmi číslicemi.'
		});
		expect(toInput({ title: 'x', why: '', status: 'dreaming', year: '1999' })).toEqual({
			problem: 'Rok napiš mezi 2000 a 2100.'
		});
	});
});

describe('toFields', () => {
	it('round-trips a saved dream', () => {
		const fields = toFields({ title: 'Loď', why: 'Moře.', status: 'achieved', targetYear: null });
		expect(fields).toEqual({ title: 'Loď', why: 'Moře.', status: 'achieved', year: '' });
		expect(toInput(fields)).toEqual({
			input: { title: 'Loď', why: 'Moře.', status: 'achieved', targetYear: null }
		});
	});
});
