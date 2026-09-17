/** Dates and the one sentence with a number in it, as the screen shows them. */

const DATE = new Intl.DateTimeFormat('cs-CZ', { day: 'numeric', month: 'long', year: 'numeric' });

/** `10. září 2026`. */
export function formatDate(iso: string): string {
	return DATE.format(new Date(iso));
}

/**
 * A date as something that happened lately: „dnes“, „včera“, and the long
 * date for everything older (D77). The day is the device's own, as the daily
 * pick's is — a dream changed at eleven at night was changed today, whatever
 * UTC thinks.
 */
export function formatWhen(iso: string, now: Date = new Date()): string {
	const then = new Date(iso);
	const days = Math.round((dayOf(now) - dayOf(then)) / 86_400_000);
	if (days === 0) return 'dnes';
	if (days === 1) return 'včera';
	return formatDate(iso);
}

/** The day a date falls in where the device is. `Date.UTC` has no summer time. */
function dayOf(date: Date): number {
	return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
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
