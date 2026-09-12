<script lang="ts">
	/**
	 * Síň slávy — the achieved wall, and where a dream goes when it leaves the
	 * reel. It reads the same board the reel does and keeps the other half:
	 * the dreams marked splněno, the most recent first, each still its own
	 * photograph because the proof is the picture, not a line in a list.
	 *
	 * A dream with both photographs stands them side by side — the dreamt one
	 * and the one taken when it happened, the same size, no arrow between
	 * them and nothing labelling which is which (D28). With only one it is
	 * the wide tile it always was.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { listBoard } from '$lib/api/client';
	import { deleting } from '$lib/dreams/deleting.svelte';
	import { achievedDreams } from '$lib/dreams/board';
	import { formatDate } from '$lib/dreams/format';
	import { photoStyle, photosOf } from '$lib/dreams/photos';
	import { connection } from '$lib/offline/status.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';

	let dreams = $state<Dream[]>([]);

	// Less whatever is inside its undo window (D65).
	const achieved = $derived(achievedDreams(dreams.filter((one) => !deleting.has(one.id))));

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
				{@const photos = photosOf(dream)}
				{#if photos.dreamt && photos.achieved}
					<a class="hall__pair" href={resolve('/sen/[id]', { id: dream.id })}>
						<div class="dream hall__half">
							<img
								class="dream__img"
								src={photos.dreamt.screenUrl}
								alt=""
								style={photoStyle(photos.dreamt)}
								loading="lazy"
								decoding="async"
							/>
						</div>
						<div class="dream hall__half">
							<img
								class="dream__img"
								src={photos.achieved.screenUrl}
								alt=""
								style={photoStyle(photos.achieved)}
								loading="lazy"
								decoding="async"
							/>
							<div class="dream__body">
								<h2 class="dream__title dream__title--sm">{dream.title}</h2>
								{#if dream.achievedAt}
									<p class="dream__why hall__when">Splněno {formatDate(dream.achievedAt)}</p>
								{/if}
							</div>
						</div>
					</a>
				{:else}
					{@const photo = photos.achieved ?? photos.dreamt}
					<a
						class="dream dream--wide hall__tile"
						class:dream--sky={!photo}
						href={resolve('/sen/[id]', { id: dream.id })}
					>
						{#if photo}
							<img
								class="dream__img"
								src={photo.screenUrl}
								alt=""
								style={photoStyle(photo)}
								loading="lazy"
								decoding="async"
							/>
						{/if}
						<div class="dream__body">
							<h2 class="dream__title dream__title--sm">{dream.title}</h2>
							{#if dream.achievedAt}
								<p class="dream__why hall__when">Splněno {formatDate(dream.achievedAt)}</p>
							{/if}
						</div>
					</a>
				{/if}
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

	/* ── the pair ──────────────────────────────────────────────────────── */

	/* Two photographs of one dream, the dreamt one and the one taken when it
	   happened, the same size and the same shape: the wall's whole argument
	   is that the second looks like the first. A 2 px gap, not a gutter —
	   they read as one object, and the words sit on the right-hand half. */
	.hall__pair {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 2px;
		border-radius: var(--radius-lg);
		overflow: hidden;
		text-decoration: none;
		box-shadow: var(--elev-photo);
		transition: transform var(--dur-fast) var(--ease-out);
	}

	.hall__pair:active {
		transform: scale(0.99);
	}

	/* Each half is a `.dream` without its own corners or shadow: the pair
	   owns both, so the seam between them stays a seam. */
	.hall__half {
		aspect-ratio: 4 / 5;
		border-radius: 0;
		box-shadow: none;
	}
</style>
