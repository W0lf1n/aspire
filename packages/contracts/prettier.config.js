/**
 * The same style as `apps/web`, because prettier resolves its configuration
 * from the file's own directory upwards and this package is outside that
 * one. Without this the repository's other TypeScript reformats to
 * prettier's defaults — double quotes, spaces — the moment the formatter is
 * pointed at it, and `pnpm lint` checks `apps/web` only, so nothing would
 * say so.
 */

/** @type {import("prettier").Config} */
const config = {
	useTabs: true,
	singleQuote: true,
	trailingComma: 'none',
	printWidth: 100
};

export default config;
