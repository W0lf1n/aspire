<script lang="ts">
	/**
	 * Nastavení · Vzhled — systém / světlý / tmavý, one segmented pill. The
	 * choice is kept in `localStorage`, so it is readable before first paint
	 * and a dark launch never flashes linen.
	 */
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { applyTheme, readTheme, type Theme } from '$lib/ui/theme';

	let theme = $state<Theme>(readTheme());

	const OPTIONS: { value: Theme; label: string }[] = [
		{ value: 'system', label: 'Systém' },
		{ value: 'light', label: 'Světlý' },
		{ value: 'dark', label: 'Tmavý' }
	];

	function chooseTheme(next: Theme) {
		theme = next;
		applyTheme(next);
	}
</script>

<svelte:head>
	<title>Aspire — vzhled</title>
</svelte:head>

<main class="page">
	<AppBar title="Vzhled" back="/nastaveni" />

	<section class="card">
		<div class="seg seg--soft" role="group" aria-label="Motiv">
			{#each OPTIONS as option (option.value)}
				<button
					type="button"
					class="seg__item"
					aria-pressed={theme === option.value}
					onclick={() => chooseTheme(option.value)}
				>
					{option.label}
				</button>
			{/each}
		</div>
		<p class="hint">Ve tmavém fotky vyniknou. Světlý je pro den, systém přepíná sám.</p>
	</section>
</main>

<TabBar />
