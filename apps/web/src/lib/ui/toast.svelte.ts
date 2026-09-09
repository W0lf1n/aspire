/**
 * One transient message at a time, with an optional action on its right.
 * The one action in M0 is *Obnovit* on a new build (`update.ts`); an undo
 * arrives with the first thing that can be undone.
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
		timer = setTimeout(() => {
			if (current?.id === id) current = null;
		}, ms);
	},

	dismiss() {
		clearTimeout(timer);
		current = null;
	}
};
