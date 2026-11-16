/**
 * localStorage, one key at a time. It can be missing (no window) or throw
 * (private mode, storage blocked); either way a read is nothing and a write
 * lasts only as long as this open.
 */

export function stored(key: string): string | null {
	try {
		return typeof localStorage === 'undefined' ? null : localStorage.getItem(key);
	} catch {
		return null;
	}
}

/** Keeps a value, or forgets it on null. */
export function store(key: string, value: string | null): void {
	try {
		if (value === null) localStorage.removeItem(key);
		else localStorage.setItem(key, value);
	} catch {
		/* private mode: the value lasts this open */
	}
}
