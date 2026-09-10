<script lang="ts">
	/**
	 * Nástěnka — the board, and the launch route.
	 *
	 * The reel is what is still ahead: a dream marked splněno leaves it for
	 * the Síň slávy, and the tile the reel opens on is the day's pick — the
	 * dream shown least recently, so the board is a different one each
	 * morning (`board.ts`, D25). Behind it the reel is shuffled, once per
	 * open, and only a window of it is in the document at a time: a hundred
	 * dreams in a fixed order is a route you know by heart, and a hundred
	 * full-screen tiles is a first paint you can feel (D30).
	 *
	 * On the one morning a year a dream has an anniversary, the wall reaches
	 * the board: one line above the reel, the way back to the dream itself
	 * (D29). It is not a card and not a tile — the photograph stays the hero.
	 *
	 * Empty, it has one state worth designing. The empty state is not an
	 * illustration with a caption; it is the first dream's own tile, with the
	 * sky where the photograph will be and the words where the words will be,
	 * so the board already looks like the board before there is anything on it.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { DREAM_CATEGORIES } from '@aspire/contracts';
	import { ApiError, likeDream, listBoard, markShown } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { readToken } from '$lib/api/token';
	import {
		REEL_WINDOW,
		anniversaryToday,
		byCategory,
		categoriesOnBoard,
		pickDaily,
		reelOrder,
		reelSequence,
		shownToday,
		tileLine,
		type BoardFilter
	} from '$lib/dreams/board';
	import { formatAnniversary } from '$lib/dreams/format';
	import { photoOf } from '$lib/dreams/photos';
	import { CATEGORY_LABEL, STATUS_BADGE } from '$lib/dreams/rules';
	import { rememberBoard } from '$lib/offline/cache';
	import { connection } from '$lib/offline/status.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[] | null>(null);

	/** The server answered 401 to a token this device still holds. */
	let unknownDevice = $state(false);

	/** The day's pick, chosen once when the board first arrives. */
	let pickedId = $state<string | null>(null);

	/**
	 * The order this opening of the reel is in: the pick, then a shuffle.
	 * Worked out once and kept, so a heart tapped or a board fetched again
	 * does not reshuffle the tiles under a thumb.
	 */
	let sequence = $state<string[]>([]);

	/** Which area the board is asking for; everything, until it is told. */
	let filter = $state<BoardFilter>('all');

	/** The chips offered: only the areas this board has anything in. */
	const areas = $derived(dreams === null ? [] : categoriesOnBoard(dreams, DREAM_CATEGORIES));

	/**
	 * The area actually asked for. An area the board has run out of — its
	 * last dream marked splněno, here or on another device — falls back to
	 * everything, rather than leaving an empty reel under a chip that is
	 * no longer offered.
	 */
	const asking = $derived<BoardFilter>(
		filter !== 'all' && !areas.includes(filter) ? 'all' : filter
	);

	/** What the reel shows: the dreams still ahead, in that order, in that area. */
	const reel = $derived(dreams === null ? [] : byCategory(reelOrder(dreams, sequence), asking));

	/** How many of them are in the document; it grows as the reel is scrolled. */
	let windowed = $state(REEL_WINDOW);

	/** The tiles actually rendered. The rest arrive as the last one is neared. */
	const shown = $derived(reel.slice(0, windowed));

	/**
	 * A new area is a new reel, from its first tile: the window starts again
	 * and so does the scroll, or the board opens somewhere in the middle of a
	 * list the person has never seen.
	 */
	function ask(area: BoardFilter) {
		filter = area;
		windowed = REEL_WINDOW;
		page?.scrollTo({ top: 0 });
	}

	/** The scroll region, and the mark at the end of what is rendered. */
	let page = $state<HTMLElement | null>(null);
	let sentinel = $state<HTMLElement | null>(null);

	/**
	 * The window grows when the end of it comes into view — one more screenful
	 * of tiles, not the whole board. Nothing is ever removed from the top:
	 * taking a tile out of a snapping scroll region moves the one under the
	 * thumb, and a reel that jumps is worse than a reel that is long.
	 */
	$effect(() => {
		if (!sentinel || !page || windowed >= reel.length) return;

		const observer = new IntersectionObserver(
			(entries) => {
				if (entries.some((entry) => entry.isIntersecting)) {
					windowed = Math.min(windowed + REEL_WINDOW, reel.length);
				}
			},
			// A screenful of slack, so the next tiles exist before they are reached.
			{ root: page, rootMargin: '100% 0px' }
		);
		observer.observe(sentinel);
		return () => observer.disconnect();
	});

	/** Dreams on the board, none of them left to swipe: all of them are done. */
	const allAchieved = $derived(dreams !== null && dreams.length > 0 && reel.length === 0);

	/** A dream that came true on this day in an earlier year, or nothing. */
	const anniversary = $derived(dreams === null ? null : anniversaryToday(dreams));

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
					openWith(rows);
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

	/**
	 * The tile the board opens on, chosen once and then left alone: a reel
	 * that reshuffles under a thumb mid-scroll is not a reel. The pick is
	 * told it has been shown, which is what makes tomorrow's a different
	 * one; without a signal the stamp is skipped and the board opens on
	 * whatever the remembered one says (D25).
	 */
	function openWith(rows: Dream[]) {
		if (pickedId !== null) return;

		const chosen = pickDaily(rows);
		if (!chosen) return;

		pickedId = chosen.id;
		sequence = reelSequence(rows, chosen.id);
		if (connection.online && !shownToday(chosen.lastShownAt)) {
			// Nobody is waiting for it, and a stamp that missed is a stamp
			// the next open makes anyway.
			void markShown(chosen.id).catch(() => undefined);
		}
	}

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

