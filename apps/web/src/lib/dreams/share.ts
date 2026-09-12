/**
 * A dream shared with somebody who has nothing — no app, no code, no account
 * (PLAN.md §3.7, D61).
 *
 * The server holds the key and renders the page; what is here is the one rule
 * the client owns: what the whole link says. The origin is the browser's own,
 * which it knows for certain, and the path is the server's.
 */

/** The link as it is copied, read aloud and pasted into a message. */
export function shareUrl(origin: string, path: string): string {
	return `${origin}${path}`;
}

/**
 * What is sent with the link when the phone has a share sheet: the dream's own
 * name, so the message says what it is before the preview has loaded.
 *
 * No affirmation and no why. The page says the affirmation, because whoever
 * opens it has been sent it on purpose; a message's own text lands in a chat
 * list, on a lock screen, in a notification somebody else can be standing next
 * to — and that is a different audience from the one the link was sent to.
 */
export function shareText(title: string): string {
	return `Můj sen: ${title}`;
}
