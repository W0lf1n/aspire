<script lang="ts">
	/**
	 * Nástěnka — the board, and the launch route.
	 *
	 * The board is the reel and nothing else: one dream fills the screen, the
	 * next one is a swipe away, and the chrome — the areas, the anniversary,
	 * whatever the connection has to say — floats on the photograph rather
	 * than taking a strip of it off the top (D40). A swipe is worth exactly
	 * one dream however long it is, which `lib/ui/pager.ts` is the whole of.
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
	 * the board: one line over the reel, the way back to the dream itself
	 * (D29). It is not a card and not a tile — the photograph stays the hero.
	 *
	 * Empty, it has one state worth designing. The empty state is not an
	 * illustration with a caption; it is the first dream's own tile, with the
	 * sky where the photograph will be and the words where the words will be,
	 * so the board already looks like the board before there is anything on
	 * it. That screen is a page rather than a reel: there is nothing to swipe.
	 */
	import { untrack } from 'svelte';
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
	import { aheadOf, prefetchOrder, rememberBoard } from '$lib/offline/cache';
	import {
		capFor,
		detects,
		effectivePolicy,
		metered,
		readPolicy,
		wantsWholeBoard
	} from '$lib/offline/policy';
	import { connection } from '$lib/offline/status.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { pager, type Pager } from '$lib/ui/pager';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[] | null>(null);

	/** The server answered 401 to a token this device still holds. */
	let unknownDevice = $state(false);

	/**
	 * Whether the board on screen came from the server. Only then is there
	 * anything to fetch, and only then does what the board no longer has mean
	 * a photograph can be let go of.
	 */
	let fromNetwork = $state(false);

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

	/** How many of them are in the document; it grows as the reel is swiped. */
	let windowed = $state(REEL_WINDOW);

	/** The tiles actually rendered. The rest arrive as the last one is neared. */
	const shown = $derived(reel.slice(0, windowed));

	/**
	 * How much of the board this device keeps for when there is no signal
	 * (D39). Read once: it is a choice made on another screen, and coming
	 * back from it re-enters this one.
	 */
	const policy = effectivePolicy(readPolicy(), detects());

	/** The photographs worth having now (`aheadOf`, D39). */
	const ahead = $derived(aheadOf(reel, windowed));

	/**
	 * A new area is a new reel, from its first tile: the window starts again
	 * and so does the scroll, or the board opens somewhere in the middle of a
	 * list the person has never seen.
	 */
	function ask(area: BoardFilter) {
		filter = area;
		windowed = REEL_WINDOW;
		at = 0;
		drive?.to(0);
	}

	/** The reel's scroll region; `pager.ts` drives it. */
	let region = $state<HTMLElement | null>(null);

	/** The pager, while the reel is on the screen. */
	let drive: Pager | null = null;

	/** The dream on the screen, which the pager is the only writer of. */
	let at = $state(0);

	/** How tall the floating chrome is, so a tile's badge can clear it. */
	let band = $state(0);

	/**
	 * One dream per gesture, however long the gesture. The browser's own
	 * snapping is still what the reel rests on; this is the fence around it,
	 * and the wheel and the arrow keys.
	 */
	$effect(() => {
		const el = region;
		if (!el) return;

		drive = pager(el, {
			pages: () => shown.length,
			onPage: (index) => {
				at = index;
			}
		});

		return () => {
			drive?.destroy();
			drive = null;
		};
	});

	/**
	 * The window grows from the dream on the screen rather than from a mark
	 * in the document: the pager already knows where the thumb is, and two
	 * dreams of slack is more than one swipe can spend. Nothing is ever
	 * removed from the top — taking a tile out of a snapping scroll region
	 * moves the one under the thumb, and a reel that jumps is worse than a
	 * reel that is long (D30).
	 */
	$effect(() => {
		const here = at;
		const total = reel.length;
		untrack(() => {
			if (windowed < total && here >= windowed - 2) {
				windowed = Math.min(windowed + REEL_WINDOW, total);
			}
		});
	});

	/**
	 * The photographs this device keeps, fetched as the reel is swiped rather
	 * than all at once (D39).
	 *
	 * It runs again every time the window grows, which is the whole point: a
	 * morning of ten swipes costs ten photographs instead of a hundred, and
	 * the connection is never handed the board while somebody is looking at
	 * the first tile. Then, and only where the browser says the connection is
	 * free, the rest of the board follows in the background — that is D24's
	 * promise kept wherever it can be kept without spending mobile data.
	 */
	$effect(() => {
		const board = dreams;
		if (!fromNetwork || board === null || ahead.length === 0) return;

		const order = sequence;
		const near = ahead;
		void (async () => {
			await rememberBoard(near, board, capFor(policy));
			if (wantsWholeBoard(policy, metered())) {
				await rememberBoard(prefetchOrder(board, order), board);
			}
		})();
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
					// The cache's board is the last one seen: nothing to fetch
					// and nothing to prune against, because a board from the
					// cache is not news about what the board still has.
					if (fromCache) return;
					fromNetwork = true;
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

{#if reel.length > 0}
	<main class="board">
		<!-- The name is the tab bar's job now; the screen is the photograph. -->
		<h1 class="visually-hidden">Aspire</h1>

		<!--
			The reel: one dream the size of the screen, edge to edge, the day's
			pick first. The whole picture opens the dream; the heart on it is
			the one control, above the link, so a tap on it is a like and a tap
			anywhere else is the dream.
		-->
		<section class="reel" style:--band="{band}px" bind:this={region} aria-label="Sny">
			{#each shown as dream, index (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				{@const line = tileLine(dream)}
				<article class="dream reel__tile" class:dream--sky={!photo}>
					{#if photo}
						<!-- The dream on the screen and its two neighbours are
						     fetched and decoded before they are reached; a
						     photograph that decodes mid-swipe is the one thing
						     that can make a reel stutter. -->
						<img
							class="dream__img"
							src={photo.screenUrl}
							alt=""
							loading={Math.abs(index - at) <= 1 ? 'eager' : 'lazy'}
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
		</section>

		<!--
			The chrome, over the photograph rather than above it: the areas,
			the anniversary, and whatever the connection has to say. Only the
			pills take a tap — everything between them falls through to the
			dream underneath, which is also what keeps the whole top of the
			screen somewhere the reel can be dragged from.
		-->
		<div class="board__top" bind:clientHeight={band}>
			{#if areas.length > 0}
				<!--
					The areas this board has something in, and „Vše“ in front of
					them. A rail rather than a wrap: over a reel it has to cost
					one line, and the nine are a set you swipe past, not a form
					you read (D32).
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

			{#if anniversary}
				<a
					class="anniversary glass"
					href={resolve('/sen/[id]', { id: anniversary.dream.id })}
					aria-label={formatAnniversary(anniversary.years, anniversary.dream.title)}
				>
					<span class="circle circle--sm circle--dusk" aria-hidden="true">
						<Icon name="trophy" size={16} stroke={1.8} />
					</span>
					<span aria-hidden="true"
						>{formatAnniversary(anniversary.years, anniversary.dream.title)}</span
					>
				</a>
			{/if}

			{#if !connection.online}
				<p class="hint glass board__note">
					Bez připojení. Nástěnka je z paměti; srdíčka a přidávání počkají, až bude signál.
				</p>
			{:else if unknownDevice}
				<p class="hint glass board__note">
					Server tohle zařízení nezná.
					<a class="link" href={resolve('/nastaveni/parovani')}>Spáruj ho znovu.</a>
				</p>
			{/if}
		</div>
	</main>
{:else}
	<main class="page">
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
				class="anniversary anniversary--flow"
				href={resolve('/sen/[id]', { id: anniversary.dream.id })}
				aria-label={formatAnniversary(anniversary.years, anniversary.dream.title)}
			>
				<span class="circle circle--sm circle--dusk" aria-hidden="true">
					<Icon name="trophy" size={16} stroke={1.8} />
				</span>
				<span aria-hidden="true"
					>{formatAnniversary(anniversary.years, anniversary.dream.title)}</span
				>
			</a>
		{/if}

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
	</main>
{/if}

<TabBar />

<style>
	/* ── the reel ──────────────────────────────────────────────────────── */

	/**
	 * The board, when there is one: the frame the reel scrolls inside and the
	 * chrome is hung from. It has no padding of its own — a photograph that
	 * stops short of the edge is a card, and this screen has no cards on it.
	 */
	.board {
		position: relative;
		display: flex;
		flex-direction: column;
		flex: 1;
		min-height: 0;
	}

	/**
	 * The scroll region. Snapping is mandatory and every dream stops it,
	 * which is the standard's own way of saying one dream per swipe;
	 * `pager.ts` puts a fence around the gesture where a browser keeps that
	 * unevenly, and owns the wheel and the arrow keys outright. Nothing sets
	 * `scroll-behavior`: the pager animates the offset itself, on the house
	 * curve, and a second opinion from CSS would fight it.
	 */
	.reel {
		flex: 1;
		min-height: 0;
		overflow-y: auto;
		overscroll-behavior-y: contain;
		scroll-snap-type: y mandatory;
		/* Vertical only, so a sideways drag is never half a page turn — with
		   pinch kept, because that is somebody's way of reading. */
		touch-action: pan-y pinch-zoom;
	}

	/**
	 * A page: the screen, edge to edge. The print's ratio, radius and shadow
	 * all go, and so does the gap that used to be between two of them — a
	 * reel has no ground to show a dream against, and a seam of it passing by
	 * mid-swipe is the tell that this is a list rather than a reel.
	 */
	.reel__tile {
		aspect-ratio: auto;
		height: 100%;
		border-radius: 0;
		box-shadow: none;
		scroll-snap-align: start;
		scroll-snap-stop: always;
	}

	/* A screen's worth of photograph needs the longer ramp: the words sit
	   clear of the bar, a sixth of the way up, where `--scrim` has barely
	   begun (tokens.css). */
	.reel__tile::after {
		background: var(--scrim-tall);
	}

	/* The status stands under the floating chrome rather than behind it. The
	   chrome measures itself and says how tall it is; the fallback is what it
	   measures when it is only the rail, so the first frame is already right
	   on the board that has no anniversary — which is every board but one. */
	.reel__tile .dream__tag {
		top: calc(var(--band, 4.25rem) + var(--space-2));
	}

	.reel__open {
		position: absolute;
		inset: 0;
		z-index: 1;
	}

	/* The words let the tap through to the link; only the heart takes it.
	   Their foot clears the floating bar, by the distance every page's last
	   row clears it. */
	.reel__body {
		z-index: 2;
		padding-bottom: var(--page-end);
		pointer-events: none;
	}

	.reel__heart {
		margin-top: var(--space-2);
		pointer-events: auto;
	}

	/* ── the chrome, floating ──────────────────────────────────────────── */

	.board__top {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: var(--space-2);
		padding: calc(var(--space-3) + env(safe-area-inset-top, 0px)) var(--space-4) var(--space-4);
		pointer-events: none;
	}

	/* The top scrim: the one the tokens have kept for exactly this, so a
	   white chip on a white sky still has an edge. */
	.board__top::before {
		content: '';
		position: absolute;
		inset: 0;
		background: var(--scrim-top);
		pointer-events: none;
	}

	.board__top > * {
		position: relative;
		max-width: 100%;
		pointer-events: auto;
	}

	/* The areas, as a rail: one line over the reel, running to both edges of
	   the screen so a chip is never cut off mid-word by the padding. The bar
	   it is scrolled with is not shown; the chips say there is more. */
	.areas {
		align-self: stretch;
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

	/**
	 * The one morning a year the wall has something to say to the board: a
	 * line, a dusk circle, and the way back to the dream. Dusk because this
	 * marks rather than acts (tokens.css), and a line rather than a card
	 * because the tile under it is the point of the screen.
	 *
	 * Over a photograph it cannot be a wash of dusk on the ground — there is
	 * no ground — so it is `.glass`, like everything else that floats, with
	 * the dusk kept on the circle where it still reads. On the empty board
	 * there is a ground, and `--flow` is the wash again.
	 */
	.anniversary {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-2);
		padding-right: var(--space-3);
		border-radius: var(--radius-full);
		color: var(--ink);
		font-size: var(--text-sm);
		line-height: var(--leading-base);
		text-decoration: none;
		text-wrap: pretty;
	}

	.anniversary--flow {
		margin-bottom: var(--space-2);
		padding-right: var(--space-2);
		border-radius: var(--radius-sm);
		background: var(--dusk-wash);
	}

	.anniversary:active {
		transform: scale(0.99);
	}

	/* What the connection has to say, in the same glass as the chips beside
	   it, because over a photograph the ground it used to sit on is gone. */
	.board__note {
		padding: var(--space-2) var(--space-3);
		border-radius: var(--radius-lg);
	}

	/* ── the empty board ───────────────────────────────────────────────── */

	/* The name, in the flow rather than in a bar: it scrolls away. Only the
	   empty board has one — where there is a reel, the photograph is the
	   screen and the bar says which one it is. */
	.wordmark {
		margin: var(--space-3) 0 var(--space-2);
		font-size: var(--text-hero);
		font-weight: 600;
		line-height: 1;
		letter-spacing: var(--track-hero);
		color: var(--ink);
	}

	/* The first dream's tile, in the flow of a page rather than a page of a
	   reel: it keeps the 4:5 ratio the primitive gives it. */
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

	/* The sky drifts: two lights moving over the gradient at the pace of a
	   slow breath — felt, not seen. `prefers-reduced-motion` stops it in
	   app.css. It is the one authored motion on the empty screen. */
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
