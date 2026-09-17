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
	 * Deleting asks nothing and says so in a toast, as Prosper does — but the
	 * request is held for as long as that toast stands, and „Vrátit“ cancels
	 * it (D65). A dialog would tax every real deletion to catch the rare
	 * wrong one; waiting taxes none of them.
	 *
	 * And this is where a dream is shared: „Sdílet“ opens a sheet with a link
	 * that is its own key (D61), for somebody who has no app and never will.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream, DreamImageKind } from '@aspire/contracts';
	import {
		addToFocus,
		deleteImage,
		getDream,
		likeDream,
		listBoard,
		moveImage,
		removeFromFocus,
		uploadImage
	} from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { focusFull, focusFullSentence } from '$lib/dreams/focus';
	import { letGo } from '$lib/dreams/letgo';
	import { changedAt } from '$lib/dreams/stats';
	import { formatDate, formatWhen } from '$lib/dreams/format';
	import { photoOf, photosOf, reelUrl } from '$lib/dreams/photos';
	import { CENTRED, sane, type Focal } from '$lib/images/focal';
	import { photographDone, replacePhotograph } from '$lib/dreams/upload';
	import { CATEGORY_LABEL, STATUS_BADGE, STATUS_CLASS } from '$lib/dreams/rules';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import PhotoPicker from '$lib/ui/PhotoPicker.svelte';
	import ShareSheet from '$lib/ui/ShareSheet.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';
	import { connection } from '$lib/offline/status.svelte';

	let dream = $state<Dream | null>(null);
	let error = $state('');
	let liking = $state(false);
	let focusing = $state(false);

	/** Whether the share sheet is up; the link inside it is the sheet's (D61). */
	let sharing = $state(false);

	/**
	 * The board, fetched alongside, only so the Teď pill knows whether the ten
	 * are full before it is tapped (D53). Null until it arrives, and null for
	 * good if it cannot: the pill still works, and the server refuses the
	 * eleventh with the same sentence.
	 */
	let board = $state<Dream[] | null>(null);

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams }) => {
				if (live) board = dreams;
			})
			.catch(() => {
				// No board to count against, so the pill simply asks the server.
			});
		return () => {
			live = false;
		};
	});

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

	/**
	 * On Teď, or off it (D53). The one tap that puts a dream on the second
	 * reel: the ordering is `/ted`'s, and this is the decision.
	 *
	 * The cap is checked here from the board this screen already has, so the
	 * eleventh dream is refused on the tap rather than after a round trip —
	 * the server says the same sentence, and means it.
	 */
	async function toggleFocus() {
		if (!dream || focusing) return;

		const on = dream.focusRank !== null;
		if (!on && board !== null && focusFull(board)) {
			toast.show(focusFullSentence());
			return;
		}

		focusing = true;
		try {
			if (on) {
				await removeFromFocus(dream.id);
				dream = { ...dream, focusRank: null };
				toast.show('Sen je pryč z „teď“');
			} else {
				dream = await addToFocus(dream.id);
				toast.show('Sen máš teď na nástěnce');
			}
			navigator.vibrate?.(10);
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			focusing = false;
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
	async function replacePhoto(picked: Blob, kind: DreamImageKind, at: Focal) {
		if (!dream || uploading) return;
		uploading = kind;
		try {
			dream = await replacePhotograph(
				dream,
				picked,
				kind,
				{ deleteImage, uploadImage, getDream },
				{ focusX: at.x, focusY: at.y, zoom: at.zoom }
			);
			toast.show(photographDone(kind));
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			uploading = null;
		}
	}

	/**
	 * Where a photograph that is already saved is looked at (D54). No file is
	 * touched and nothing is re-sized: the crop is metadata, so this is one
	 * small request and the picture on screen has already moved.
	 */
	async function movePhoto(kind: DreamImageKind, at: Focal) {
		const image = dream ? photoOf(dream, kind) : null;
		if (!dream || !image) return;

		try {
			const moved = await moveImage(dream.id, image.id, {
				focusX: at.x,
				focusY: at.y,
				zoom: at.zoom
			});
			dream = {
				...dream,
				images: dream.images.map((one) => (one.id === moved.id ? moved : one))
			};
			toast.show('Fotka je, kde má být');
		} catch (e) {
			toast.show(describeError(e));
		}
	}

	/** Where each of the two photographs is looked at, for the pickers. */
	function focalOf(kind: DreamImageKind): Focal {
		const image = dream ? photoOf(dream, kind) : null;
		return image ? sane({ x: image.focusX, y: image.focusY, zoom: image.zoom }) : { ...CENTRED };
	}

	/**
	 * Let it go — in a few seconds (D65). Nothing is sent yet: every screen
	 * drops the dream at once and the request goes when the toast does, which
	 * is `dreams/letgo.ts`, because the Seznam's rows do the same (D78).
	 */
	async function remove() {
		if (!dream) return;
		letGo(dream);
		await goto(resolve('/'));
	}
</script>

<svelte:head>
	<title>Aspire — {dream?.title ?? 'sen'}</title>
</svelte:head>

<main class="page">
	<AppBar title="Sen" back="/" />

	{#if dream}
		<PhotoPicker
			current={reelUrl(photos.dreamt)}
			focal={focalOf('dreamt')}
			title={dream.title}
			why={dream.why}
			busy={uploading !== null || !connection.online}
			onpick={(picked, at) => replacePhoto(picked, 'dreamt', at)}
			onmove={(at) => movePhoto('dreamt', at)}
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
					focal={focalOf('achieved')}
					wide
					busy={uploading !== null || !connection.online}
					onpick={(picked, at) => replacePhoto(picked, 'achieved', at)}
					onmove={(at) => movePhoto('achieved', at)}
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
				<div>
					<dt>Naposledy upraveno</dt>
					<dd>{formatWhen(changedAt(dream))}</dd>
				</div>
			</dl>

			{#if connection.online}
				<!--
					On Teď, or not (D53). Its own row above the three that act on
					the dream itself, because this one is about the board rather
					than about the dream: it decides which reel the dream turns
					up on, and the ordering is on Teď's own screen.
				-->
				<div class="actions">
					<button
						type="button"
						class="btn {dream.focusRank === null ? '' : 'btn--accent'}"
						onclick={toggleFocus}
						disabled={focusing || dream.status === 'achieved'}
						aria-pressed={dream.focusRank !== null}
					>
						<Icon name="board" size={18} stroke={1.8} />
						{dream.focusRank === null ? 'Dát na teď' : 'Mám na teď'}
					</button>
					{#if dream.focusRank !== null}
						<a class="btn btn--quiet" href={resolve('/ted')}>Pořadí</a>
					{/if}
				</div>

				{#if dream.status === 'achieved'}
					<p class="hint">Splněný sen na teď nepatří — je za tebou, ne před tebou.</p>
				{/if}

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

				<!--
					Sharing is its own row, below the three that act on the
					dream: those change what the dream is, and this one only
					decides who else can look at it (D61).
				-->
				<div class="actions">
					<button type="button" class="btn btn--quiet" onclick={() => (sharing = true)}>
						<Icon name="link" size={18} stroke={1.8} />
						Sdílet
					</button>
				</div>
			{:else}
				<p class="hint">Bez připojení. Srdíčko, úpravy i mazání počkají, až bude signál.</p>
			{/if}
		</section>
	{:else if error}
		<p class="error-text" role="alert">{error}</p>
	{/if}
</main>

<ShareSheet {dream} open={sharing} onclose={() => (sharing = false)} />

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
