<script lang="ts">
	/**
	 * Teď — choosing the ten, and putting them in order (D53).
	 *
	 * The board's second reel is the handful of dreams being worked on now, in
	 * the person's own order. A dream gets on it from its own screen, with one
	 * pill; this is where the ten are seen together, ordered, and taken off —
	 * which is the part that needs a list rather than a pill.
	 *
	 * Arrows rather than drag. D30 dropped dragging a hundred tiles into an
	 * order and nothing in the app has had it since; ten rows is exactly the
	 * length arrows are for, and a drag handle on a phone competes with the
	 * scroll it sits inside.
	 *
	 * Every change saves the whole order in one request. The ten are never
	 * half-ordered on the server, there is nothing unsaved to lose by leaving
	 * the screen, and a save that fails puts the list back the way it was and
	 * says so.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { listBoard, saveFocus } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { reelDreams } from '$lib/dreams/board';
	import { focusDreams, focusFullSentence } from '$lib/dreams/focus';
	import { photoOf, photoStyle } from '$lib/dreams/photos';
	import { FOCUS_MAX, listLine } from '$lib/dreams/rules';
	import { SEARCH_FROM, searchDreams } from '$lib/dreams/search';
	import { connection } from '$lib/offline/status.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import Sheet from '$lib/ui/Sheet.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[]>([]);
	let asked = $state(false);

	/** The order on the screen: ids, because the order is the thing being edited. */
	let chosen = $state<string[]>([]);

	/** Whether the sheet of everything else is up. */
	let adding = $state(false);

	/** What is typed in the sheet's field. */
	let query = $state('');

	let busy = $state(false);

	const byId = $derived(new Map(dreams.map((dream) => [dream.id, dream])));

	/** The ten, as dreams, in the order being edited. */
	const rows = $derived(
		chosen.map((id) => byId.get(id)).filter((dream): dream is Dream => dream !== undefined)
	);

	/** Everything still ahead that is not already on Teď. */
	const candidates = $derived(reelDreams(dreams).filter((dream) => !chosen.includes(dream.id)));
	const offered = $derived(searchDreams(candidates, query));
	const searchable = $derived(candidates.length >= SEARCH_FROM);

	const full = $derived(chosen.length >= FOCUS_MAX);
	const locked = $derived(!connection.online);

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams: board }) => {
				if (!live) return;
				dreams = board;
				chosen = focusDreams(board).map((dream) => dream.id);
				asked = true;
			})
			.catch(() => {
				// Not paired, or nothing remembered and no signal. The empty
				// state says what this screen is for either way.
				if (live) asked = true;
			});
		return () => {
			live = false;
		};
	});

	/**
	 * The order as it now stands, sent whole. The list on the screen has
	 * already moved — an arrow that waits for a round trip is an arrow that
	 * feels broken — so a refusal puts it back where it was.
	 */
	async function save(next: string[], undo: string[]) {
		chosen = next;
		busy = true;
		try {
			applySaved(await saveFocus(next));
		} catch (e) {
			chosen = undo;
			toast.show(describeError(e));
		} finally {
			busy = false;
		}
	}

	/**
	 * The board brought in line with what the server now says is on Teď. Only
	 * the ten come back, so every dream not among them has no rank — which is
	 * how a dream taken off here stops being on Teď on the board too.
	 */
	function applySaved(saved: Dream[]) {
		const ranks = new Map(saved.map((dream) => [dream.id, dream.focusRank]));
		dreams = dreams.map((dream) => {
			const rank = ranks.get(dream.id) ?? null;
			return dream.focusRank === rank ? dream : { ...dream, focusRank: rank };
		});
	}

	function move(index: number, by: -1 | 1) {
		const to = index + by;
		if (to < 0 || to >= chosen.length) return;

		const next = [...chosen];
		[next[index], next[to]] = [next[to], next[index]];
		void save(next, chosen);
	}

	function take(id: string) {
		void save(
			chosen.filter((other) => other !== id),
			chosen
		);
	}

	function put(id: string) {
		if (full) {
			toast.show(focusFullSentence());
			return;
		}
		void save([...chosen, id], chosen);
		// One tap, one dream: the sheet stays up so the rest can be chosen
		// without opening it again, and the row that was tapped leaves it.
	}
</script>

<svelte:head>
	<title>Aspire — teď</title>
</svelte:head>

