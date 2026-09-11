/**
 * Whether a dream being written is one that is already written down.
 *
 * A dream arrives twice — once in the morning and once three weeks later,
 * phrased slightly differently — and the list quietly grows two of it. So
 * the form says so while the title is being typed. It never stops the save:
 * the same dream written twice is sometimes exactly what a person means,
 * and this is his list (D50). It tells him, and he decides.
 *
 * What counts as the same is deliberately generous about the phrasing and
 * mean about everything else: the check runs on a folded, punctuation-less
 * key, and a match is an equal key, one title standing whole inside another,
 * or a bigram similarity over `NEAR`. „Barcelona“ and „Barcelona 2027“ are
 * the same dream; „Naučit se španělsky“ and „Naučit se anglicky“ are not,
 * and a check that cannot tell those apart is a check that gets ignored.
 */

import type { Dream, DreamInput } from '@aspire/contracts';
import { fold } from './search';

/** How alike two titles have to read before the form mentions it. */
const NEAR = 0.72;

/**
 * How short a title may be and still count as contained in another.
 *
 * The containment is word for word rather than letter for letter — „les“ is
 * not inside „lesa“ — so three is safe where a substring would be absurd,
 * and three is what Czech needs: dům, byt, pes and auto are whole dreams
 * on their own, and a „Dům“ already written down is exactly what „Dům u
 * lesa“ should mention.
 */
const CONTAINED_MIN = 3;

/** How many of them a screen names. Past three it is a list, not a warning. */
export const SIMILAR_LIMIT = 3;

/** A title as it is compared: folded, and with only letters, digits and gaps. */
export function key(title: string): string {
	return fold(title)
		.replace(/[^\p{L}\p{N}]+/gu, ' ')
		.trim();
}

/**
 * Sørensen–Dice on character bigrams: how much of the two titles is the same
 * two letters in the same order. It is a word's-worth of tolerance — a
 * different ending, a missing word, a swapped case — without the O(n²) of an
 * edit distance, which matters because this runs on every keystroke against
 * the whole board.
 */
export function similarity(a: string, b: string): number {
	const left = key(a);
	const right = key(b);
	if (left.length === 0 || right.length === 0) return 0;
	if (left === right) return 1;

	const mine = bigrams(left);
	const theirs = bigrams(right);
	if (mine.length === 0 || theirs.length === 0) return 0;

	// Counted rather than set-wise, so a repeated pair is matched once only.
	const counts = new Map<string, number>();
	for (const pair of mine) counts.set(pair, (counts.get(pair) ?? 0) + 1);

	let shared = 0;
	for (const pair of theirs) {
		const spare = counts.get(pair) ?? 0;
		if (spare > 0) {
			shared += 1;
			counts.set(pair, spare - 1);
		}
	}

	return (2 * shared) / (mine.length + theirs.length);
}

/** Do these two titles read as the same dream? */
export function looksLikeSame(a: string, b: string): boolean {
	const left = key(a);
	const right = key(b);
	if (left.length === 0 || right.length === 0) return false;
	if (left === right) return true;
	if (contains(left, right) || contains(right, left)) return true;
	return similarity(left, right) >= NEAR;
}

/**
 * The dreams already written down that this one looks like, the most alike
 * first, at most `SIMILAR_LIMIT` of them.
 *
 * The affirmation counts too, and only when it is word for word: an
 * affirmation is a sentence a person says to themselves, and the same
 * sentence under two titles is one dream with two names.
 */
export function similarDreams(
	input: Pick<DreamInput, 'title' | 'affirmation'>,
	dreams: Dream[],
	exceptId: string | null = null
): Dream[] {
	if (key(input.title).length === 0) return [];

	const said = key(input.affirmation);

	return dreams
		.filter((dream) => dream.id !== exceptId)
		.filter(
			(dream) =>
				looksLikeSame(input.title, dream.title) ||
				(said.length > 0 && key(dream.affirmation) === said)
		)
		.map((dream) => ({ dream, score: similarity(input.title, dream.title) }))
		.sort((a, b) => b.score - a.score)
		.slice(0, SIMILAR_LIMIT)
		.map(({ dream }) => dream);
}

/** Does the longer title hold the shorter one whole, as words? */
function contains(long: string, short: string): boolean {
	if (short.length < CONTAINED_MIN || short.length >= long.length) return false;
	return ` ${long} `.includes(` ${short} `);
}

/** Every pair of neighbouring characters, spaces and all. */
function bigrams(text: string): string[] {
	const pairs: string[] = [];
	for (let i = 0; i < text.length - 1; i++) pairs.push(text.slice(i, i + 2));
	return pairs;
}
