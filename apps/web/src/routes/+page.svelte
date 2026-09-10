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
	import { ApiError, likeDream, listBoard } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { readToken } from '$lib/api/token';
	import { STATUS_BADGE } from '$lib/dreams/rules';
	import { rememberBoard } from '$lib/offline/cache';
	import { connection } from '$lib/offline/status.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[] | null>(null);

	/** The server answered 401 to a token this device still holds. */
	let unknownDevice = $state(false);

	/** The photograph a tile shows: the first one whose sizes are ready. */
	function photoOf(dream: Dream) {
		return dream.images.find((image) => image.ready) ?? null;
	}

	$effect(() => {
		let live = true;
		let again: ReturnType<typeof setTimeout> | undefined;
		let asks = 0;

		const load = () => {
			listBoard()
				.then(({ dreams: rows, fromCache }) => {
					if (!live) return;
					dreams = rows;
					unknownDevice = false;
					// The cache's board is the last one seen: nothing to keep,
					// nothing to wait for.
					if (fromCache) return;
					void rememberBoard(rows);
					// A photograph still being resized shows as the sky; the
					// sizes take a second, so the board asks again a few times.
					const waiting = rows.some((d) => d.images.some((image) => !image.ready));
					if (waiting && asks++ < 5) again = setTimeout(load, 1500);
				})
				.catch((e: unknown) => {
					if (!live) return;
					// Not paired, or nothing cached and no signal: the board is
					// empty either way, and the empty state says what to do next.
					// A token the server no longer knows gets its own sentence.
					dreams = [];
					unknownDevice = e instanceof ApiError && e.status === 401 && readToken() !== null;
				});
		};
		load();

		return () => {
			live = false;
			clearTimeout(again);
		};
	});

	async function like(dream: Dream) {
		try {
			const liked = await likeDream(dream.id);
			dreams = dreams?.map((d) => (d.id === liked.id ? liked : d)) ?? null;
			navigator.vibrate?.(10);
		} catch (e) {
			toast.show(describeError(e));
		}
	}
</script>

<svelte:head>
	<title>Aspire</title>
</svelte:head>

<main class="page" class:page--reel={dreams !== null && dreams.length > 0}>
	<h1 class="wordmark">Aspire</h1>

	{#if !connection.online}
		<p class="hint how">
			Bez připojení. Nástěnka je z paměti; srdíčka a přidávání počkají, až bude signál.
		</p>
	{:else if unknownDevice}
		<p class="hint how">
			Server tohle zařízení nezná.
			<a class="link" href={resolve('/nastaveni/parovani')}>Spáruj ho znovu.</a>
		</p>
	{/if}

	{#if dreams && dreams.length > 0}
		<!--
			The reel: one tile the height of the screen per dream, snapping as
			they scroll. The whole picture opens the dream; the heart on it is
			the one control, above the link, so a tap on it is a like and a tap
			anywhere else is the dream.
		-->
		<section class="reel">
			{#each dreams as dream, index (dream.id)}
				{@const photo = photoOf(dream)}
				<article class="dream reel__tile" class:dream--sky={!photo}>
					{#if photo}
						<img
							class="dream__img"
							src={photo.screenUrl}
							alt=""
							loading={index === 0 ? 'eager' : 'lazy'}
							decoding="async"
						/>
					{/if}
					<span class="badge dream__tag">{STATUS_BADGE[dream.status]}</span>
					<a
						class="reel__open"
						href={resolve('/sen/[id]', { id: dream.id })}
						aria-label={dream.title}
					></a>
					<div class="dream__body reel__body">
						<h2 class="dream__title">{dream.title}</h2>
						{#if dream.why}
							<p class="dream__why">{dream.why}</p>
						{/if}
						<button
							type="button"
							class="btn btn--photo reel__heart"
							onclick={() => like(dream)}
							disabled={!connection.online}
							aria-label="Palivo"
						>
							<Icon name="heart" size={18} stroke={2} />
							{dream.likes}
						</button>
					</div>
				</article>
			{/each}
		</section>
	{:else}
		<article class="dream dream--sky first" aria-labelledby="first-title">
			<span class="sky" aria-hidden="true"></span>
			<div class="dream__body">
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

	/* On a desktop the column is 34rem wide and a 4:5 tile would run under
	   the bar, hiding the pill on first paint. The tile gives up its ratio
	   before it gives up its foot: capped to what is left of the viewport
	   under the wordmark and above the bar. */
	@media (min-width: 35rem) {
		.first {
			max-height: calc(100dvh - 9.5rem - var(--page-end, 0px));
		}
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

	/* ── the reel ──────────────────────────────────────────────────────── */

	/* Snapping is the page's, because the page is the scroll region; a
	   flick lands on a tile, a slow drag can stop between two. */
	.page--reel {
		scroll-snap-type: y proximity;
	}

	.reel {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	/* One screen each: what the page shows between its own padding and the
	   bar. The ratio gives way to the height; the image covers whatever is
	   left. */
	.reel__tile {
		flex: none;
		aspect-ratio: auto;
		height: calc(100dvh - var(--space-3) - env(safe-area-inset-top, 0px) - var(--page-end, 0px));
		scroll-snap-align: start;
		scroll-snap-stop: always;
	}

	.reel__open {
		position: absolute;
		inset: 0;
		z-index: 1;
	}

	/* The words let the tap through to the link; only the heart takes it. */
	.reel__body {
		z-index: 2;
		pointer-events: none;
	}

	.reel__heart {
		margin-top: var(--space-2);
		pointer-events: auto;
	}
</style>
