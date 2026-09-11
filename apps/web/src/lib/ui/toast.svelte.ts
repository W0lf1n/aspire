/**
 * One transient message at a time, with an optional action on its right.
 * The one action in M0 is *Obnovit* on a new build (`update.svelte.ts`); an
 * undo arrives with the first thing that can be undone.
 *
 * `ms: 0` makes one that stays until it is tapped. It is for the message
 * whose action cannot be got back to once it is gone — a new build being
 * ready — and for nothing else: a toast that sits there is a toast in the
 * way, and tapping it anywhere puts it away (D42).
 */

export interface Toast {
	id: number;
	message: string;
	action?: ToastAction;
	ms: number;
}

export interface ToastAction {
	label: string;
	run: () => void | Promise<void>;
}

let current = $state<Toast | null>(null);
let timer: ReturnType<typeof setTimeout> | undefined;
let nextId = 0;

interface ShowOptions {
	action?: ToastAction;
	/** How long it stays; `0` is until it is tapped. */
	ms?: number;
}

export const toast = {
	get current() {
		return current;
	},

	show(message: string, options: ShowOptions = {}) {
		clearTimeout(timer);
		const id = ++nextId;
		const ms = options.ms ?? (options.action ? 6000 : 2600);
		current = { id, message, action: options.action, ms };
		if (ms > 0) {
			timer = setTimeout(() => {
				if (current?.id === id) current = null;
			}, ms);
		}
	},

	dismiss() {
		clearTimeout(timer);
		current = null;
	}
};