<main class="page" class:page--reel={reel.length > 0} bind:this={page}>
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

	{#if anniversary}
		<a
			class="anniversary"
			href={resolve('/sen/[id]', { id: anniversary.dream.id })}
			aria-label={formatAnniversary(anniversary.years, anniversary.dream.title)}
		>
			<span class="circle circle--sm circle--dusk" aria-hidden="true">
				<Icon name="trophy" size={16} stroke={1.8} />
			</span>
			<span aria-hidden="true">{formatAnniversary(anniversary.years, anniversary.dream.title)}</span
			>
		</a>
	{/if}

	{#if areas.length > 0}
		<!--
			The areas this board has something in, and „Vše“ in front of them.
			A rail rather than a wrap: above a reel it has to cost one line,
			and the nine are a set you swipe past, not a form you read (D32).
		-->
		<div class="areas" role="group" aria-label="Oblast">
			<button
				type="button"
				class="chip"
				class:chip--on={asking === 'all'}
				aria-pressed={asking === 'all'}
				onclick={() => ask('all')}>Vše</button
			>
			{#each areas as area (area)}
				<button
					type="button"
					class="chip"
					class:chip--on={asking === area}
					aria-pressed={asking === area}
					onclick={() => ask(area)}>{CATEGORY_LABEL[area]}</button
				>
			{/each}
		</div>
	{/if}

	{#if reel.length > 0}
		<!--
			The reel: one tile the height of the screen per dream, snapping as
			they scroll, the day's pick first. The whole picture opens the
			dream; the heart on it is the one control, above the link, so a tap
			on it is a like and a tap anywhere else is the dream.
		-->
		<section class="reel">
			{#each shown as dream, index (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				{@const line = tileLine(dream)}
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
					<span class="badge dream__tag">
						{STATUS_BADGE[dream.status]}{dream.category
							? ` · ${CATEGORY_LABEL[dream.category]}`
							: ''}
					</span>
					<a
						class="reel__open"
						href={resolve('/sen/[id]', { id: dream.id })}
						aria-label={dream.title}
					></a>
					<div class="dream__body reel__body">
						<h2 class="dream__title">{dream.title}</h2>
						{#if line.text}
							<p class="dream__why" class:dream__say={line.said}>{line.text}</p>
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

			{#if windowed < reel.length}
				<!-- The end of what is rendered; seeing it brings the next few. -->
				<div class="reel__more" bind:this={sentinel} aria-hidden="true"></div>
			{/if}
		</section>
	{:else}
		<article class="dream dream--sky first" aria-labelledby="first-title">
			<span class="sky" aria-hidden="true"></span>
			<div class="dream__body">
				<h2 id="first-title" class="dream__title">
					{allAchieved ? 'Všechno splněno' : 'Zatím žádný sen'}
				</h2>
				<p class="dream__why">
					{allAchieved
						? 'Na nástěnce nezbyl sen, který by čekal. Vysni si další.'
						: 'Přidej první. Obrázek, který ti připomene, proč to všechno děláš.'}
				</p>
				<div class="actions first__actions">
					<a class="btn btn--photo btn--lg" href={resolve('/pridat')}>
						<Icon name="plus" size={18} stroke={2} />
						Přidat sen
					</a>
				</div>
			</div>
		</article>

		{#if allAchieved}
			<p class="hint how">
				Splněné sny nezmizely — najdeš je v
				<a class="link" href={resolve('/sin-slavy')}>Síni slávy</a>.
			</p>
		{:else}
			<p class="hint how">
				Fotka z telefonu, název a jedna věta proč. Sny pak listuješ jako příběhy, jeden na celou
				obrazovku.
			</p>
		{/if}
	{/if}
</main>

<TabBar />

<style>
	/* The areas, as a rail: one line above the reel, running to both edges of
	   the screen so a chip is never cut off mid-word by the page's padding.
	   The bar it is scrolled with is not shown; the chips say there is more. */
	.areas {
		display: flex;
		gap: var(--space-2);
		margin-inline: calc(var(--space-4) * -1);
		padding-inline: var(--space-4);
		overflow-x: auto;
		scrollbar-width: none;
	}

	.areas::-webkit-scrollbar {
		display: none;
	}

	/* The one morning a year the wall has something to say to the board: a
	   line, a dusk circle, and the way back to the dream. Dusk because this
	   marks rather than acts (tokens.css), and a line rather than a card
	   because the tile under it is the point of the screen. */
	.anniversary {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		margin-bottom: var(--space-2);
		padding: var(--space-2);
		border-radius: var(--radius-sm);
		background: var(--dusk-wash);
		color: var(--ink);
		font-size: var(--text-sm);
		line-height: var(--leading-base);
		text-decoration: none;
		text-wrap: pretty;
	}

	.anniversary:active {
		transform: scale(0.99);
	}

	/* The name, in the flow, at the hero size: it is the only title the board
	   has, and it scrolls away with the tile.

	   It is also the reel's first snap point. Without one the snapping runs
	   straight past everything above the first tile — the areas included —
	   and a swipe up lands back on the tile it came from, which makes a
	   filter you cannot reach. The top of the board is a place to stop. */
	.wordmark {
		scroll-snap-align: start;
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

	/* Not a tile and not a gap: a mark the observer can watch, outside the
	   snapping so it never becomes somewhere the reel can stop. */
	.reel__more {
		height: 1px;
		scroll-snap-align: none;
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
