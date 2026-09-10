/**
 * Whether the server can be reached, as far as the app knows.
 *
 * The browser's own flag is a hint: `navigator.onLine` false means offline
 * for certain, true means a network exists, not that the server does. So
 * the flag is corrected by what actually happens: a request nobody answered,
 * or a board handed over from the cache, turns it off; any real answer turns
 * it back on. Offline, every write is locked (D24) and the screens say so.
 */

import { toast } from '$lib/ui/toast.svelte';

let online = $state(true);

export const connection = {
	get online() {
		return online;
	},
	set(value: boolean) {
		online = value;
	}
};

/** Follow the browser's flag, and say it once when the signal goes. */
export function watchConnection(): () => void {
	if (typeof window === 'undefined') return () => {};

	online = navigator.onLine;
	const up = () => {
		online = true;
	};
	const down = () => {
		online = false;
		toast.show('Bez připojení. Sny jsou z paměti, přidat teď nejde.');
	};

	window.addEventListener('online', up);
	window.addEventListener('offline', down);
	return () => {
		window.removeEventListener('online', up);
		window.removeEventListener('offline', down);
	};
}
