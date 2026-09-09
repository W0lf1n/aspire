<script lang="ts">
	/**
	 * Síň slávy — the achieved wall. Empty until a dream is marked achieved
	 * (M3); the empty state says how one gets here.
	 */
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
</script>

<svelte:head>
	<title>Aspire — síň slávy</title>
</svelte:head>

<main class="page">
	<h1 class="title">Síň slávy</h1>

	<section class="card empty">
		<span class="circle circle--lg circle--sky"><Icon name="trophy" size={24} stroke={1.8} /></span>
		<h2 class="empty__title">Zatím prázdná</h2>
		<p class="hint">
			Splněný sen sem přijde sám. Označíš ho jako splněný a přidáš skutečnou fotku vedle té vysněné.
		</p>
	</section>
</main>

<TabBar />

<style>
	.empty {
		align-items: flex-start;
		gap: var(--space-3);
		padding: var(--space-5) var(--space-4);
	}

	.empty__title {
		font-size: var(--text-xl);
		font-weight: 600;
		letter-spacing: var(--track-xl);
	}
</style>
