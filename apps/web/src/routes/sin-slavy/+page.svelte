<script lang="ts">
	/**
	 * Síň slávy — the achieved wall, and where a dream goes when it leaves the
	 * reel. It reads the same board the reel does and keeps the other half:
	 * the dreams marked splněno, the most recent first, each still its own
	 * photograph because the proof is the picture, not a line in a list.
	 *
	 * The before-and-after photograph and the anniversary are M3's; this is
	 * the wall the achieved filter needs in order not to lose a dream.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { listBoard } from '$lib/api/client';
	import { achievedDreams } from '$lib/dreams/board';
	import { formatDate } from '$lib/dreams/format';
	import { connection } from '$lib/offline/status.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';

	let dreams = $state<Dream[]>([]);

	const achieved = $derived(achievedDreams(dreams));

	/** The photograph a tile shows: the first one whose sizes are ready. */
	function photoOf(dream: Dream) {
		return dream.images.find((image) => image.ready) ?? null;
	}

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams: rows }) => {
				if (live) dreams = rows;
			})
			.catch(() => {
				// Not paired, or nothing remembered and no signal: an empty wall
				// either way, and its empty state says how a dream gets here.
				if (live) dreams = [];
			});
		return () => {
			live = false;
		};
	});
</script>

<svelte:head>
	<title>Aspire — síň slávy</title>
</svelte:head>

<main class="page">
	<h1 class="title">Síň slávy</h1>

	{#if !connection.online}
		<p class="hint how">Bez připojení. Síň je z paměti — poslední, kterou tohle zařízení vidělo.</p>
	{/if}

	{#if achieved.length > 0}
		<!--
			The wall: a wide tile per dream, the whole of it the way back to the
			dream itself. Wide rather than the reel's full screen, because these
			are looked at together — the point is how many there are.
		-->
		<section class="hall">
			{#each achieved as dream (dream.id)}
				{@const photo = photoOf(dream)}
				<a
					class="dream dream--wide hall__tile"
					class:dream--sky={!photo}
					href={resolve('/sen/[id]', { id: dream.id })}
				>
					{#if photo}
						<img class="dream__img" src={photo.screenUrl} alt="" loading="lazy" decoding="async" />
					{/if}
					<div class="dream__body">
						<h2 class="dream__title dream__title--sm">{dream.title}</h2>
						{#if dream.achievedAt}
							<p class="dream__why hall__when">Splněno {formatDate(dream.achievedAt)}</p>
						{/if}
					</div>
				</a>
			{/each}
		</section>
	{:else}
		<section class="card empty">
			<span class="circle circle--lg circle--sky"
				><Icon name="trophy" size={24} stroke={1.8} /></span
			>
			<h2 class="empty__title">Zatím prázdná</h2>
			<p class="hint">
				Splněný sen sem přijde sám. Označíš ho jako splněný a přidáš skutečnou fotku vedle té
				vysněné.
			</p>
		</section>
	{/if}
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

	.how {
		padding-inline: var(--space-2);
	}

	/* ── the wall ──────────────────────────────────────────────────────── */

	.hall {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	/* The whole tile is the way back to the dream, so the words inside it
	   must not read as a link; it presses like a card instead. */
	.hall__tile {
		text-decoration: none;
		transition: transform var(--dur-fast) var(--ease-out);
	}

	.hall__tile:active {
		transform: scale(0.99);
	}

	/* The date is the quiet half of the pair: the title carries the dream. */
	.hall__when {
		font-size: var(--text-sm);
	}
</style>
