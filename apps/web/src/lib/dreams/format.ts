/** Dates as the screen shows them: `10. září 2026`. */

const DATE = new Intl.DateTimeFormat('cs-CZ', { day: 'numeric', month: 'long', year: 'numeric' });

export function formatDate(iso: string): string {
	return DATE.format(new Date(iso));
}
