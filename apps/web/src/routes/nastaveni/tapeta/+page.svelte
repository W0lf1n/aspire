<script lang="ts">
	/**
	 * Nastavení · Tapeta — the lock-screen collage (PLAN.md §3.5).
	 *
	 * Pick up to six dreams, in the order they should appear, and the server
	 * draws them onto a canvas the size of this phone's own screen. Nothing
	 * is stored: the image comes back once and goes wherever the phone puts
	 * it. The point is the dream seen a hundred times a day without opening
	 * anything (D33).
	 */
	import type { Dream } from '@aspire/contracts';
	import { listBoard, wallpaper } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { pickDaily } from '$lib/dreams/board';
	import { photoOf } from '$lib/dreams/photos';
	import {
		MAX_ON_WALLPAPER,
		canvasFor,
		toggleChosen,
		wallpaperCandidates,
		wallpaperPick
	} from '$lib/dreams/wallpaper';
	import { connection } from '$lib/offline/status.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[]>([]);
	let chosen = $state<string[]>([]);
	let making = $state(false);

	/** The last collage made, kept so the preview is what was actually drawn. */
	let made = $state<string | null>(null);

	const candidates = $derived(wallpaperCandidates(dreams));
	const left = $derived(MAX_ON_WALLPAPER - chosen.length);

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams: rows }) => {
				if (!live) return;
				dreams = rows;
				// The screen opens on a collage rather than on a question: the
				// day's dream and the ones with the most fuel, already chosen
				// (D58). Worked out once, here, because the pick draws at
				// random among dreams never shown and a derived value that
				// moves is a choice that rearranges itself under a thumb.
				chosen = wallpaperPick(rows, pickDaily(rows)?.id ?? null).map((dream) => dream.id);
			})
			.catch(() => {
				if (live) dreams = [];
			});
		return () => {
			live = false;
		};
	});

	// A blob URL outlives the page unless it is let go of.
	$effect(() => () => {
		if (made) URL.revokeObjectURL(made);
	});

	function choose(id: string) {
		const next = toggleChosen(chosen, id);
		if (next === chosen) {
			toast.show(`Víc než ${MAX_ON_WALLPAPER} snů se na tapetu nevejde.`);
			return;
		}
		chosen = next;
	}

	/**
	 * The collage, and then the phone's own way of keeping it: the share
	 * sheet where there is one, because on a phone that is where „Uložit
	 * obrázek“ lives and a download link is not. A browser without it gets
	 * the link instead.
	 */
	async function make() {
		if (making || chosen.length === 0) return;
		making = true;
		try {
			const canvas = canvasFor(window.screen.width, window.screen.height, devicePixelRatio);
			const image = await wallpaper(chosen, canvas);

			if (made) URL.revokeObjectURL(made);
			made = URL.createObjectURL(image);

			const file = new File([image], 'aspire-tapeta.jpg', { type: 'image/jpeg' });
			if (navigator.canShare?.({ files: [file] })) {
				await navigator.share({ files: [file] });
			} else {
				const link = document.createElement('a');
				link.href = made;
				link.download = 'aspire-tapeta.jpg';
				link.click();
			}
		} catch (e) {
			// A share the person backed out of is not a failure worth a sentence.
			if (e instanceof DOMException && e.name === 'AbortError') return;
			toast.show(describeError(e));
		} finally {
			making = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — tapeta</title>
</svelte:head>

<main class="page">
	<AppBar title="Tapeta" back="/nastaveni" />

	{#if !connection.online}
		<p class="hint">Bez připojení. Tapetu skládá server, takže tahle počká na signál.</p>
	{:else if candidates.length === 0}
		<section class="card empty">
			<span class="circle circle--lg circle--sky"><Icon name="image" size={24} stroke={1.8} /></span
			>
			<h2 class="empty__title">Zatím není z čeho</h2>
			<p class="hint">Na tapetu jdou sny, které mají fotku. Přidej ji některému a vrať se sem.</p>
		</section>
	{:else}
		<p class="hint">
			Dnešní sny už jsou vybrané — ťuknutím je měníš. Skládají se v pořadí, ve kterém je ťukneš, na
			plochu velkou jako displej tohohle telefonu.
		</p>

		<!--
			The choice: the photographs themselves, small and square, because
			this is a screen about pictures and a list of titles would be a
			screen about words. The number on a chosen one is its place on the
			collage, which is the only thing the order means.
		-->
		<section class="pick">
			{#each candidates as dream (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				{@const place = chosen.indexOf(dream.id)}
				<button
					type="button"
					class="tile"
					class:tile--on={place >= 0}
					aria-pressed={place >= 0}
					aria-label={dream.title}
					onclick={() => choose(dream.id)}
				>
					{#if photo}
						<img class="tile__img" src={photo.thumbUrl} alt="" loading="lazy" decoding="async" />
					{/if}
					{#if place >= 0}
						<span class="badge tile__place">{place + 1}</span>
					{/if}
				</button>
			{/each}
		</section>

		<div class="actions actions--fill">
			<button
				type="button"
				class="btn btn--accent"
				disabled={making || chosen.length === 0}
				onclick={make}
			>
				<Icon name="image" size={18} stroke={1.8} />
				{making ? 'Skládám…' : 'Udělat tapetu'}
			</button>
		</div>
		<p class="hint">
			{chosen.length === 0
				? 'Ťukni na fotku.'
				: left > 0
					? `Vybráno ${chosen.length}, vejde se ještě ${left}.`
					: 'Plno. Ťuknutím na vybranou ji zase sundáš.'}
		</p>

		{#if made}
			<section class="card">
				<p class="label">Hotová tapeta</p>
				<img class="made" src={made} alt="Složená tapeta" />
				<p class="hint">
					Podrž na ní prst a ulož ji do fotek. Pak ji telefon nastaví jako pozadí sám.
				</p>
			</section>
		{/if}
	{/if}
</main>

<TabBar />

<style>
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

	/* Three across on a phone, and as many as fit above that: the tiles are
	   the thumbnails the media store already makes, so the grid costs one
	   small file each. */
	.pick {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(6.5rem, 1fr));
		gap: var(--space-2);
	}

	.tile {
		position: relative;
		aspect-ratio: 1;
		overflow: hidden;
		border-radius: var(--radius-md);
		background: var(--surface-3);
		transition:
			transform var(--dur-fast) var(--ease-out),
			outline-color var(--dur-fast) var(--ease-out);
		outline: 2px solid transparent;
		outline-offset: -2px;
	}

	.tile:active {
		transform: scale(0.98);
	}

	/* Chosen is the ember outline and the number; the photograph is not
	   dimmed, because dimming the picture is the one thing this screen is
	   not allowed to do to it. */
	.tile--on {
		outline-color: var(--signal);
	}

	.tile__img {
		width: 100%;
		height: 100%;
		object-fit: cover;
	}

	.tile__place {
		position: absolute;
		top: var(--space-2);
		left: var(--space-2);
		background: var(--signal);
		color: var(--signal-ink);
	}

	/* The result, at the shape it will actually be. */
	.made {
		width: 100%;
		max-width: 14rem;
		align-self: center;
		border-radius: var(--radius-md);
	}
</style>
