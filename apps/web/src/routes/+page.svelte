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
	import {
		ApiError,
		deleteImage,
		getDream,
		likeDream,
		listBoard,
		markShown,
		uploadImage
	} from '$lib/api/client';
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
		type BoardFilter
	} from '$lib/dreams/board';
	import { deleting } from '$lib/dreams/deleting.svelte';
	import { formatAnniversary } from '$lib/dreams/format';
	import { REELS, focusDreams, readReel, saveReel, type Reel } from '$lib/dreams/focus';
	import { CATEGORY_LABEL, FOCUS_MAX } from '$lib/dreams/rules';
	import { photographDone, replacePhotograph } from '$lib/dreams/upload';
	import { downscale } from '$lib/images/downscale';
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
	import ReelTile from '$lib/ui/ReelTile.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { pager, type Pager } from '$lib/ui/pager';
	import { sideways, stepWithin } from '$lib/ui/sideways';
	import { toast } from '$lib/ui/toast.svelte';

	/** What the server said. A dream deleted a moment ago is still in it (D65). */
	let dreams = $state<Dream[] | null>(null);

	/**
	 * The board as the screens must show it: without any dream inside its undo
	 * window. The raw answer is kept beside it, so „Vrátit“ puts the tile back
	 * without a second trip to the server.
	 */
	const board = $derived(dreams === null ? null : dreams.filter((one) => !deleting.has(one.id)));

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
	 * The dream having its photograph taken, and what to show on its tile
	 * while the server works (D52). The preview is the downscaled file itself,
	 * so the sky becomes the picture on the tap rather than eight seconds
	 * later — the upload, the resize and the three sizes all happen behind a
	 * tile that already looks right.
	 */
	let picking = $state<{ id: string; preview: string } | null>(null);

	/**
	 * The last preview's object URL. Let go of when the next pick makes one or
	 * when the screen goes, never the moment the upload finishes: the real
	 * photograph and the preview swap on a later frame, and a URL revoked
	 * between the two blanks the tile.
	 */
	let lastPreview: string | null = null;

	$effect(() => () => {
		if (lastPreview) URL.revokeObjectURL(lastPreview);
	});

	/**
	 * Whether a tile can be given a photograph right now. Only this half is
	 * the board's: the tile's own control wears `use:writes`, which is where
	 * the connection is remembered (D67).
	 */
	const canPick = $derived(picking === null);

	/**
	 * The order this opening of the reel is in: the pick, then a shuffle.
	 * Worked out once and kept, so a heart tapped or a board fetched again
	 * does not reshuffle the tiles under a thumb.
	 */
	let sequence = $state<string[]>([]);

	/**
	 * Which of the two reels is on the screen (D53), remembered from the last
	 * open: a week of focusing on the same ten should not cost a tap every
	 * morning.
	 */
	let which = $state<Reel>(readReel());

	/** Which area the board is asking for; everything, until it is told. */
	let filter = $state<BoardFilter>('all');

	/** The chips offered: only the areas this board has anything in. */
	const areas = $derived(board === null ? [] : categoriesOnBoard(board, DREAM_CATEGORIES));

	/**
	 * The area actually asked for. An area the board has run out of — its
	 * last dream marked splněno, here or on another device — falls back to
	 * everything, rather than leaving an empty reel under a chip that is
	 * no longer offered.
	 */
	const asking = $derived<BoardFilter>(
		filter !== 'all' && !areas.includes(filter) ? 'all' : filter
	);

	/** Everything still ahead, in this open's order, in the area asked for. */
	const everything = $derived(board === null ? [] : byCategory(reelOrder(board, sequence), asking));

	/**
	 * Teď: the ten he is on now, in his own order and not shuffled (D53). Ten
	 * dreams are reached in ten swipes, so the order is the point rather than
	 * a route learned by heart — which is what D30's shuffle exists to stop on
	 * a board of a hundred.
	 */
	const focus = $derived(board === null ? [] : focusDreams(board));

	/** What is actually on the screen. */
	const reel = $derived(which === 'focus' ? focus : everything);

	/**
	 * Whether there is a second reel to offer at all. A board nobody has
	 * chosen a Teď on still gets the choice — that is how the screen says
	 * there is one — but a board with no dreams does not.
	 */
	const hasBoard = $derived(board !== null && board.length > 0);

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
		restart();
	}

	/**
	 * The other reel. A different reel is a different list, so it starts at
	 * its first tile — Teď at rank 1, Vše at the day's pick — rather than
	 * wherever the thumb happened to be in the one being left.
	 */
	function show(next: Reel) {
		if (next === which) return;
		which = next;
		saveReel(next);
		restart();
	}

	/** Back to the top of whatever the reel now is. */
	function restart() {
		windowed = REEL_WINDOW;
		at = 0;
		drive?.to(0);
	}

	/** The reel's scroll region; `pager.ts` drives it. */
	let region = $state<HTMLElement | null>(null);

	/**
	 * Teď with nothing on it is a page rather than a reel, and a swipe has to
	 * carry back out of it or the gesture that got somebody there is a gesture
	 * that strands them (D71).
	 */
	let emptyFocus = $state<HTMLElement | null>(null);

	/** The pager, while the reel is on the screen. */
	let drive: Pager | null = null;

	/** The dream on the screen, which the pager is the only writer of. */
	let at = $state(0);

	/** How tall the floating chrome is, so a tile's badge can clear it. */
	let band = $state(0);

	/**
	 * Across the reel to change which reel it is (D71). A gesture the pager
	 * deliberately ignores — the browser keeps every vertical pan for itself,
	 * and this takes only what went clearly further across than down — so the
	 * two never argue over the same swipe. At either end it does nothing,
	 * because a swipe that wrapped round would go two ways from one gesture.
	 */
	$effect(() => {
		const el = region ?? emptyFocus;
		if (!el) return;

		const swipes = sideways(el, {
			onSwipe: (direction) => {
				const next = stepWithin(REELS, which, direction);
				if (next === which) return;
				show(next);
				navigator.vibrate?.(10);
			}
		});

		return () => swipes.destroy();
	});

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
		const rows = board;
		if (!fromNetwork || rows === null || ahead.length === 0) return;

		const order = sequence;
		const near = ahead;
		void (async () => {
			await rememberBoard(near, rows, capFor(policy));
			if (wantsWholeBoard(policy, metered())) {
				await rememberBoard(prefetchOrder(rows, order), rows);
			}
		})();
	});

	/** Dreams on the board, none of them left to swipe: all of them are done. */
	const allAchieved = $derived(board !== null && board.length > 0 && reel.length === 0);

	/** A dream that came true on this day in an earlier year, or nothing. */
	const anniversary = $derived(board === null ? null : anniversaryToday(board));

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
					openWith(rows.filter((one) => !deleting.has(one.id)));
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
	 * The tile the Vše reel opens on, chosen once and then left alone: a reel
	 * that reshuffles under a thumb mid-scroll is not a reel (D25).
	 */
	function openWith(rows: Dream[]) {
		if (pickedId !== null) return;

		const chosen = pickDaily(rows);
		if (!chosen) return;

		pickedId = chosen.id;
		sequence = reelSequence(rows, chosen.id);
	}

	/** Whether this open has already said the pick was shown. */
	let stamped = false;

	/**
	 * The pick is told it has been shown, which is what makes tomorrow's a
	 * different one — but only once it actually has been, which means only on
	 * Vše (D53). A board opened on Teď has not put the day's dream in front of
	 * anybody, and „shown“ is what the word means (D25, D36); switching to Vše
	 * stamps it then.
	 *
	 * Without a signal it is skipped and the board still opens on a pick
	 * worked out from the remembered board (D24). Nobody waits for it, and a
	 * stamp that missed is one the next open makes anyway.
	 */
	$effect(() => {
		const id = pickedId;
		const rows = board;
		if (which !== 'all' || id === null || rows === null || stamped) return;
		if (!connection.online) return;

		const chosen = rows.find((dream) => dream.id === id);
		stamped = true;
		if (chosen && !shownToday(chosen.lastShownAt)) {
			void markShown(id).catch(() => undefined);
		}
	});

	/**
	 * The photograph a dream on the reel did not have (D52).
	 *
	 * A dream written as a sentence in the Seznam arrives with no picture, and
	 * the reel is where that is noticed — so it is where it gets fixed, rather
	 * than two screens away. The sequence is the dream screen's own
	 * (`dreams/upload.ts`): the file downscaled here, the old row of this kind
	 * out, the new one in, and the dream asked for again until its sizes are
	 * ready.
	 */
	async function takePhoto(dream: Dream, event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		// Cleared so picking the same file twice is still a change event.
		input.value = '';
		if (!file || picking) return;

		try {
			const photo = await downscale(file);
			if (lastPreview) URL.revokeObjectURL(lastPreview);
			lastPreview = URL.createObjectURL(photo);
			picking = { id: dream.id, preview: lastPreview };

			const saved = await replacePhotograph(dream, photo, 'dreamt', {
				deleteImage,
				uploadImage,
				getDream
			});
			dreams = dreams?.map((d) => (d.id === saved.id ? saved : d)) ?? null;
			toast.show(photographDone('dreamt'));
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			// The preview's URL outlives this on purpose; the tile is still
			// pointing at it until the real photograph has rendered.
			picking = null;
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
			anywhere else is the dream — and a heart tapped is the dream coming
			round sooner, which is the only thing the count is for (D58).
		-->
		<section class="reel" style:--band="{band}px" bind:this={region} aria-label="Sny">
			{#each shown as dream, index (dream.id)}
				<ReelTile
					{dream}
					eager={Math.abs(index - at) <= 1}
					preview={picking?.id === dream.id ? picking.preview : null}
					{canPick}
					onlike={like}
					onpick={takePhoto}
				/>
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
			{#if hasBoard}
				<!--
					The two reels (D53): everything still ahead, shuffled, with
					the day's pick at its head — and the ten he is on now, in
					his own order. A segment rather than a chip beside the
					areas, because these two are one choice and exactly one of
					them is true at a time; the areas narrow whichever is.
				-->
				<div
					class="seg seg--glass board__reels"
					style:--slot={which === 'all' ? 0 : 1}
					role="group"
					aria-label="Která nástěnka"
				>
					<!-- The lens, as the tab bar has one: it marks the choice by
					     sliding to it, so the segment reads as one piece of glass
					     with a bright pane rather than two buttons (D74). -->
					<span class="seg__lens" aria-hidden="true"></span>
					<button
						type="button"
						class="seg__item"
						aria-pressed={which === 'all'}
						onclick={() => show('all')}>Vše</button
					>
					<button
						type="button"
						class="seg__item"
						aria-pressed={which === 'focus'}
						onclick={() => show('focus')}>Teď</button
					>
				</div>
			{/if}

			{#if areas.length > 0 && which === 'all'}
				<!--
					The areas this board has something in, and „Vše“ in front of
					them. A rail rather than a wrap: over a reel it has to cost
					one line, whatever the set grows to. Four pills fit one now
					(D43); it was ten and the rail is what survived it.
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
{:else if which === 'focus' && hasBoard}
	<!--
		Teď with nothing on it. A page rather than a reel, because there is
		nothing to swipe — and its own screen rather than a silent fall back to
		Vše: the person tapped Teď, so the screen owes them an answer about Teď
		rather than quietly showing them something else (D53).
	-->
	<main class="page" bind:this={emptyFocus}>
		<div class="seg seg--soft empty__reels" role="group" aria-label="Která nástěnka">
			<button type="button" class="seg__item" aria-pressed={false} onclick={() => show('all')}
				>Vše</button
			>
			<button type="button" class="seg__item" aria-pressed={true}>Teď</button>
		</div>

		<section class="card empty">
			<span class="circle circle--lg circle--sky" aria-hidden="true">
				<Icon name="board" size={24} stroke={1.8} />
			</span>
			<h2 class="empty__title">Zatím nic na teď</h2>
			<p class="hint">
				Vyber až {FOCUS_MAX} snů, na které se teď soustředíš. Budeš je mít v pořadí, které jim dáš, bez
				míchání.
			</p>
			<div class="actions actions--fill">
				<a class="btn btn--accent" href={resolve('/ted')}>Vybrat sny</a>
			</div>
		</section>
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
	 *
	 * A page of it is `ui/ReelTile.svelte`, and so are the rules that make one
	 * exactly this tall: a scoped rule left behind by its markup is a rule
	 * that stops matching and says nothing.
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

	/* The two reels, first in the floating chrome: the choice that decides
	   what the areas under it are narrowing. It takes its own width rather
	   than stretching — two words, not a full-width control. */
	.board__reels {
		align-self: flex-start;
	}

	/* On the empty Teď there is a ground again, so the segment is the soft one
	   a card would carry, in the flow above the card that explains. Full
	   width, because `.seg--soft` spends its segments' padding on the
	   assumption of a track that stretches — which is what it does in every
	   form in the app. */
	.empty__reels {
		margin-top: var(--space-3);
	}

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
