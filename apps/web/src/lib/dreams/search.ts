/**
 * Finding one dream in a list of them, by typing.
 *
 * The Seznam is the board as one column and the column gets long: a hundred
 * dreams is a hundred lines, and the line you want is the one you cannot
 * scroll to. So the list narrows as you type, against everything a dream
 * says about itself — its title, its why, its affirmation, the state it is
 * in, the area it belongs to and the year it is for — because the word you
 * remember about a dream is as often in the why as it is in the title.
 *
 * Typing is folded before it is compared: „strom“ finds *Strom*, and
 * „stesti“ finds *Štěstí*. Czech on a phone keyboard is Czech with the
 * diacritics left off about half the time, and a search that insists on them
 * is a search that answers „nic“ to a word that is on the screen.
 *
 * Words are an AND rather than a phrase — „dum les“ finds *Dům u lesa* —
 * because a person types the two words they remember, not the words in the
 * order they were written.
 *
 * All of it here rather than on the server (D49): the board is already in
 * memory and in the cache, so the list narrows on the keystroke and narrows
 * with no signal, and there is no endpoint to keep in step with the words
 * the screens use.
 */

import type { Dream } from '@aspire/contracts';
import { CATEGORY_LABEL, STATUS_BADGE } from './rules';

/**
 * From how many lines the Seznam offers a search field.
 *
 * Below this the whole list is on one screen, and a field for narrowing six
 * lines is one more thing to read on a screen whose job is to be the list.
 */
export const SEARCH_FROM = 6;

/**
 * Text as it is compared: lower case, and without the marks above the
 * letters. NFD splits „č“ into a c and a caron; the range is every combining
 * mark Unicode puts there, so it takes the caron, the acute and the ring
 * that Czech uses and leaves the letters.
 */
export function fold(text: string): string {
	return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/** What a dream is searched by: itself, in the words the screens show. */
export type Searchable = Pick<
	Dream,
	'title' | 'why' | 'affirmation' | 'status' | 'category' | 'targetYear'
>;

/**
 * Everything a dream says, folded into one string to look in. The status and
 * the area are the Czech words the list already shows — „splněno“ finds what
 * is done and „být“ finds that area — so what is searched is what is read.
 */
export function haystack(dream: Searchable): string {
	return fold(
		[
			dream.title,
			dream.why,
			dream.affirmation,
			STATUS_BADGE[dream.status],
			dream.category === null ? '' : CATEGORY_LABEL[dream.category],
			dream.targetYear === null ? '' : String(dream.targetYear)
		].join(' ')
	);
}

/** A query as the words it is made of, folded. Nothing typed is no words. */
export function terms(query: string): string[] {
	return fold(query)
		.split(/\s+/)
		.filter((term) => term.length > 0);
}

/**
 * The list, narrowed to what was typed, in the order it came in. An empty
 * query is the whole list and not a search at all, so it is given back
 * untouched rather than compared against.
 */
export function searchDreams<T extends Searchable>(dreams: T[], query: string): T[] {
	const wanted = terms(query);
	if (wanted.length === 0) return dreams;
	return dreams.filter((dream) => {
		const text = haystack(dream);
		return wanted.every((term) => text.includes(term));
	});
}
