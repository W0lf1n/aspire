/**
 * A new build reaches the installed app on its own.
 *
 * The service worker precaches the shell and takes over the moment it is
 * installed, so the one question is *when the browser looks for it*. It
 * looks on a navigation into the worker's scope and at most once a day
 * otherwise — and an installed app brought back from the background performs
 * no navigation. So the app asks itself: every time it comes back into view,
 * and every time the network comes back, `registration.update()`.
 *
 * When a new worker has taken over, the page still running the old bundle
 * says so — in a toast that **stays until it is tapped**, and in the version
 * row of Nastavení, which is still there tomorrow. It also reloads by itself
 * on the next navigation, never in the middle of a screen.
 *
 * All three exist because an eight-second toast was once the only one of them
 * (D42): it was missed, and an installed PWA has no address bar to reload
 * from. `ready` is the flag the screens read.
 */

import { toast } from './toast.svelte';

let pending = $state(false);

export const update = {
	/** Whether a new build has taken over and this page is the old one. */
	get ready() {
		return pending;
	},

	/** Into the new build, now. What *Obnovit* does, wherever it is offered. */
	now(): void {
		pending = false;
		location.reload();
	},

	/**
	 * Ask for a new build and reload either way — **the reload an installed
	 * app has no address bar for** (D42).
	 *
	 * It is offered always rather than only when something is waiting,
	 * because a control that appears only once there is news cannot be
	 * reached: `applyUpdate` reloads on the next navigation, so navigating to
	 * a screen to press it is the same as pressing it. Asking first is what
	 * makes it more than a reload — a worker that has been sitting unasked
	 * since yesterday installs now, and the page comes back on the new build.
	 */
	async refresh(): Promise<void> {
		if (typeof navigator !== 'undefined' && 'serviceWorker' in navigator) {
			await navigator.serviceWorker
				.getRegistration()
				.then((registration) => registration?.update())
				.catch(() => {
					/* Offline, or the server is away: reload on what is cached. */
				});
		}
		update.now();
	}
};

/**
 * Reload into the new build, if one has taken over. Called from the layout
 * after every navigation, which is the one safe moment for it.
 */
export function applyUpdate(): void {
	if (!pending) return;
	update.now();
}

/**
 * Start asking for updates on every resume and reconnect, and announce a
 * worker that has taken over. Returns the teardown.
 */
export function watchUpdates(): () => void {
	if (typeof navigator === 'undefined' || !('serviceWorker' in navigator)) return () => {};
	const sw = navigator.serviceWorker;

	/* The very first install also fires `controllerchange` — the page went
	   from uncontrolled to controlled — and there is no older bundle to
	   leave behind in that case. */
	let hadController = sw.controller !== null;

	const check = () => {
		if (document.visibilityState !== 'visible') return;
		void sw
			.getRegistration()
			.then((registration) => registration?.update())
			.catch(() => {
				/* Offline, or the server is away: the next resume asks again. */
			});
	};

	const onController = () => {
		if (!hadController) {
			hadController = true;
			return;
		}
		pending = true;
		toast.show('Nová verze je připravená', {
			action: { label: 'Obnovit', run: () => update.now() },
			// Until it is tapped: the one thing it offers cannot be found again
			// once it has gone, and there is no reload button in an installed app.
			ms: 0
		});
	};

	document.addEventListener('visibilitychange', check);
	window.addEventListener('online', check);
	sw.addEventListener('controllerchange', onController);
	check();

	return () => {
		document.removeEventListener('visibilitychange', check);
		window.removeEventListener('online', check);
		sw.removeEventListener('controllerchange', onController);
	};
}
