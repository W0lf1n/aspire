/**
 * A control that writes, and the one thing every one of them has to
 * remember: offline, every write is locked (D24, D67).
 *
 * It used to be remembered by hand — `disabled={busy || !connection.online}`
 * written out at each control, with each screen free to forget the second
 * half. `use:writes` is the same rule with a name: wear it instead of a
 * `disabled` of your own, and a control that writes cannot be built without
 * the lock on it.
 *
 * The screens' own sentences stay where they are. „Bez připojení. Seznam je
 * z paměti a nový sen počká na signál.“ says something true about the Seznam
 * and nothing about Upozornění; a lock is one rule everywhere, but what to
 * tell somebody about it is that screen's to say. The two sentences a *form*
 * shows are here, because a form has no room to say more than which verb it
 * cannot do.
 */

import { connection } from './status.svelte';

/** What a control that writes may not do right now, as a form says it. */
export const CANNOT = {
	add: 'Bez připojení se sen nedá přidat.',
	save: 'Bez připojení se sen nedá uložit.'
} as const;

/**
 * Whether a control that writes has to be dead: without a signal, always —
 * and otherwise only for a reason of its own, like a request already in
 * flight.
 */
export function isLocked(online: boolean, busy = false): boolean {
	return !online || busy;
}

/** The sentence a form shows, or nothing when it can be sent. */
export function cannot(online: boolean, verb: keyof typeof CANNOT): string {
	return online ? '' : CANNOT[verb];
}

/**
 * The action. `use:writes` on a control with nothing else to say, or
 * `use:writes={() => busy}` where the control has a reason of its own to be
 * dead — the two are or-ed, and the connection half is never the caller's to
 * forget.
 */
export function writes(
	node: HTMLButtonElement | HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement,
	busy?: () => boolean
) {
	$effect(() => {
		node.disabled = isLocked(connection.online, busy?.() ?? false);
	});
}
