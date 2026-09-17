<script lang="ts">
	/**
	 * A dream's tile once it has two photographs or more (D86): the collage
	 * first, as the reel shows it, and then each photograph on its own, swiped
	 * across the way a post in a feed is.
	 *
	 * A collage is five photographs a third of their size, and the dream's own
	 * screen is where there is time to look at one properly. So the tile is a
	 * row the browser scrolls across and snaps one slide at a time — no
	 * gesture of ours to get wrong, momentum and the edge's give included —
	 * and the dots under it say where in the row it is and take a tap.
	 *
	 * Only here. On the reel a swipe across already means the other reel
	 * (D71), and a tile that kept it for its photographs would take that
	 * gesture away on exactly the dreams that have the most to show.
	 *
	 * The first slide carries the words and whatever the screen puts beside
	 * them; the photographs after it carry nothing, and wear what the
	 * photograph would wear on its own — filling, or whole on its mat (D81).
	 */
	import type { Snippet } from 'svelte';
	import type { DreamImage } from '@aspire/contracts';
	import type { Template } from '$lib/dreams/collage';
	import { matIsBlur, matStyle, photoStyle, reelUrl, tileStyle } from '$lib/dreams/photos';
	import Collage from './Collage.svelte';
	import { offsetOf, slideAt } from './carousel';

	interface Props {
		/** The ready dreamt photographs, in order; the first is the lead. */
		photos: DreamImage[];
		template: Template;
		/** What the first slide says over the collage. */
		children: Snippet;
	}

	let { photos, template, children }: Props = $props();

	let track: HTMLElement | null = $state(null);
	let showing = $state(0);

	/** The collage, and every photograph after it. */
	const count = $derived(photos.length + 1);

	function scrolled() {
		if (track) showing = slideAt(track.scrollLeft, track.clientWidth, count);
	}

	function go(slide: number) {
		if (!track) return;
		const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
		track.scrollTo({
			left: offsetOf(slide, track.clientWidth, count),
			behavior: reduced ? 'auto' : 'smooth'
		});
	}

	// A photograph taken away while the last slide was showing leaves the
	// row shorter than where the dots think it is.
	$effect(() => {
		if (showing > count - 1) showing = count - 1;
	});
</script>

<div class="gallery">
	<article class="dream gallery__frame">
		<!-- svelte-ignore a11y_no_noninteractive_tabindex -->
		<div
			class="gallery__track"
			bind:this={track}
			onscroll={scrolled}
			tabindex="0"
			role="region"
			aria-roledescription="galerie"
			aria-label="Fotky snu"
		>
			<div
				class="gallery__slide gallery__slide--lead"
				role="group"
				aria-label={`Koláž, 1 z ${count}`}
			>
				<Collage {photos} {template} loading="eager" />
				<div class="dream__body">
					{@render children()}
				</div>
			</div>

			{#each photos as photo, index (photo.id)}
				<div
					class="gallery__slide"
					role="group"
					aria-label={`Fotka ${index + 1}, ${index + 2} z ${count}`}
					style={matStyle(photo)}
				>
					{#if matIsBlur(photo)}
						<img
							class="dream__under"
							src={photo.thumbUrl}
							alt=""
							aria-hidden="true"
							style={photoStyle(photo)}
							loading="lazy"
						/>
					{/if}
					<img
						class="dream__img"
						src={reelUrl(photo)}
						alt=""
						style={tileStyle(photo)}
						loading="lazy"
						decoding="async"
					/>
				</div>
			{/each}
		</div>
	</article>

	<div class="gallery__dots" role="group" aria-label="Snímky">
		{#each Array.from({ length: count }, (_unused, slide) => slide) as slide (slide)}
			<button
				type="button"
				class="gallery__dot"
				aria-current={showing === slide ? 'true' : undefined}
				aria-label={slide === 0 ? 'Koláž' : `Fotka ${slide}`}
				onclick={() => go(slide)}
			></button>
		{/each}
	</div>
</div>

<style>
	.gallery {
		flex: none;
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	/* The print, as `PhotoPicker`'s is: on a desktop a 4:5 tile would push
	   the card under the bar, so it gives up its ratio first. */
	@media (min-width: 35rem) {
		.gallery__frame {
			max-height: 24rem;
		}
	}

	/* The scrim is the first slide's alone: a photograph on its own has no
	   words over it to read. */
	.gallery__frame::after {
		content: none;
	}

	/* The row: one slide wide, scrolled across and snapped to every slide.
	   `contain` so a swipe past the last photograph does not turn into the
	   browser's own back gesture. */
	.gallery__track {
		position: absolute;
		inset: 0;
		display: flex;
		overflow-x: auto;
		overflow-y: hidden;
		overscroll-behavior-x: contain;
		scroll-snap-type: x mandatory;
		scrollbar-width: none;
		border-radius: inherit;
	}

	.gallery__track::-webkit-scrollbar {
		display: none;
	}

	.gallery__track:focus-visible {
		outline: 2px solid var(--signal);
		outline-offset: -2px;
	}

	/* A whole slide per swipe, however hard the swipe. */
	.gallery__slide {
		position: relative;
		flex: 0 0 100%;
		display: flex;
		flex-direction: column;
		justify-content: flex-end;
		overflow: hidden;
		scroll-snap-align: start;
		scroll-snap-stop: always;
		isolation: isolate;
	}

	.gallery__slide--lead::after {
		content: '';
		position: absolute;
		inset: 0;
		background: var(--scrim);
		pointer-events: none;
	}

	.gallery__dots {
		display: flex;
		justify-content: center;
	}

	/* A dot you can hit: the button is a finger's worth, the dot inside it
	   is six pixels, and the one showing stretches into a short bar. */
	.gallery__dot {
		display: grid;
		place-items: center;
		width: 20px;
		height: 20px;
	}

	.gallery__dot::before {
		content: '';
		width: 6px;
		height: 6px;
		border-radius: var(--radius-full);
		background: var(--ink-3);
		transition:
			width var(--dur-fast) var(--ease-out),
			background var(--dur-fast) var(--ease-out);
	}

	.gallery__dot[aria-current='true']::before {
		width: 14px;
		background: var(--ink);
	}
</style>
