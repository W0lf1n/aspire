/**
 * Theme preference.
 *
 * The one thing kept in localStorage besides the device token: it has to be
 * readable synchronously before first paint, so a dark launch never flashes
 * linen.
 */

export type Theme = 'system' | 'light' | 'dark';

/** The choice as a summary line, on the Nastavení hub. */
export const THEME_LABEL: Record<Theme, string> = {
	system: 'podle systému',
	light: 'světlý',
	dark: 'tmavý'
};

export const THEME_KEY = 'theme';

export function readTheme(): Theme {
	if (typeof localStorage === 'undefined') return 'system';
	try {
		const value = localStorage.getItem(THEME_KEY);
		return value === 'light' || value === 'dark' ? value : 'system';
	} catch {
		return 'system';
	}
}

export function applyTheme(theme: Theme): void {
	if (typeof document === 'undefined') return;
	const root = document.documentElement;
	if (theme === 'system') root.removeAttribute('data-theme');
	else root.setAttribute('data-theme', theme);
	syncThemeColor();

	try {
		if (theme === 'system') localStorage.removeItem(THEME_KEY);
		else localStorage.setItem(THEME_KEY, theme);
	} catch {
		/* private mode: the choice lasts the session */
	}
}

/**
 * The status bar follows the ground.
 *
 * `app.html` carries one `theme-color` per scheme, as literals, because a
 * meta tag cannot read a custom property — they are for the first paint,
 * before this runs. From then on every meta is pointed at the ground the
 * tokens resolved to, read off the root so the number is still written in
 * one place.
 */
export function syncThemeColor(): void {
	if (typeof document === 'undefined') return;
	const metas = document.querySelectorAll<HTMLMetaElement>('meta[name="theme-color"]');
	if (metas.length === 0) return;
	const ground = getComputedStyle(document.documentElement).getPropertyValue('--ground').trim();
	if (ground) metas.forEach((meta) => (meta.content = ground));
}
