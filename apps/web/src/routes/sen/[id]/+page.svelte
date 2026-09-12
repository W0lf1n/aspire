<script lang="ts">
	/**
	 * Sen — one dream, on its own. The tile as the board shows it, with the
	 * photo pill that changes its photograph, then the affirmation, the facts
	 * and the three things you can do: tap the heart, change the words, or
	 * let it go. The board shows the affirmation in the why's place; here
	 * there is room for both, so both are here (D27).
	 *
	 * A dream marked splněno gets a second picker under the first: the
	 * photograph of it having happened, beside the one that was dreamt (D28).
	 * It is offered only while the dream is achieved, and never taken away —
	 * a status changed back leaves the picture where it is.
	 * Deleting asks nothing and says so in a toast, as Prosper does; a dream
	 * is a few words and one photograph, both quick to give back.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream, DreamImageKind } from '@aspire/contracts';
	import {
		deleteDream,
		deleteImage,
		getDream,
		likeDream,
		listBoard,
		uploadImage
	} from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { formatDate } from '$lib/dreams/format';
	import { photosOf } from '$lib/dreams/photos';
	import { photographDone, replacePhotograph } from '$lib/dreams/upload';
	import { CATEGORY_LABEL, STATUS_BADGE, STATUS_CLASS } from '$lib/dreams/rules';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import PhotoPicker from '$lib/ui/PhotoPicker.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';
	import { connection } from '$lib/offline/status.svelte';

	let dream = $state<Dream | null>(null);
	let error = $state('');
	let liking = $state(false);

	/** Which picker is busy, so the other one is not disabled with it. */
	let uploading = $state<DreamImageKind | null>(null);

	const photos = $derived(dream ? photosOf(dream) : { dreamt: null, achieved: null });

	$effect(() => {
		const id = page.params.id ?? '';
		let live = true;
		getDream(id)
			.then((found) => {
				if (live) dream = found;
			})
			.catch(async (e: unknown) => {
				// Without a signal the board's cache still knows this dream, even
				// one never opened on its own: the list carries the same words.
				const remembered = await fromBoard(id);
				if (!live) return;
				if (remembered) dream = remembered;
				else error = describeError(e);
			});
		return () => {
			live = false;
		};
	});

	async function fromBoard(id: string): Promise<Dream | null> {
		try {
			const { dreams } = await listBoard();
			return dreams.find((d) => d.id === id) ?? null;
		} catch {
			return null;
		}
	}

	async function like() {
		if (!dream || liking) return;
		liking = true;
		try {
			dream = await likeDream(dream.id);
			navigator.vibrate?.(10);
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			liking = false;
		}
	}

	/**
	 * The new photograph replaces the old of the same kind. The sequence — out
	 * before in, and only its own kind — is `dreams/upload.ts`, because the
	 * reel does the same thing to a dream that has no photograph yet (D52).
	 */
	async function replacePhoto(picked: Blob, kind: DreamImageKind) {
		if (!dream || uploading) return;
		uploading = kind;
		try {
			dream = await replacePhotograph(dream, picked, kind, { deleteImage, uploadImage, getDream });
			toast.show(photographDone(kind));
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			uploading = null;
		}
	}

	async function remove() {
		if (!dream) return;
		const doomed = dream;
		try {
			await deleteDream(doomed.id);
			toast.show(`„${doomed.title}“ je pryč`);
			await goto(resolve('/'));
		} catch (e) {
			toast.show(describeError(e));
		}
	}
</script>

<svelte:head>
	<title>Aspire — {dream?.title ?? 'sen'}</title>
</svelte:head>

<main class="page">
	<AppBar title="Sen" back="/" />

	{#if dream}
		<PhotoPicker
			current={photos.dreamt?.screenUrl ?? null}
			title={dream.title}
			why={dream.why}
			busy={uploading !== null || !connection.online}
			onpick={(picked) => replacePhoto(picked, 'dreamt')}
			onproblem={(sentence) => toast.show(sentence)}
		/>

		{#if dream.status === 'achieved'}
			<!--
				The proof: the photograph of it having happened, wide under the
				dreamt one so the dreamt one stays the hero. The Síň slávy
				stands the two side by side; this is where the second one is put.
			-->
			<section class="proof">
				<p class="label">Jak to dopadlo</p>
				<PhotoPicker
					current={photos.achieved?.screenUrl ?? null}
					wide
					busy={uploading !== null || !connection.online}
					onpick={(picked) => replacePhoto(picked, 'achieved')}
					onproblem={(sentence) => toast.show(sentence)}
				/>
				<p class="hint">Skutečná fotka toho dne. V Síni slávy pak stojí vedle té vysněné.</p>
			</section>
		{/if}

		<section class="card">
			{#if dream.affirmation}
				<p class="say">{dream.affirmation}</p>
			{/if}

			<dl class="facts">
				<div>
					<dt>Stav</dt>
					<dd>
						<span class="badge {STATUS_CLASS[dream.status]}">{STATUS_BADGE[dream.status]}</span>
					</dd>
				</div>
				<div>
					<dt>Oblast</dt>
					<dd>{dream.category ? CATEGORY_LABEL[dream.category] : '—'}</dd>
				</div>
				<div>
					<dt>Kdy</dt>
					<dd>{dream.targetYear ?? '—'}</dd>
				</div>
				<div>
					<dt>Na nástěnce od</dt>
					<dd>{formatDate(dream.createdAt)}</dd>
				</div>
			</dl>

			{#if connection.online}
				<div class="actions actions--fill">
					<button
						type="button"
						class="btn btn--accent"
						onclick={like}
						disabled={liking}
						aria-label="Palivo"
					>
						<Icon name="heart" size={18} stroke={2} />
						{dream.likes}
					</button>
					<a class="btn" href={resolve('/sen/[id]/upravit', { id: dream.id })}>
						<Icon name="pencil" size={18} stroke={1.8} />
						Upravit
					</a>
					<button type="button" class="btn btn--danger" onclick={remove}>Smazat</button>
				</div>
			{:else}
				<p class="hint">Bez připojení. Srdíčko, úpravy i mazání počkají, až bude signál.</p>
			{/if}
		</section>
	{:else if error}
		<p class="error-text" role="alert">{error}</p>
	{/if}
</main>

<TabBar />

<style>
	/* The second photograph and the two lines that say what it is for; the
	   picker is a tile of its own, so this only stacks them. */
	.proof {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	/* The affirmation, first in the card and before any fact, because it is
	   the only line on this screen written in the person's own voice. Bigger
	   than the facts and a weight above them, and still ink on a card: the
	   photograph above it is the loud half of the screen. */
	.say {
		font-size: var(--text-lg);
		font-weight: 500;
		line-height: var(--leading-base);
		color: var(--ink);
		text-wrap: pretty;
	}
</style>