<main class="page">
	<AppBar title="Teď" back="/" />

	{#if locked}
		<p class="hint">Bez připojení. Pořadí se dá měnit, až bude signál.</p>
	{/if}

	{#if rows.length > 0}
		<p class="hint">
			Tyhle sny máš na druhé nástěnce, v tomhle pořadí. Nemíchají se — první je první.
		</p>

		<section class="card card--list">
			{#each rows as dream, index (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				<div class="row">
					<span class="row__no">{index + 1}</span>
					{#if photo}
						<img
							class="circle shot"
							src={photo.thumbUrl}
							alt=""
							style={photoStyle(photo)}
							loading="lazy"
							decoding="async"
						/>
					{:else}
						<span class="circle circle--sky" aria-hidden="true"></span>
					{/if}
					<span class="row__body">
						<a class="row__title link--plain" href={resolve('/sen/[id]', { id: dream.id })}>
							{dream.title}
						</a>
						<span class="row__sub">{listLine(dream)}</span>
					</span>
					<span class="row__end ted__acts">
						<button
							type="button"
							class="round round--sm"
							onclick={() => move(index, -1)}
							disabled={busy || locked || index === 0}
							aria-label="Nahoru"
						>
							<span class="ted__up"><Icon name="chevron-down" size={16} stroke={2} /></span>
						</button>
						<button
							type="button"
							class="round round--sm"
							onclick={() => move(index, 1)}
							disabled={busy || locked || index === rows.length - 1}
							aria-label="Dolů"
						>
							<Icon name="chevron-down" size={16} stroke={2} />
						</button>
						<button
							type="button"
							class="round round--sm"
							onclick={() => take(dream.id)}
							disabled={busy || locked}
							aria-label="Odebrat z teď"
						>
							<Icon name="close" size={14} stroke={2} />
						</button>
					</span>
				</div>
			{/each}
		</section>

		<p class="hint count">{rows.length} z {FOCUS_MAX}</p>
	{:else if asked}
		<section class="card">
			<p class="hint">
				Zatím tu nic není. Vyber až {FOCUS_MAX} snů, na které se teď soustředíš — na nástěnce je pak najdeš
				pod „Teď“, v pořadí, které jim dáš.
			</p>
		</section>
	{/if}

	{#if asked}
		<div class="actions actions--fill">
			<button
				type="button"
				class="btn btn--accent"
				onclick={() => {
					query = '';
					adding = true;
				}}
				disabled={locked || full || candidates.length === 0}
			>
				<Icon name="plus" size={18} stroke={2} />
				Přidat sen
			</button>
		</div>

		{#if full}
			<p class="hint">Víc než {FOCUS_MAX} snů na teď nejde. Napřed nějaký odeber.</p>
		{:else if candidates.length === 0 && rows.length > 0}
			<p class="hint">Všechny sny, které tě čekají, už na teď máš.</p>
		{/if}
	{/if}

	<TabBar />
</main>

<Sheet open={adding} title="Přidat na teď" onclose={() => (adding = false)}>
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
		</label>
	{/if}

	{#if offered.length > 0}
		<section class="card card--list sheet__list">
			{#each offered as dream (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				<button type="button" class="row row--press" onclick={() => put(dream.id)} disabled={busy}>
					{#if photo}
						<img
							class="circle shot"
							src={photo.thumbUrl}
							alt=""
							style={photoStyle(photo)}
							loading="lazy"
							decoding="async"
						/>
					{:else}
						<span class="circle circle--sky" aria-hidden="true"></span>
					{/if}
					<span class="row__body">
						<span class="row__title">{dream.title}</span>
						<span class="row__sub">{listLine(dream)}</span>
					</span>
					<span class="card__go"><Icon name="plus" size={18} stroke={2} /></span>
				</button>
			{/each}
		</section>
	{:else}
		<p class="hint">Nic takového mezi zbývajícími sny není.</p>
	{/if}
</Sheet>

<style>
	/* A photograph in the circle's place: the same 40 px disc, cropped. */
	.shot {
		object-fit: cover;
	}

	/* The title is a way into the dream without the row itself being a link:
	   the row's own controls are what it is mostly for. */
	.link--plain {
		color: inherit;
		text-decoration: none;
	}

	/* `.row__end` stacks its slot; these three go across it. */
	.ted__acts {
		flex-direction: row;
		align-items: center;
		gap: var(--space-1);
	}

	/* Up is down, turned over: one chevron in the set does for both, rather
	   than a second path that is the same path upside down. */
	.ted__up {
		display: grid;
		rotate: 180deg;
	}

	.count {
		margin-inline: var(--space-2);
		font-variant-numeric: tabular-nums;
	}

	/* The sheet's list scrolls inside the sheet rather than growing it past
	   the screen: a board of a hundred dreams is a hundred rows. */
	.sheet__list {
		max-height: 50dvh;
		overflow-y: auto;
	}
</style>
