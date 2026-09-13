<script lang="ts">
	/**
	 * The bottom navigation: Nástěnka · Seznam · ⊕ · Síň slávy · Nastavení.
	 *
	 * Five slots, and the disc takes the middle one: it is the one
	 * accent-coloured thing on the bar and it opens Přidat — the screen with
	 * the photograph on it — so it sits where the bar is symmetrical about it,
	 * two tabs either side. The four tabs are peers. Anything under
	 * `/nastaveni` keeps Nastavení lit, which is how its pages count.
	 *
	 * A frosted pill floating over the page's bottom edge — the page scrolls
	 * under it. 62 px tall, 16 px in from each side, 24 px off the bottom (or
	 * clear of the home indicator), glass with the glass shadow. `.page` in
	 * `app.css` reserves the room (`--page-end`), so the last row still scrolls
	 * clear of it. Prosper's bar bends what scrolls under it; this one is
	 * frosted, rimmed and lensed without the bend, which is the part a
	 * photograph does not need.
	 */
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import Icon from './Icon.svelte';
	import { connection } from '$lib/offline/status.svelte';
	import { TABS, activeTab } from './nav';

	const here = $derived(page.url.pathname);
	const active = $derived(activeTab(here));

	/** The slot the lens sits in — the disc takes the middle one. */
	const SLOT: Record<string, number> = { board: 0, list: 1, hall: 3, settings: 4 };
	const lensSlot = $derived(active ? SLOT[active] : null);
</script>

<nav class="tabbar" aria-label="Hlavní navigace">
	{#if lensSlot !== null && lensSlot !== undefined}
		<span class="lens" style:--slot={lensSlot} aria-hidden="true"></span>
	{/if}

	{#each TABS.slice(0, 2) as tab (tab.id)}
		<a
			class="tab"
			class:tab--on={active === tab.id}
			href={resolve(tab.path)}
			aria-current={active === tab.id ? 'page' : undefined}
		>
			<Icon name={tab.icon} size={24} stroke={1.7} />
			<span class="tab__label">{tab.label}</span>
		</a>
	{/each}

	<!-- Offline, nothing can be added, and the disc says so by resting. -->
	<a
		class="add"
		class:add--off={!connection.online}
		href={resolve('/pridat')}
		aria-label="Přidat sen"
		aria-disabled={!connection.online}
	>
		<span class="add__disc"><Icon name="plus" size={26} stroke={2.2} /></span>
	</a>

	{#each TABS.slice(2) as tab (tab.id)}
		<a
			class="tab"
			class:tab--on={active === tab.id}
			href={resolve(tab.path)}
			aria-current={active === tab.id ? 'page' : undefined}
		>
			<Icon name={tab.icon} size={24} stroke={1.7} />
			<span class="tab__label">{tab.label}</span>
		</a>
	{/each}
</nav>

<style>
	.tabbar {
		/* One number the slots are worked out from: the columns, the lens's
		   width and where it slides to are all this. */
		--slots: 5;

		position: absolute;
		left: var(--space-4);
		right: var(--space-4);
		bottom: var(--tabbar-lift);
		z-index: var(--z-nav);
		display: grid;
		grid-template-columns: repeat(var(--slots), 1fr);
		align-items: center;
		height: var(--tabbar);
		padding: 0 var(--space-2);
		border-radius: var(--radius-full);
		background: var(--glass);
		box-shadow: var(--elev-glass);
		-webkit-backdrop-filter: var(--glass-blur-bar);
		backdrop-filter: var(--glass-blur-bar);
		isolation: isolate;
	}

	/* Without backdrop blur the glass has nothing to frost; go opaque. */
	@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
		.tabbar {
			background: var(--surface);
		}
	}

	/* The rim: a 1 px ring, lit from the top left. A gradient masked down to
	   the ring rather than a border, so it can change colour around the edge. */
	.tabbar::before,
	.lens::before {
		content: '';
		position: absolute;
		inset: 0;
		z-index: -1;
		padding: 1px;
		border-radius: inherit;
		background: linear-gradient(
			135deg,
			var(--glass-shine) 0%,
			var(--glass-edge) 30%,
			var(--glass-rim) 65%,
			var(--glass-shine) 100%
		);
		-webkit-mask:
			linear-gradient(#000 0 0) content-box,
			linear-gradient(#000 0 0);
		mask:
			linear-gradient(#000 0 0) content-box,
			linear-gradient(#000 0 0);
		-webkit-mask-composite: xor;
		mask-composite: exclude;
		pointer-events: none;
	}

	/* The thickness: light entering along the top edge and dying out by the
	   middle of the pill. */
	.tabbar::after {
		content: '';
		position: absolute;
		inset: 1px;
		z-index: -1;
		border-radius: inherit;
		background: linear-gradient(180deg, var(--glass-shine) -40%, transparent 55%);
		opacity: 0.45;
		pointer-events: none;
	}

	/* The lens under the current tab: one of the equal slots inside the
	   padding, a step lighter than the glass, springing to the chosen one. */
	.lens {
		position: absolute;
		top: 6px;
		bottom: 6px;
		left: calc(var(--space-2) + var(--slot) * ((100% - 2 * var(--space-2)) / var(--slots)));
		z-index: -1;
		width: calc((100% - 2 * var(--space-2)) / var(--slots));
		border-radius: var(--radius-full);
		background: var(--glass-lens);
		transition: left var(--dur-slow) var(--ease-spring);
	}

	.lens::before {
		background: linear-gradient(
			180deg,
			var(--glass-shine) 0%,
			var(--glass-edge) 45%,
			var(--glass-rim) 100%
		);
	}

	.tab {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 4px;
		min-width: 0;
		height: var(--tabbar);
		padding: 0;
		color: var(--ink-3);
		text-decoration: none;
		transition: color var(--dur-fast) var(--ease-out);
	}

	.tab__label {
		max-width: 100%;
		font-size: var(--text-2xs);
		font-weight: 600;
		line-height: 1;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.tab--on {
		color: var(--ink);
	}

	@media (hover: hover) {
		.tab:hover {
			color: var(--ink-2);
		}

		.tab--on:hover {
			color: var(--ink);
		}
	}

	.add {
		display: flex;
		justify-content: center;
		align-items: center;
		height: var(--tabbar);
		text-decoration: none;
	}

	/* The sun on the bar: ember, with the theme's ink on it. */
	.add__disc {
		display: grid;
		place-items: center;
		width: 48px;
		height: 48px;
		border-radius: var(--radius-full);
		background: var(--signal);
		color: var(--signal-ink);
		transition: background var(--dur-fast) var(--ease-out);
	}

	.add:active .add__disc {
		background: color-mix(in srgb, var(--signal) 85%, var(--ink));
	}

	.add--off {
		opacity: 0.4;
		pointer-events: none;
	}
</style>
