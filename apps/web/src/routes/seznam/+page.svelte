<script lang="ts">
	/**
	 * Seznam — every dream as a line, and the fastest way to write a new one.
	 *
	 * The reel is for looking and this is for writing (D44). It is the whole
	 * board in one column, the most recently written at the top, a dream that
	 * is achieved standing in the same column as a dream that is not — the
	 * reel keeps only what is ahead and the Síň slávy only what is behind, so
	 * this is the one screen on which the board is all of itself.
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
	 */
	import { resolve } from '$app/paths';
	import type { Dream, DreamInput } from '@aspire/contracts';
	import { createDream, listBoard } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { listOrder } from '$lib/dreams/board';
	import { photoOf } from '$lib/dreams/photos';
	import { listLine } from '$lib/dreams/rules';
	import { connection } from '$lib/offline/status.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import Icon from '$lib/ui/Icon.svelte';
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

	const rows = $derived(listOrder(dreams));

	const locked = $derived(connection.online ? '' : 'Bez připojení se sen nedá přidat.');

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
</script>

<svelte:head>
	<title>Aspire — seznam</title>
</svelte:head>

<main class="page">
	<div class="head">
		<h1 class="title">Seznam</h1>
		<button
			type="button"
			class="round"
			onclick={open}
			disabled={!connection.online}
			aria-label="Nový sen"
		>
			<Icon name="plus" size={22} stroke={2} />
		</button>
	</div>

	{#if !connection.online}
		<p class="hint">Bez připojení. Seznam je z paměti a nový sen počká na signál.</p>
	{/if}

	{#if rows.length > 0}
		<section class="card card--list">
			{#each rows as dream (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				<a class="row row--press" href={resolve('/sen/[id]', { id: dream.id })}>
					{#if photo}
						<img class="circle shot" src={photo.thumbUrl} alt="" loading="lazy" decoding="async" />
					{:else}
						<!-- No photograph yet: the sky stands in for it, as it does on a tile. -->
						<span class="circle circle--sky" aria-hidden="true"></span>
					{/if}
					<span class="row__body">
						<span class="row__title">{dream.title}</span>
						<span class="row__sub">{listLine(dream)}</span>
					</span>
					<span class="card__go"><Icon name="chevron-right" size={18} /></span>
				</a>
			{/each}
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
			{busy}
			{error}
			{locked}
			onsubmit={add}
			oncancel={close}
		/>
	{/key}
</Sheet>

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

	/* A photograph in the circle's place: the same 40 px disc, cropped. */
	.shot {
		object-fit: cover;
	}
</style>
