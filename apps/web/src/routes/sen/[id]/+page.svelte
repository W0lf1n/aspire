<script lang="ts">
	/**
	 * Sen — one dream, on its own. The tile as the board shows it, then the
	 * facts and the three things you can do: tap the heart, change it, or
	 * let it go. Deleting asks nothing and says so in a toast, as Prosper
	 * does; a dream is a few words and one photograph, both quick to give
	 * back.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream } from '@aspire/contracts';
	import { deleteDream, getDream, likeDream } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { formatDate } from '$lib/dreams/format';
	import { STATUS_BADGE, STATUS_CLASS } from '$lib/dreams/rules';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dream = $state<Dream | null>(null);
	let error = $state('');
	let liking = $state(false);

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
		<article class="dream dream--sky tile">
			<span class="badge dream__tag">{STATUS_BADGE[dream.status]}</span>
			<div class="dream__body">
				<h2 class="dream__title">{dream.title}</h2>
				{#if dream.why}
					<p class="dream__why">{dream.why}</p>
				{/if}
			</div>
		</article>

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

<style>
	.tile {
		flex: none;
	}

	/* On a desktop a 4:5 tile would push the card under the bar; it gives
	   up its ratio before the card gives up its place. */
	@media (min-width: 35rem) {
		.tile {
			max-height: 24rem;
		}
	}
</style>
