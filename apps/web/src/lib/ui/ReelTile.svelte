<script lang="ts">
	/**
	 * One dream, one page of the reel: the photograph edge to edge, the title
	 * and its line at the foot over the scrim, and the two things a dream can
	 * be given from the board — a tap on the heart, and a photograph for a
	 * tile that has none (D52, D58).
	 *
	 * It is a page of a pager, so its height is exactly the scrollport and
	 * nothing here may add to it (rule 14): the chrome floats, the picture is
	 * absolute, and the controls live inside the body that was already there.
	 *
	 * The whole picture is a link to the dream; the controls sit above it, so
	 * a tap on one of them is that control and a tap anywhere else is the
	 * dream. The styles for all of it are here rather than on the board,
	 * because a scoped rule that stays behind when its markup moves is a rule
	 * that silently stops matching.
	 *
	 * **Two photographs or more are a row swiped across** (D87): the collage
	 * and then each photograph, or the photographs alone, as the dream says
	 * (`dreams/slides.ts`). The row is the browser's own sideways scroller,
	 * inside the page and no taller than it, so rule 14 holds; every slide is
	 * the link, and the words, the heart and the dots stay where they are while
	 * the photographs pass under them. Past the last photograph the same swipe
	 * is the other reel — `sideways.ts` asks the row, which is why it wears
	 * `data-slides`.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import type { DreamImage } from '@aspire/contracts';
	import {
		matIsBlur,
		matStyle,
		photoComing,
		photoOf,
		photoStyle,
		reelUrl,
		tileStyle
	} from '$lib/dreams/photos';
	import { tileLine } from '$lib/dreams/board';
	import { CATEGORY_LABEL, STATUS_BADGE } from '$lib/dreams/rules';
	import { slideKey, slidesOf } from '$lib/dreams/slides';
	import { writes } from '$lib/offline/writes.svelte';
	import Collage from '$lib/ui/Collage.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import { slideAt } from './carousel';

	interface Props {
		dream: Dream;
		/**
		 * Whether this is the tile on the screen or one of its neighbours: a
		 * photograph that decodes mid-swipe is the one thing that can make a
		 * reel stutter, so those three are fetched and decoded ahead.
		 */
		eager?: boolean;
		/**
		 * The downscaled file being uploaded onto this tile, if it is this one:
		 * the sky becomes the picture on the tap rather than eight seconds
		 * later (D52).
		 */
		preview?: string | null;
		/**
		 * Whether a photograph can be started at all: false while another
		 * upload is in flight. The connection half of the lock is `writes`'s,
		 * not the caller's.
		 */
		canPick?: boolean;
		onlike: (dream: Dream) => void;
		onpick: (dream: Dream, event: Event) => void;
	}

	const { dream, eager = false, preview = null, canPick = false, onlike, onpick }: Props = $props();

	const photo = $derived(photoOf(dream, 'dreamt'));

	/**
	 * Two photographs or more are slides — the collage and each photograph,
	 * or the photographs alone (D87); one is the photograph, exactly as it
	 * was. The cover is still `photo` either way — it is what says the tile
	 * has a picture at all.
	 */
	const slides = $derived(slidesOf(dream));
	const line = $derived(tileLine(dream));

	/** Which slide is showing, for the dots. */
	let row: HTMLElement | null = $state(null);
	let showing = $state(0);

	function scrolled() {
		if (row) showing = slideAt(row.scrollLeft, row.clientWidth, slides.length);
	}

	// A photograph taken away while the last slide was showing.
	$effect(() => {
		if (showing > Math.max(0, slides.length - 1)) showing = Math.max(0, slides.length - 1);
	});

	/** This tile is the one having its photograph taken. */
	const busy = $derived(preview !== null);

	/** The row is up, rather than the one picture or the file being saved. */
	const sliding = $derived(slides.length > 0 && !busy);

	/**
	 * A photograph uploaded and not yet resized. The tile paints the sky for
	 * it exactly as it does for a dream that has none, and only this tells
	 * the two apart (D52 amended).
	 */
	const coming = $derived(photoComing(dream, 'dreamt'));

	/** The saved photograph, or the one being saved. */
	const src = $derived(preview ?? reelUrl(photo));

	const loading = $derived<'eager' | 'lazy'>(eager ? 'eager' : 'lazy');

	/**
	 * A photograph shown whole stands on a mat (D81): one of the five colours,
	 * painted by the tile, or the thumb blurred under it. Filling, the thumb is
	 * under the picture for another reason — the shape of the photograph before
	 * its pixels (D63) — and it would show through the mat around a picture
	 * that does not cover it, so on a colour it is left out.
	 */
	const mat = $derived(busy || sliding ? '' : matStyle(photo));
	const under = $derived(photo !== null && underFor(photo));

	function underFor(image: DreamImage): boolean {
		return image.fit !== 'whole' || matIsBlur(image);
	}
