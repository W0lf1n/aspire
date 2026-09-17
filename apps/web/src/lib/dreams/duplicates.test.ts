import { describe, expect, it } from 'vitest';
import type { Dream } from '@aspire/contracts';
import { key, looksLikeSame, similarDreams, similarity } from './duplicates';

function dream(id: string, over: Partial<Dream> = {}): Dream {
	return {
		id,
		title: id,
		why: '',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder: 0,
		focusRank: null,
		targetYear: null,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-09-01T00:00:00Z',
		updatedAt: '2026-09-01T00:00:00Z',
		images: [],
		...over
	};
}

describe('key', () => {
	it('folds, lowers and drops the punctuation', () => {
		expect(key('  „Dům“ u lesa! ')).toBe('dum u lesa');
	});

	it('is nothing for a title that is only punctuation', () => {
		expect(key(' … ')).toBe('');
	});
});

describe('similarity', () => {
	it('is one for the same title written differently', () => {
		expect(similarity('Dům u lesa', 'dum u  lesa!')).toBe(1);
	});

	it('is nothing against an empty title', () => {
		expect(similarity('Dům', '')).toBe(0);
	});

	it('is high for a title with a year added', () => {
		expect(similarity('Barcelona', 'Barcelona 2027')).toBeGreaterThan(0.72);
	});

	it('is low for two dreams that only start alike', () => {
		expect(similarity('Naučit se španělsky', 'Naučit se anglicky')).toBeLessThan(0.72);
	});
});

describe('looksLikeSame', () => {
	it('sees through the diacritics and the punctuation', () => {
		expect(looksLikeSame('Dům u lesa', 'dum u lesa')).toBe(true);
	});

	it('sees a whole title standing inside another', () => {
		expect(looksLikeSame('Dům u lesa', 'Dům')).toBe(true);
	});

	it('sees a short whole word too, because Czech dreams are short', () => {
		expect(looksLikeSame('Koupit byt', 'Byt')).toBe(true);
	});

	it('does not call a fragment of a word a duplicate', () => {
		expect(looksLikeSame('Dům u lesa', 'les')).toBe(false);
		expect(looksLikeSame('Koupit lyže', 'ly')).toBe(false);
	});

	it('keeps two different things apart', () => {
		expect(looksLikeSame('Koupit dům', 'Koupit byt')).toBe(false);
		expect(looksLikeSame('Naučit se španělsky', 'Naučit se anglicky')).toBe(false);
	});

	it('is false against nothing', () => {
		expect(looksLikeSame('', 'Dům')).toBe(false);
	});
});

describe('similarDreams', () => {
	const board = [
		dream('a', { title: 'Dům u lesa' }),
		dream('b', { title: 'Barcelona', affirmation: 'Bydlím u moře.' }),
		dream('c', { title: 'Maraton pod čtyři hodiny' })
	];

	it('says nothing while there is no title yet', () => {
		expect(similarDreams({ title: '  ', affirmation: '' }, board)).toEqual([]);
	});

	it('says nothing about a dream that is new', () => {
		expect(similarDreams({ title: 'Plachetnice', affirmation: '' }, board)).toEqual([]);
	});

	it('finds the one already written down', () => {
		expect(similarDreams({ title: 'dum u lesa', affirmation: '' }, board).map((d) => d.id)).toEqual(
			['a']
		);
	});

	it('finds it by the affirmation alone', () => {
		expect(
			similarDreams({ title: 'Byt v Barceloně', affirmation: 'Bydlím u moře.' }, board).map(
				(d) => d.id
			)
		).toEqual(['b']);
	});

	it('leaves the dream being edited out of its own answer', () => {
		expect(similarDreams({ title: 'Dům u lesa', affirmation: '' }, board, 'a')).toEqual([]);
	});

	it('names the most alike first, and at most three', () => {
		const many = [
			dream('far', { title: 'Barcelona 2027' }),
			dream('near', { title: 'Barcelona' }),
			dream('also', { title: 'Barcelona na jaře' }),
			dream('fourth', { title: 'Barcelona s dětmi' })
		];
		const found = similarDreams({ title: 'Barcelona', affirmation: '' }, many);
		expect(found).toHaveLength(3);
		expect(found[0].id).toBe('near');
	});
});
