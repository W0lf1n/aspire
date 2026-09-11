/**
 * Which shell caches a new build may throw away.
 *
 * The shell is precached under the build's own name, so every build brings a
 * cache and `activate` cleans up. It cleaned up too well: it deleted *every*
 * other shell, including the one a page still running the old bundle was
 * being served from. That page then asked for a route chunk whose hashed name
 * exists in neither the cache nor the new image, got a 404, and could not
 * finish a navigation — which is also the moment the app had chosen to reload
 * itself into the new build. The app was left with no way forward and no way
 * to refresh, which is exactly what happened on the first deploy after M5
 * (D42).
 *
 * So a new build keeps **one generation back**: its own shell, and the newest
 * of the others. A page from the build before last is already beyond saving,
 * and the photographs and the board are not shells and are never touched.
 */

/** `aspire-<build stamp>`, which is what SvelteKit's `version` is by default. */
const SHELL = /^aspire-(\d+)$/;

/**
 * The caches to delete: everything that is not the current shell, not one of
 * `kept`, and not the newest shell of those left over.
 *
 * A cache whose name carries no build stamp cannot be placed in time, so it
 * is not the one generation worth keeping — it goes.
 */
export function shellsToForget(keys: string[], current: string, kept: Iterable<string>): string[] {
	const spared = new Set([current, ...kept]);
	const others = keys.filter((key) => !spared.has(key));

	const stamped = others.filter((key) => SHELL.test(key));
	const rest = others.filter((key) => !SHELL.test(key));

	// Newest first, by the stamp in the name.
	stamped.sort((a, b) => stampOf(b) - stampOf(a));

	return [...stamped.slice(1), ...rest];
}

function stampOf(key: string): number {
	return Number(SHELL.exec(key)?.[1] ?? 0);
}
