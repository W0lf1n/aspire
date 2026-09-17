<script lang="ts">
	/**
	 * Seznam — every dream as a line, and the fastest way to write a new one.
	 *
	 * The reel is for looking and this is for writing (D44). It is the whole
	 * board in one column, a dream that is achieved standing in the same column
	 * as a dream that is not — the reel keeps only what is ahead and the Síň
	 * slávy only what is behind, so this is the one screen on which the board
	 * is all of itself.
	 *
	 * **The order is his** (D79). A new dream still lands on the first line,
	 * where the person writing it is looking, and from there a line goes where
	 * it is dragged — by its grip, or by the line itself after a long press —
	 * or where its number says once that has been typed over. Both are one
	 * move, `dreams/order.ts`, and the reel is not told: Vše is still shuffled
	 * (D30), and this is the inventory, not the queue.
	 *
	 * ＋ opens a sheet rather than a screen: five fields, no photograph, and
	 * the dream is saved as „sním“, which is the state a dream is in the
	 * second it is written down. Přidat — the ⊕ on the bar — is still the way
	 * in when the dream arrives as a picture; this is the way in when it
	 * arrives as a sentence, and the photograph is added later on the dream's
	 * own screen.
	 *
	 * The saved dream goes straight on the top of the list rather than the
	 * board being fetched again: the server has just said what it made, and
	 * a list that reloads under a thumb is a list that loses its place.
	 *
	 * Every line is numbered and the number is the dream's place in the whole
	 * list, so it holds while the list is being searched: three lines
	 * numbered 4, 17 and 38 say where in the column they are, which is how
	 * you get back to one after the field is empty again (D48). A narrowed
	 * list cannot be reordered for the same reason — between 4 and 17 there
	 * are twelve lines nobody can see, and „above 17“ does not say which.
	 *
	 * The field above them appears from six lines up and narrows the list
	 * against everything a dream says about itself — `dreams/search.ts`
	 * (D49). It runs here rather than on the server, over the board this
	 * screen has already fetched, so it answers on the keystroke and answers
	 * offline.
	 *
	 * Above both is the board counted (D77): how many dreams, how many in each
	 * state, and when any of them was last changed. A number is a button — it
	 * narrows the list to the dreams it counted, the way the field narrows it
	 * to the ones it found, and the two stack. `dreams/stats.ts`.
	 *
	 * A line swiped to the left shows its hearts, Sdílet and Smazat (D78),
	 * `ui/DreamRow.svelte`.
	 */
	import { DREAM_STATUSES, type Dream, type DreamInput } from '@aspire/contracts';
	import { createDream, likeDream, listBoard, placeDream } from '$lib/api/client';
	import { deleting } from '$lib/dreams/deleting.svelte';
	import { describeError } from '$lib/api/errors';
	import { listOrder } from '$lib/dreams/board';
	import { formatWhen } from '$lib/dreams/format';
	import { letGo } from '$lib/dreams/letgo';
	import { placed, serverPlace } from '$lib/dreams/order';
	import { STATUS_BADGE } from '$lib/dreams/rules';
	import { SEARCH_FROM, searchDreams } from '$lib/dreams/search';
	import { boardStats, byStatus, type StatusFilter } from '$lib/dreams/stats';
	import { connection } from '$lib/offline/status.svelte';
	import { cannot, writes } from '$lib/offline/writes.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import DreamRow from '$lib/ui/DreamRow.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import { edgeScroll, heldTo, landsOn, stepsAside } from '$lib/ui/reorder';
	import ShareSheet from '$lib/ui/ShareSheet.svelte';
	import Sheet from '$lib/ui/Sheet.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[]>([]);

	/** Whether the board has been asked for yet; an empty list is not nothing. */
	let asked = $state(false);

	/** Whether the sheet is up. The sheet asks; this decides (`Sheet`). */
	let writing = $state(false);

	/**
	 * Which time this is. The form seeds from its props once, so a new one is
	 * what empties the fields — and it is keyed on the way in, not on the way
	 * out, so the sheet still has its words in it while it sinks.
	 */
	let attempt = $state(0);

	let busy = $state(false);
	let error = $state('');

	/** What is typed in the search field. Empty is the whole list. */
	let query = $state('');

	// Less whatever is inside its undo window, so a dream deleted on its own
	// screen is not still in the list a tab away (D65).
	const rows = $derived(listOrder(dreams.filter((one) => !deleting.has(one.id))));

	/** Which line each dream is, counted down the whole list from the top. */
	const place = $derived(new Map(rows.map((dream, i) => [dream.id, i + 1])));

	/** The board counted, and the one state the list is narrowed to, if any. */
	const stats = $derived(boardStats(rows));
	let only = $state<StatusFilter>(null);

	const found = $derived(searchDreams(byStatus(rows, only), query));

	/** Whether the field is worth having at all, and whether it is being used. */
	const searchable = $derived(rows.length >= SEARCH_FROM);
	const searching = $derived(searchable && query.trim().length > 0);

	/** Whether the list is less than the whole of itself, for either reason. */
	const narrowed = $derived(searching || only !== null);

	/** Whether a line can go anywhere: the whole list, a signal, and somewhere to go. */
	const movable = $derived(!narrowed && connection.online && rows.length > 1);

	function everything() {
		query = '';
		only = null;
	}

	const locked = $derived(cannot(connection.online, 'add'));

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams: board }) => {
				if (!live) return;
				dreams = board;
				asked = true;
			})
			.catch(() => {
				// Not paired, or nothing remembered and no signal. An empty list
				// either way, and its empty state says how a dream gets onto it.
				if (live) asked = true;
			});
		return () => {
			live = false;
		};
	});

	function open() {
		error = '';
		attempt += 1;
		writing = true;
	}

	function close() {
		if (busy) return;
		writing = false;
	}

	async function add(input: DreamInput) {
		busy = true;
		error = '';
		try {
			dreams = [await createDream(input), ...dreams];
			writing = false;
			toast.show('Sen je v seznamu');
		} catch (e) {
			error = describeError(e);
		} finally {
			busy = false;
		}
	}

	// ── behind a swipe (D78) ────────────────────────────────────────────────

	/** The one line whose tray is open. Opening another shuts it. */
	let opened = $state<string | null>(null);

	/** The dream the share sheet is up for; it keeps it while it sinks. */
	let shared = $state<Dream | null>(null);
	let sharing = $state(false);

	async function like(dream: Dream) {
		try {
			const liked = await likeDream(dream.id);
			dreams = dreams.map((one) => (one.id === liked.id ? liked : one));
			navigator.vibrate?.(10);
		} catch (e) {
			toast.show(describeError(e));
		}
	}

	function share(dream: Dream) {
		shared = dream;
		sharing = true;
	}

	/** A tap anywhere but on the open line shuts its tray, as a scroll does. */
	function elsewhere(event: PointerEvent) {
		if (opened === null) return;
		const row = (event.target as HTMLElement).closest('[data-row]');
		if (row?.getAttribute('data-row') !== opened) opened = null;
	}

	// ── his own order (D79) ─────────────────────────────────────────────────

	let scroller: HTMLElement | null = $state(null);
	let list: HTMLElement | null = $state(null);

	/** The line in the hand: which, from where, to where, and how far it has gone. */
	let drag = $state<{ id: string; from: number; to: number; dy: number; row: number } | null>(null);

	/** Where the finger went down and where it is, and how far the list has scrolled since. */
	let startY = 0;
	let lastY = 0;
	let startScroll = 0;
	let frame = 0;

	/**
	 * One move, from either of the two ways to ask for it. The list has moved
	 * before the server is told — a row that waits for a round trip is a row
	 * that feels stuck — so a refusal puts the board back as it was.
	 */
	async function moveTo(dream: Dream, to: number) {
		if (!movable || place.get(dream.id) === to) return;

		const before = dreams;
		// Everything the server still has: a dream inside its undo window is
		// off this screen and still in the server's count (`serverPlace`).
		const held = dreams.filter((one) => !deleting.has(one.id) || deleting.holds(one.id));
		const line = serverPlace(rows, held, dream.id, to);

		dreams = placed(held, dream.id, line);
		try {
			await placeDream(dream.id, line);
		} catch (e) {
			dreams = before;
			toast.show(describeError(e));
		}
	}

	function grab(dream: Dream, at: { clientY: number }) {
		if (!movable || drag) return;

		const from = rows.findIndex((one) => one.id === dream.id);
		const row = list?.querySelector(`[data-row="${dream.id}"]`)?.getBoundingClientRect().height;
		if (from < 0 || !row) return;

		opened = null;
		startY = lastY = at.clientY;
		startScroll = scroller?.scrollTop ?? 0;
		drag = { id: dream.id, from, to: from, dy: 0, row };

		window.addEventListener('pointermove', dragged);
		window.addEventListener('pointerup', dropped);
		window.addEventListener('pointercancel', dropped);
		frame = requestAnimationFrame(tick);
	}

	/** Where the line is now: the finger's travel, plus what the list scrolled under it. */
	function follow() {
		if (!drag) return;
		const travelled = lastY - startY + ((scroller?.scrollTop ?? 0) - startScroll);
		const dy = heldTo(travelled, drag.from, drag.row, rows.length);
		drag = { ...drag, dy, to: landsOn(drag.from, dy, drag.row, rows.length) };
	}

	function dragged(event: PointerEvent) {
		lastY = event.clientY;
		follow();
	}

	/** Near either edge of the list the list moves, so a line can go further than one screen. */
	function tick() {
		if (!drag || !scroller) return;
		const box = scroller.getBoundingClientRect();
		const by = edgeScroll(lastY, box.top, box.bottom);
		if (by !== 0) {
			scroller.scrollTop += by;
			follow();
		}
		frame = requestAnimationFrame(tick);
	}

	function letGoOfRow() {
		cancelAnimationFrame(frame);
		window.removeEventListener('pointermove', dragged);
		window.removeEventListener('pointerup', dropped);
		window.removeEventListener('pointercancel', dropped);
	}

	function dropped(event: PointerEvent) {
		const done = drag;
		letGoOfRow();
		drag = null;
		if (!done || event.type === 'pointercancel' || done.to === done.from) return;

		const dream = rows[done.from];
		if (dream) void moveTo(dream, done.to + 1);
	}

	/**
	 * A finger that is holding a line must not also scroll the list. The
	 * browser decides that on `touchmove`, and only listens to a listener that
	 * was there — and not passive — before the touch began, so this one is on
	 * the list for as long as the list is, and does nothing until a line is in
	 * the hand.
	 */
	$effect(() => {
		const el = list;
		if (!el) return;
		const hold = (event: TouchEvent) => {
			if (drag && event.cancelable) event.preventDefault();
		};
		el.addEventListener('touchmove', hold, { passive: false });
		return () => el.removeEventListener('touchmove', hold);
	});

	// A screen left mid-drag takes its listeners with it.
	$effect(() => letGoOfRow);
