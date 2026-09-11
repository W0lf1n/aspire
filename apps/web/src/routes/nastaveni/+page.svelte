<script lang="ts">
	/**
	 * Nastavení — the hub, and the last tab. Each row is a room of its own
	 * with a back chevron there: Vzhled, Upozornění, Tapeta, Stahování,
	 * Párování. Every row carries a live one-line summary of what is in the
	 * room, so the hub reads as a status page before it is a menu.
	 *
	 * The theme and the token are on the device and read at once; the nudge
	 * is the server's and arrives a moment later, so the row starts at the
	 * one state that is true before anybody has asked for anything.
	 *
	 * The version card carries *Obnovit aplikaci*, which is the reload an
	 * installed app has no address bar for: it asks for a new build and comes
	 * back on it. It is here always, not only when one is waiting — a toast
	 * can be missed, and a control that appears only once there is news cannot
	 * be reached, because navigating to it is already a reload (D42).
	 */
	import { resolve } from '$app/paths';
	// The package version, not `$app/environment`'s: that one is the build
	// stamp the service worker keys its cache on, and reads as a timestamp.
	import { version } from '../../../package.json';
	import type { NudgeMode } from '@aspire/contracts';
	import { readToken } from '$lib/api/token';
	import { current } from '$lib/push/nudge';
	import { detects, effectivePolicy, readPolicy } from '$lib/offline/policy';
	import { update } from '$lib/ui/update.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { settingsRows } from '$lib/ui/settings';
	import { readTheme } from '$lib/ui/theme';

	let nudge = $state<NudgeMode>('off');

	/** The reload is a moment of nothing happening; the button says so. */
	let refreshing = $state(false);

	async function refresh() {
		refreshing = true;
		await update.refresh();
	}

	const rows = $derived(
		settingsRows({
			theme: readTheme(),
			paired: readToken() !== null,
			nudge,
			offline: effectivePolicy(readPolicy(), detects())
		})
	);

	$effect(() => {
		let live = true;
		current()
			.then((settings) => {
				if (live) nudge = settings.mode;
			})
			.catch(() => undefined);
		return () => {
			live = false;
		};
	});
</script>

<svelte:head>
	<title>Aspire — nastavení</title>
</svelte:head>

<main class="page">
	<h1 class="title">Nastavení</h1>

	<section class="card card--list">
		{#each rows as row (row.id)}
			<a class="row row--press" href={resolve(row.href)}>
				<span class="circle hub__icon"><Icon name={row.icon} size={20} stroke={1.7} /></span>
				<span class="row__body">
					<span class="row__title">{row.title}</span>
					<span class="row__sub">{row.sub}</span>
				</span>
				<span class="card__go"><Icon name="chevron-right" size={18} /></span>
			</a>
		{/each}
	</section>

	<section class="card">
		<dl class="facts">
			<div>
				<dt>Verze</dt>
				<dd>{version}</dd>
			</div>
			<div>
				<dt>Server</dt>
				<dd>tento web</dd>
			</div>
		</dl>

		<div class="actions">
			<button
				type="button"
				class="btn btn--sm"
				class:btn--primary={update.ready}
				class:btn--quiet={!update.ready}
				disabled={refreshing}
				onclick={refresh}
			>
				Obnovit aplikaci
			</button>
		</div>
		<p class="hint">
			{update.ready
				? 'Nová verze je stažená. Obnovením se do ní aplikace přepne.'
				: 'Zkusí najít novou verzi a znovu se načte. Sny ani spárování se tím nesmažou.'}
		</p>
	</section>
</main>

<TabBar />

<style>
	/* Inside a card there is nothing for glass to frost, so the room's circle
	   is the soft surface with the icon in the ink. */
	.hub__icon {
		background: var(--surface-3);
		color: var(--ink);
	}
</style>
