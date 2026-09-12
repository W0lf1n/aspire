import { describe, expect, it } from 'vitest';
import type { Dream } from '@aspire/contracts';
import { fold, haystack, searchDreams, terms } from './search';

function dream(over: Partial<Dream> = {}): Dream {
	return {
		id: 'a',
		title: 'Dům u lesa',
		why: 'Ticho a vlastní zahrada.',
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
		images: [],
		...over
	};
}

describe('fold', () => {
	it('takes the marks off and lowers the case', () => {
		expect(fold('Štěstí ŘŮŽ')).toBe('stesti ruz');
	});

	it('leaves a word that has none alone', () => {
		expect(fold('Barcelona 2027')).toBe('barcelona 2027');
	});
});

describe('terms', () => {
	it('is the words, folded', () => {
		expect(terms('  Dům   LESA ')).toEqual(['dum', 'lesa']);
	});

	it('is nothing when nothing is typed', () => {
		expect(terms('   ')).toEqual([]);
	});
});

describe('haystack', () => {
	it('holds the title, the why, the affirmation, the state, the area and the year', () => {
		const text = haystack(
			dream({
				title: 'Dům u lesa',
				why: 'Ticho.',
				affirmation: 'Bydlím u lesa.',
				status: 'achieved',
				category: 'be',
				targetYear: 2030
			})
		);
		expect(text).toContain('dum u lesa');
		expect(text).toContain('ticho');
		expect(text).toContain('bydlim u lesa');
		expect(text).toContain('splneno');
		expect(text).toContain('byt');
		expect(text).toContain('2030');
	});
});

describe('searchDreams', () => {
	/** One dream, asked whether it answers the query. */
	function asked(over: Partial<Dream>, query: string): boolean {
		return searchDreams([dream(over)], query).length === 1;
	}

	it('finds a word typed without its diacritics', () => {
		expect(asked({ title: 'Štěstí' }, 'stesti')).toBe(true);
	});

	it('wants every word, in any order', () => {
		expect(asked({ title: 'Dům u lesa' }, 'lesa dum')).toBe(true);
		expect(asked({ title: 'Dům u lesa' }, 'dum moře')).toBe(false);
	});

	it('finds a dream by the state it is in', () => {
		expect(asked({ status: 'achieved' }, 'splněno')).toBe(true);
		expect(asked({ status: 'dreaming' }, 'splněno')).toBe(false);
	});

	it('finds a dream by its area', () => {
		expect(asked({ category: 'do' }, 'dělat')).toBe(true);
		expect(asked({ category: 'want' }, 'dělat')).toBe(false);
	});

	it('finds the dreams that are on Teď', () => {
		// What is searched is what is read: the Seznam says „teď“ on those
		// lines, so typing it narrows to them (D53).
		expect(asked({ focusRank: 3 }, 'teď')).toBe(true);
		expect(asked({ focusRank: 3 }, 'ted')).toBe(true);
		expect(asked({ focusRank: null }, 'teď')).toBe(false);
	});

	it('finds a dream by a word in the why', () => {
		expect(asked({ why: 'Ticho a vlastní zahrada.' }, 'zahrada')).toBe(true);
	});
});

describe('searchDreams', () => {
	const rows = [
		dream({ id: 'a', title: 'Dům u lesa', why: '' }),
		dream({ id: 'b', title: 'Barcelona', why: 'Sagrada Família.', targetYear: 2027 }),
		dream({ id: 'c', title: 'Maraton', why: '', status: 'achieved' })
	];

	it('keeps the order it was given', () => {
		expect(searchDreams(rows, 'a').map((d) => d.id)).toEqual(['a', 'b', 'c']);
	});

	it('narrows to what was typed', () => {
		expect(searchDreams(rows, 'sagrada').map((d) => d.id)).toEqual(['b']);
	});

	it('finds by the year', () => {
		expect(searchDreams(rows, '2027').map((d) => d.id)).toEqual(['b']);
	});

	it('gives the list back untouched when nothing is typed', () => {
		expect(searchDreams(rows, '')).toBe(rows);
	});

	it('can find nothing', () => {
		expect(searchDreams(rows, 'plachetnice')).toEqual([]);
	});
});
