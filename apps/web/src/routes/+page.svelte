<script lang="ts">
	/**
	 * Nástěnka — the board, and the launch route.
	 *
	 * In M0 it has one state worth designing: empty. The empty state is not
	 * an illustration with a caption; it is the first dream's own tile, with
	 * the sky where the photograph will be and the words where the words will
	 * be, so the board already looks like the board before there is anything
	 * on it. M1 replaces the tile with the swipe.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { listDreams } from '$lib/api/client';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';

	let dreams = $state<Dream[] | null>(null);

	$effect(() => {
		let live = true;
		listDreams()
			.then((rows) => {
				if (live) dreams = rows;
			})
			.catch(() => {
				// Not paired, offline, or the server is away: the board is empty
				// either way, and the empty state says what to do next.
				if (live) dreams = [];
			});
		return () => {
			live = false;
		};
	});
</script>

<svelte:head>
	<title>Aspire</title>
</svelte:head>

<main class="page">
	<h1 class="wordmark">Aspire</h1>

	{#if dreams && dreams.length > 0}
		<!-- The swipe arrives with M1; until then the rows are a list. -->
		<section class="card card--list">
			{#each dreams as dream (dream.id)}
				<div class="row">
					<span class="circle circle--sky"><Icon name="image" size={20} stroke={1.8} /></span>
					<span class="row__body">
						<span class="row__title">{dream.title}</span>
						<span class="row__sub">{dream.why}</span>
					</span>
				</div>
			{/each}
		</section>
	{:else}
		<article class="dream dream--sky first" aria-labelledby="first-title">
			<span class="sky" aria-hidden="true"></span>
			<div class="dream__body">
				<span class="badge badge--photo badge--tiny">první sen</span>
				<h2 id="first-title" class="dream__title">Zatím žádný sen</h2>
				<p class="dream__why">Přidej první. Obrázek, který ti připomene, proč to všechno děláš.</p>
				<div class="actions first__actions">
					<a class="btn btn--photo btn--lg" href={resolve('/pridat')}>
						<Icon name="plus" size={18} stroke={2} />
						Přidat sen
					</a>
				</div>
			</div>
		</article>

		<p class="hint how">
			Fotka z telefonu, název a jedna věta proč. Sny pak listuješ jako příběhy, jeden na celou
			obrazovku.
		</p>
	{/if}
</main>

<TabBar />

<style>
	/* The name, in the flow, at the hero size: it is the only title the board
	   has, and it scrolls away with the tile. */
	.wordmark {
		margin: var(--space-3) 0 var(--space-2);
		font-size: var(--text-hero);
		font-weight: 600;
		line-height: 1;
		letter-spacing: var(--track-hero);
		color: var(--ink);
	}

	/* The sky drifts: two lights moving over the gradient at the pace of a
	   slow breath — felt, not seen. `prefers-reduced-motion` stops it in
	   app.css. The tile is the one authored motion on the screen. */
	.first {
		flex: none;
	}

	.sky {
		position: absolute;
		inset: -20%;
		background:
			radial-gradient(40% 36% at 30% 26%, rgb(255 255 255 / 34%), transparent 70%),
			radial-gradient(50% 42% at 76% 78%, rgb(90 79 214 / 55%), transparent 70%);
		animation: drift var(--dur-drift) var(--ease-in-out) infinite alternate;
		will-change: transform;
	}

	@keyframes drift {
		from {
			transform: translate3d(-3%, -2%, 0) scale(1);
		}
		to {
			transform: translate3d(3%, 3%, 0) scale(1.06);
		}
	}

	.first__actions {
		margin-top: var(--space-2);
	}

	.how {
		padding-inline: var(--space-2);
	}
</style>
