<script lang="ts">
	/**
	 * Sen — one dream, on its own. The tile as the board shows it, with the
	 * photo pill that changes its photograph, then the facts and the three
	 * things you can do: tap the heart, change the words, or let it go.
	 * Deleting asks nothing and says so in a toast, as Prosper does; a dream
	 * is a few words and one photograph, both quick to give back.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream } from '@aspire/contracts';
	import { deleteDream, deleteImage, getDream, likeDream, uploadImage } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { formatDate } from '$lib/dreams/format';
	import { STATUS_BADGE, STATUS_CLASS } from '$lib/dreams/rules';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import PhotoPicker from '$lib/ui/PhotoPicker.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dream = $state<Dream | null>(null);
	let error = $state('');
	let liking = $state(false);
	let uploading = $state(false);

	const photo = $derived(dream?.images.find((image) => image.ready) ?? null);

	$effect(() => {
		const id = page.params.id ?? '';
		let live = true;
		getDream(id)
			.then((found) => {
				if (live) dream = found;
			})
			.catch((e: unknown) => {
				if (live) error = describeError(e);
			});
		return () => {
			live = false;
		};
	});

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
	 * The new photograph replaces the old: the old one goes first, so a
	 * failed upload leaves the sky rather than the wrong picture. Then the
	 * dream is asked for again until the sizes are ready — a second or so.
	 */
	async function replacePhoto(picked: Blob) {
		if (!dream || uploading) return;
		uploading = true;
		try {
			for (const image of dream.images) await deleteImage(dream.id, image.id);
			await uploadImage(dream.id, picked);
			for (let attempt = 0; attempt < 10; attempt++) {
				await new Promise((resolve) => setTimeout(resolve, 800));
				dream = await getDream(dream.id);
				if (dream.images.some((image) => image.ready)) break;
			}
			toast.show('Fotka je na nástěnce');
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			uploading = false;
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
			current={photo?.screenUrl ?? null}
			title={dream.title}
			why={dream.why}
			busy={uploading}
			onpick={replacePhoto}
			onproblem={(sentence) => toast.show(sentence)}
		/>

		<section class="card">
			<dl class="facts">
				<div>
					<dt>Stav</dt>
					<dd>
						<span class="badge {STATUS_CLASS[dream.status]}">{STATUS_BADGE[dream.status]}</span>
					</dd>
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
		</section>
	{:else if error}
		<p class="error-text" role="alert">{error}</p>
	{/if}
</main>

<TabBar />
