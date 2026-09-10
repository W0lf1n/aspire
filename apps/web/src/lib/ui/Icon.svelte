<script lang="ts" module>
	/**
	 * The icon set.
	 *
	 * One 24-grid, drawn in Prosper's grammar: the chrome glyphs by hand, the
	 * rest a small subset of Lucide (ISC) copied in as paths rather than
	 * loaded from a CDN — the offline promise and the bundle budget both say
	 * so. Stroke 1.7 for chrome, round caps, round joins.
	 */
	const PATHS = {
		/* ── chrome ───────────────────────────────────────────────────── */
		'chevron-left': '<path d="M14.5 5.5 8 12l6.5 6.5"/>',
		'chevron-right': '<path d="M9.5 5.5 16 12l-6.5 6.5"/>',
		'chevron-down': '<path d="M5.5 9.5 12 16l6.5-6.5"/>',
		plus: '<path d="M12 6.4v11.2M6.4 12h11.2"/>',
		close: '<path d="m6.6 6.6 10.8 10.8M17.4 6.6 6.6 17.4"/>',
		check: '<path d="m5.6 12.4 4.4 4.4 8.4-9.6"/>',

		/** Nástěnka — a stack of two prints, the front one whole. */
		board:
			'<rect x="4" y="5.5" width="12.5" height="15" rx="2.6"/><path d="M9.2 3.6h8.2a2.6 2.6 0 0 1 2.6 2.6v9.3"/>',
		/** Síň slávy — a trophy (Lucide). */
		trophy:
			'<path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"/><path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"/><path d="M4 22h16"/><path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"/><path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"/><path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"/>',
		/** Nastavení — sliders, not a gear. Prosper's glyph, so the two apps share it. */
		settings:
			'<path d="M3.8 7.6h4.4M13.2 7.6h7M3.8 16.4h7.4M16.2 16.4h4"/><circle cx="10.7" cy="7.6" r="2.2"/><circle cx="13.7" cy="16.4" r="2.2"/>',

		/* ── the rooms of Nastavení, and the sources of a photo (Lucide) ── */
		'sun-moon':
			'<path d="M12 8a2.83 2.83 0 0 0 4 4 4 4 0 1 1-4-4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.9 4.9 1.4 1.4"/><path d="m17.7 17.7 1.4 1.4"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.3 17.7-1.4 1.4"/><path d="m19.1 4.9-1.4 1.4"/>',
		link: '<path d="M9 17H7A5 5 0 0 1 7 7h2"/><path d="M15 7h2a5 5 0 1 1 0 10h-2"/><path d="M8 12h8"/>',
		camera:
			'<path d="M14.5 4h-5L7 7H4a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-3l-2.5-3z"/><circle cx="12" cy="13" r="3"/>',
		image:
			'<rect width="18" height="18" x="3" y="3" rx="2" ry="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"/>',
		sparkles:
			'<path d="M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z"/><path d="M20 3v4"/><path d="M22 5h-4"/><path d="M4 17v2"/><path d="M5 18H3"/>',

		/* ── a dream's own screen (Lucide) ──────────────────────────────── */
		heart:
			'<path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/>',
		pencil:
			'<path d="M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z"/><path d="m15 5 4 4"/>'
	} as const;

	export type IconName = keyof typeof PATHS;

	export function isIconName(name: string): name is IconName {
		return Object.hasOwn(PATHS, name);
	}
</script>

<script lang="ts">
	interface Props {
		name: IconName;
		/** Rendered pixel size. The grid is 24, so anything else scales the stroke. */
		size?: number;
		/** Stroke on the 24 grid. Chrome runs at 1.7, a glyph in a circle at 2. */
		stroke?: number;
	}

	let { name, size = 22, stroke = 1.7 }: Props = $props();
</script>

<svg
	viewBox="0 0 24 24"
	width={size}
	height={size}
	fill="none"
	stroke="currentColor"
	stroke-width={stroke}
	stroke-linecap="round"
	stroke-linejoin="round"
	aria-hidden="true"
	focusable="false"
>
	<!-- eslint-disable-next-line svelte/no-at-html-tags -- a closed set of literals declared above -->
	{@html PATHS[name]}
</svg>

<style>
	svg {
		display: block;
		flex: none;
		shape-rendering: geometricPrecision;
	}
</style>
