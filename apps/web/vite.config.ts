import { defineConfig } from 'vitest/config';
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';

export default defineConfig({
	plugins: [
		sveltekit({
			compilerOptions: {
				// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},

			// Static output: an app shell plus assets, served by nginx in the
			// same container that proxies `/api/` to the API (deploy/).
			adapter: adapter({ fallback: '200.html', precompress: true })
		})
	],
	server: {
		// The client is always same-origin with the API. In development that
		// origin is Vite's, so `/api` is proxied to the API's dev port (see
		// apps/api/src/Aspire.Api/Properties/launchSettings.json) rather than
		// opening a CORS door.
		proxy: {
			'/api': 'http://127.0.0.1:5300',
			// The photographs: nginx's job in production, the API's on a laptop.
			'/media': 'http://127.0.0.1:5300',
			// A shared dream's page, which the API writes (D61). A regular
			// expression, because the plain prefix `/s` would swallow
			// `/seznam` — and nginx routes this to the API in production, so
			// the link the app shows has to work here too.
			'^/s/': 'http://127.0.0.1:5300'
		}
	},
	test: {
		expect: { requireAssertions: true },
		projects: [
			// The rules, in node: everything this app decides lives in a `.ts`
			// module a component imports, and this is where those are checked.
			{
				extends: './vite.config.ts',
				test: {
					name: 'server',
					environment: 'node',
					include: ['src/**/*.{test,spec}.{js,ts}'],
					exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			},
			// The components, in a DOM: what a screen actually renders from
			// those rules, and what a tap on it does (D68). `happy-dom` rather
			// than a real browser — the one dev dependency this buys, against
			// Playwright's download — because what is asked here is which
			// element is on the page, not how it paints.
			{
				extends: './vite.config.ts',
				// Svelte ships a server build and a browser one, and the server
				// build's `mount` only knows how to say it is not the browser.
				// This is what picks the other half.
				resolve: { conditions: ['browser'] },
				test: {
					name: 'client',
					environment: 'happy-dom',
					include: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			}
		]
	}
});