</script>

<svelte:head>
	<title>Aspire — seznam</title>
</svelte:head>

<svelte:window onpointerdowncapture={elsewhere} />

<main class="page" bind:this={scroller} onscroll={() => (opened = null)}>
	<div class="head">
		<h1 class="title">Seznam</h1>
		<button type="button" class="round" onclick={open} use:writes aria-label="Nový sen">
			<Icon name="plus" size={22} stroke={2} />
		</button>
	</div>

	{#if !connection.online}
		<p class="hint">Bez připojení. Seznam je z paměti a nový sen i pořadí počkají na signál.</p>
	{/if}

	{#if stats.total > 0}
		<section class="card stats" aria-label="Nástěnka v číslech">
			<button
				type="button"
				class="stats__item"
				aria-pressed={only === null}
				onclick={() => (only = null)}
			>
				<span class="stats__n">{stats.total}</span>
				<span class="stats__label">celkem</span>
			</button>
			{#each DREAM_STATUSES as status (status)}
				<button
					type="button"
					class="stats__item"
					aria-pressed={only === status}
					onclick={() => (only = only === status ? null : status)}
				>
					<span class="stats__n">{stats.by[status]}</span>
					<span class="stats__label">{STATUS_BADGE[status]}</span>
				</button>
			{/each}
		</section>
		{#if stats.lastChange}
			<p class="hint count">Naposledy upraveno {formatWhen(stats.lastChange)}</p>
		{/if}
	{/if}

	{#if searchable}
		<label class="search">
			<Icon name="search" size={18} />
			<input
				class="search__input"
				type="search"
				bind:value={query}
				placeholder="Hledat mezi sny"
				autocomplete="off"
				aria-label="Hledat mezi sny"
			/>
			{#if query.length > 0}
				<button
					type="button"
					class="search__clear"
					onclick={() => (query = '')}
					aria-label="Zrušit hledání"
				>
					<Icon name="close" size={14} stroke={2} />
				</button>
			{/if}
		</label>
	{/if}

	{#if narrowed}
		<p class="hint count" aria-live="polite">{found.length} z {rows.length}</p>
	{/if}

	{#if found.length > 0}
		<section class="card card--list lines" class:lines--moving={drag !== null} bind:this={list}>
			{#each found as dream, index (dream.id)}
				<DreamRow
					{dream}
					place={place.get(dream.id) ?? index + 1}
					count={rows.length}
					{movable}
					open={opened === dream.id}
					lifted={drag?.id === dream.id}
					aside={drag === null
						? 0
						: drag.id === dream.id
							? drag.dy
							: stepsAside(index, drag.from, drag.to, drag.row)}
					onopen={(is) => (opened = is ? dream.id : null)}
					onlike={like}
					onshare={share}
					onremove={letGo}
					onplace={moveTo}
					ongrab={grab}
				/>
			{/each}
		</section>
	{:else if narrowed}
		<section class="card">
			<p class="hint">Nic takového v seznamu není.</p>
			<div class="actions actions--fill">
				<button type="button" class="btn" onclick={everything}>Ukázat všechno</button>
			</div>
		</section>
	{:else if asked}
		<section class="card">
			<p class="hint">
				Zatím tu není nic. Napiš sen jednou větou — fotku mu dáš, až na ni narazíš.
			</p>
			<div class="actions actions--fill">
				<button type="button" class="btn btn--accent" onclick={open} disabled={!!locked}>
					Napsat sen
				</button>
			</div>
		</section>
	{/if}

	<TabBar />
</main>

<Sheet open={writing} title="Nový sen" onclose={close}>
	{#key attempt}
		<DreamForm
			submitLabel="Přidat"
			accent
			withStatus={false}
			existing={dreams}
			{busy}
			{error}
			{locked}
			onsubmit={add}
			oncancel={close}
		/>
	{/key}
</Sheet>

<ShareSheet dream={shared} open={sharing} onclose={() => (sharing = false)} />

<style>
	/* The screen's name with the one control it has on its right, on the
	   title's own line — the list below it is then a column of nothing but
	   dreams. */
	.head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-3);
	}

	/* How much of the list the search left, over the list it left it of. */
	.count {
		margin-inline: var(--space-2);
		font-variant-numeric: tabular-nums;
	}

	/* The card clips its lines, so a face that slides aside and a line's own
	   opaque ground both stop at the card's corners. */
	.lines {
		overflow: hidden;
	}

	/* While a line is in the hand the others ease out of its way. Only then:
	   the moment it lands the list is simply in its new order, and a line that
	   eased back from where it had stepped to would be seen moving twice. */
	.lines--moving :global(.swipe:not(.swipe--lifted)) {
		transition: translate var(--dur-fast) var(--ease-out);
	}
</style>
