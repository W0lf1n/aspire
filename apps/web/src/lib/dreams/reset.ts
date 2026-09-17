/**
 * Starting over (D80).
 *
 * The one destructive thing in the app that cannot be taken back. Smazat
 * waits six seconds and offers „Vrátit“ (D65), and that works because it is
 * one dream: the request can be held. A whole board cannot be held in a
 * toast — a hundred dreams and their photographs are gone from the disk the
 * moment the server says yes — so this is the one place in the app where
 * friction is the point, and it is Prosper's friction, word for word: a
 * sentence typed out.
 *
 * A phrase rather than „jsi si jistý?“, because a confirm dialog is dismissed
 * by the same tap that opened it and by the second time that tap is muscle
 * memory. Thirteen characters cannot be. `ResetPhrase` on the server says the
 * same thing, so the request that empties a board takes the sentence as well
 * as the URL.
 */

import { fold } from './search';

/** Typed out to unlock the wipe. Shown on the screen in exactly this form. */
export const RESET_PHRASE = 'začínám znovu';

/**
 * Case and diacritics are folded and runs of whitespace collapse to one.
 *
 * The deliberateness this buys comes from typing thirteen characters, not
 * from finding „č“ on a phone keyboard at midnight — and a confirmation that
 * has to be attempted three times is one people stop reading.
 */
export function matchesResetPhrase(typed: string): boolean {
	return collapse(typed) === collapse(RESET_PHRASE);
}

function collapse(text: string): string {
	return fold(text).trim().replace(/\s+/g, ' ');
}

/**
 * How much there is to lose, said before it is lost: „Teď na ní je 10 snů.“
 * The verb moves with the noun — *je* one, *jsou* two to four, *je* five.
 */
export function boardHolds(dreams: number): string {
	if (dreams <= 0) return 'Teď na ní nic není.';
	if (dreams === 1) return 'Teď na ní je 1 sen.';
	return dreams < 5 ? `Teď na ní jsou ${dreams} sny.` : `Teď na ní je ${dreams} snů.`;
}

/**
 * What the toast says afterwards. Czech counts dreams three ways: one *sen*,
 * two to four *sny*, and five or more — and none — *snů*.
 */
export function resetDone(dreams: number): string {
	if (dreams <= 0) return 'Nástěnka už prázdná byla.';
	const noun = dreams === 1 ? 'sen' : dreams < 5 ? 'sny' : 'snů';
	const verb = dreams === 1 ? 'Smazán' : dreams < 5 ? 'Smazány' : 'Smazáno';
	return `${verb} ${dreams} ${noun}. Začínáš znovu.`;
}
