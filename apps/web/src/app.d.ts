// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		interface PageState {
			/** Párování, opened because the board took this device off it (D93). */
			disconnected?: boolean;
		}
		// interface Platform {}
	}
}

export {};
