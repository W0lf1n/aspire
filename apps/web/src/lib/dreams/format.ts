/** Dates and the one sentence with a number in it, as the screen shows them. */

const DATE = new Intl.DateTimeFormat('cs-CZ', { day: 'numeric', month: 'long', year: 'numeric' });

/** `10. září 2026`. */
export function formatDate(iso: string): string {
	return DATE.format(new Date(iso));
}

/**
 * The anniversary line: „Před rokem se ti splnil sen ‚Dům u lesa‘.“
 *
 * The verb agrees with *sen*, not with the person, so the sentence is right
 * for whoever is holding the phone — a board is a pairing code, and the
 * server has never been told anybody's gender (D21). Czech wants `rokem`
 * for one and `lety` for every number above it.
 */
export function formatAnniversary(years: number, title: string): string {
	const when = years === 1 ? 'Před rokem' : `Před ${years} lety`;
	return `${when} se ti splnil sen „${title}“.`;
}
