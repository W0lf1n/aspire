/**
 * A dream on its way out, and the few seconds in which it is not gone yet
 * (D65).
 *
 * Smazat used to be one tap and no way back: the request went as the finger
 * lifted, and the server deletes the row and the photographs with it, so
 * there was nothing left to put back. The fix is not a dialog asking whether
 * he meant it — he nearly always did, and a dialog taxes every real deletion
 * to catch the rare wrong one. The fix is to **wait**: the request is held
 * for as long as the toast that announces it stands, and „Vrátit“ cancels it
 * before it is ever sent.
 *
 * So the dream is gone from every screen at once, and gone from the server a
 * few seconds later. That gap is the whole of this module: which ids the
 * screens must stop showing, and when the request finally goes.
 *
 * **The screens hide it, they do not forget it.** An id stays hidden after
 * its request has gone, because a board fetched before the deletion landed
 * still carries the dream, and a tile that flickers back for one frame reads
 * as a deletion that failed. Only „Vrátit“ takes an id off the list.
 *
 * **Leaving commits.** The window is for the finger that slipped while the
 * screen was being looked at. Anybody who closes the app or switches away
 * meant it, so the request goes then rather than being lost with the page —
 * `keepalive` is what lets it outlive the document.
 */

export const UNDO_MS = 6000;

/** What the screens must not show. Committed ids stay on it; „Vrátit“ takes one off. */
let hidden = $state<string[]>([]);

/** The one request being held, if any. */
let held: { id: string; send: () => Promise<void> } | null = null;
let timer: ReturnType<typeof setTimeout> | undefined;
let listening = false;

/** Leaving the page ends the window: what was held goes now (`keepalive`). */
function onLeave() {
	if (typeof document !== 'undefined' && document.visibilityState !== 'hidden') return;
	void commit();
}

function listen() {
	if (listening || typeof window === 'undefined') return;
	listening = true;
	window.addEventListener('pagehide', onLeave);
	document.addEventListener('visibilitychange', onLeave);
}

function deafen() {
	if (!listening || typeof window === 'undefined') return;
	listening = false;
	window.removeEventListener('pagehide', onLeave);
	document.removeEventListener('visibilitychange', onLeave);
}

/** Send what is held, now. Nothing to send is not a failure. */
async function commit(): Promise<void> {
	const going = held;
	clearTimeout(timer);
	timer = undefined;
	held = null;
	deafen();
	if (going) await going.send();
}

export const deleting = {
	/** Whether a screen must leave this dream out of its list. */
	has(id: string): boolean {
		return hidden.includes(id);
	},

	/** Everything hidden right now; a screen usually wants `has`. */
	get ids(): string[] {
		return hidden;
	},

	/**
	 * Hide it now and send `remove` when the window closes. A second deletion
	 * inside the first one's window sends the first: one is held at a time,
	 * and the toast that offers „Vrátit“ is also one at a time.
	 */
	hold(id: string, remove: () => Promise<void>, ms: number = UNDO_MS): void {
		void commit();
		if (!hidden.includes(id)) hidden = [...hidden, id];
		held = { id, send: remove };
		timer = setTimeout(() => void commit(), ms);
		listen();
	},

	/**
	 * Put it back on the screens: „Vrátit“ while the window is open, and the
	 * same thing when a request that did go came back a failure — the server
	 * still has the dream either way, so hiding it would be a lie.
	 */
	keep(id: string): void {
		hidden = hidden.filter((one) => one !== id);
		if (held?.id === id) {
			clearTimeout(timer);
			timer = undefined;
			held = null;
			deafen();
		}
	},

	/** Send what is held without waiting for the window to close. */
	flush(): Promise<void> {
		return commit();
	}
};