</script>

<article class="dream reel__tile" class:dream--sky={!src && !sliding} style={mat}>
	{#if sliding}
		<div class="reel__slides" data-slides bind:this={row} onscroll={scrolled}>
			{#each slides as slide, index (slideKey(slide))}
				<!--
					Every slide is the dream's link, so a tap anywhere on the row
					opens it and a swipe across it is the browser's. Only the
					first is announced: the rest are the same link again.
				-->
				<a
					class="reel__slide"
					href={resolve('/sen/[id]', { id: dream.id })}
					draggable="false"
					style={slide.kind === 'photo' ? matStyle(slide.photo) : ''}
					tabindex={index === 0 ? undefined : -1}
					aria-hidden={index === 0 ? undefined : 'true'}
					aria-label={index === 0 ? dream.title : undefined}
				>
					{#if slide.kind === 'collage'}
						<Collage
							photos={slide.photos}
							template={slide.template}
							loading={index === 0 ? loading : 'lazy'}
						/>
					{:else}
						{#if underFor(slide.photo)}
							<img
								class="dream__under"
								src={slide.photo.thumbUrl}
								alt=""
								aria-hidden="true"
								style={photoStyle(slide.photo)}
								loading={index === 0 ? loading : 'lazy'}
								decoding="async"
							/>
						{/if}
						<img
							class="dream__img"
							src={reelUrl(slide.photo)}
							alt=""
							style={tileStyle(slide.photo)}
							draggable="false"
							loading={index === 0 ? loading : 'lazy'}
							decoding="async"
						/>
					{/if}
				</a>
			{/each}
		</div>
	{:else if src}
		{#if photo && under && !busy}
			<!-- The thumb, blurred, under the picture: the shape of the
			     photograph arrives with its first kilobytes while the rest is
			     on its way (D63). Absolute in the same frame, so it adds no
			     height to the pager. -->
			<img
				class="dream__under"
				src={photo.thumbUrl}
				alt=""
				aria-hidden="true"
				style={photoStyle(photo)}
				{loading}
				decoding="async"
			/>
		{/if}
		<img
			class="dream__img"
			{src}
			alt=""
			style={busy ? '' : tileStyle(photo)}
			{loading}
			decoding="async"
		/>
	{/if}
	<span class="badge dream__tag">
		{STATUS_BADGE[dream.status]}{dream.category ? ` · ${CATEGORY_LABEL[dream.category]}` : ''}
	</span>
	{#if !sliding}
		<a class="reel__open" href={resolve('/sen/[id]', { id: dream.id })} aria-label={dream.title}
		></a>
	{/if}
	<div class="dream__body reel__body">
		{#if sliding}
			<!-- Where in the row the tile is: over the photograph, in its ink. -->
			<div class="reel__dots" aria-hidden="true">
				{#each slides as slide, index (slideKey(slide))}
					<span class="reel__dot" class:reel__dot--on={index === showing}></span>
				{/each}
			</div>
		{/if}
		<h2 class="dream__title">{dream.title}</h2>
		{#if line.text}
			<p class="dream__why" class:dream__say={line.said}>{line.text}</p>
		{/if}
		<div class="reel__acts">
			<!--
				The heart, and no number beside it (D58). The count is a
				measurement, and a measurement on a photograph is the one thing
				§2's third principle keeps off the board; it is read on the
				dream's own screen, where the facts are. What the tile says is
				the one bit that matters — whether this dream has been fuelled
				at all — and the tap answers with the haptic and a heart that
				fills.
			-->
			<button
				type="button"
				class="btn btn--photo reel__fuel"
				class:reel__fuel--lit={dream.likes > 0}
				onclick={() => onlike(dream)}
				use:writes
				aria-label={`Palivo: ${dream.likes}`}
			>
				<Icon name="heart" size={18} stroke={2} />
			</button>

			{#if !photo && coming && !busy}
				<!--
					The photograph is on the server and its sizes are being made.
					Without this the sky reads as an upload that failed, and the
					pill beside it invites the same photograph to be sent again
					(D52 amended). It sits in this row rather than above it, so
					it adds no height to the scrollport and rule 14 holds.
				-->
				<span class="btn btn--photo reel__pick reel__pick--busy">
					<Icon name="camera" size={18} stroke={1.8} />
					Zpracovává se…
				</span>
			{:else if !photo}
				<!--
					A dream written as a sentence in the Seznam has no picture,
					and this is the screen that notices: the pill is on the tile
					rather than two screens away (D52). It is gone the moment
					there is a photograph — replacing one is the dream's own
					screen, where there is room to look at it first.
				-->
				<label class="btn btn--photo reel__pick" class:reel__pick--busy={busy}>
					<Icon name="camera" size={18} stroke={1.8} />
					{busy ? 'Ukládám…' : 'Přidat fotky'}
					<input
						class="reel__file"
						type="file"
						accept="image/*"
						multiple
						onchange={(event) => onpick(dream, event)}
						use:writes={() => !canPick}
					/>
				</label>
			{/if}
		</div>
	</div>
</article>

<style>
	/**
	 * A page: the screen, edge to edge. The print's ratio, radius and shadow
	 * all go, and so does the gap that used to be between two of them — a
	 * reel has no ground to show a dream against, and a seam of it passing by
	 * mid-swipe is the tell that this is a list rather than a reel.
	 */
	.reel__tile {
		aspect-ratio: auto;
		height: 100%;
		border-radius: 0;
		box-shadow: none;
		scroll-snap-align: start;
		scroll-snap-stop: always;
	}

	/* A screen's worth of photograph needs the longer ramp: the words sit
	   clear of the bar, a sixth of the way up, where `--scrim` has barely
	   begun (tokens.css). */
	.reel__tile::after {
		background: var(--scrim-tall);
	}

	/* The status stands under the floating chrome rather than behind it. The
	   chrome measures itself and sets `--band` on the reel; the fallback is
	   what it measures when it is only the rail, so the first frame is
	   already right on the board that has no anniversary — which is every
	   board but one. */
	.reel__tile .dream__tag {
		top: calc(var(--band, 4.25rem) + var(--space-2));
	}

	.reel__open {
		position: absolute;
		inset: 0;
		z-index: 1;
	}

	/* The row: the page's own size, scrolled across and snapped a slide at a
	   time. Under the scrim, which lets a tap through, so the words still read
	   over every photograph. `contain`, so a swipe past the last photograph is
	   not the browser's back — it is the other reel, which `sideways.ts` says. */
	.reel__slides {
		position: absolute;
		inset: 0;
		display: flex;
		overflow-x: auto;
		overflow-y: hidden;
		overscroll-behavior-x: contain;
		scroll-snap-type: x mandatory;
		scrollbar-width: none;
		touch-action: pan-x pan-y pinch-zoom;
	}

	.reel__slides::-webkit-scrollbar {
		display: none;
	}

	.reel__slide {
		position: relative;
		flex: 0 0 100%;
		overflow: hidden;
		scroll-snap-align: start;
		scroll-snap-stop: always;
		-webkit-user-drag: none;
	}

	/* The dots: the photograph's ink, the one showing at full strength and
	   stretched — no accent over a photograph (rule 4). */
	.reel__dots {
		display: flex;
		gap: 6px;
		margin-bottom: var(--space-1);
	}

	.reel__dot {
		width: 6px;
		height: 6px;
		border-radius: var(--radius-full);
		background: var(--photo-ink);
		opacity: 0.45;
		transition:
			width var(--dur-fast) var(--ease-out),
			opacity var(--dur-fast) var(--ease-out);
	}

	.reel__dot--on {
		width: 14px;
		opacity: 1;
	}

	/* The words let the tap through to the link; only the controls take it.
	   Their foot clears the floating bar, by the distance every page's last
	   row clears it. */
	.reel__body {
		z-index: 2;
		padding-bottom: var(--page-end);
		pointer-events: none;
	}

	/* The tile's controls: the heart, and the photo pill on a tile that has no
	   photograph yet. A row, because they are the same kind of thing — the two
	   things a dream can be given from the reel — and they wrap rather than
	   squeeze on a narrow phone. Only this row takes a tap; the words above it
	   fall through to the dream. */
	.reel__acts {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
		margin-top: var(--space-2);
		pointer-events: auto;
	}

	/* The heart alone is a circle, not a pill with nothing in it. */
	.reel__fuel {
		width: 40px;
		padding: 0;
	}

	/* Fuelled at least once: the heart fills. White rather than ember, because
	   the accent stays off a photograph and the interface steps back there
	   (rule 4) — the ember heart is on the dream's own screen. */
	.reel__fuel--lit :global(svg) {
		fill: var(--photo-ink);
	}

	.reel__pick--busy {
		opacity: 0.6;
	}

	/* The real input, kept for the picker it opens and hidden from the eye;
	   the label is the pill, as it is in `PhotoPicker`. */
	.reel__file {
		position: absolute;
		width: 1px;
		height: 1px;
		opacity: 0;
		pointer-events: none;
	}
</style>
