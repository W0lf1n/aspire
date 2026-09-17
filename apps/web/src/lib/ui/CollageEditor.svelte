<script lang="ts">
	/**
	 * A collage placed on the page it is mostly seen on, a cell at a time (D85).
	 *
	 * The first photograph of a dream is placed in `CropEditor`, filling the
	 * reel's own page with the title where it will sit. From the second on, a
	 * photograph is one cell of a picture, and placing it on its own is placing
	 * it for a shape it will never have: a face kept in view on a whole page is
	 * cut in half by a cell a third as wide. So this is the same page, the same
	 * words on it and the same fingers (`ui/placing.ts`) — with the collage
	 * drawn on it whole.
	 *
	 * Every cell is its own photograph under a finger. Touching one takes it in
	 * hand and the drag has already begun; the ring says which one the arrows,
	 * a wheel and „Na střed“ move. A second finger landing on the next cell is
	 * still the pinch on the first. The three templates are across the top,
	 * because which cells there are is the other half of how the collage looks,
	 * and choosing one here shows it with the photographs in it rather than as
	 * a diagram.
	 *
	 * A cell is not only a crop (D88). The cell in hand has Vyplnit · Celá and
	 * the mats under the templates, exactly as a photograph on its own has in
	 * `CropEditor`, so a landscape picture in a tall cell can be all of itself
	 * on a mat instead of a sliver — and moved, and brought closer, from there.
	 *
	 * Nothing is sent until Hotovo: then the template if it changed, and every
	 * photograph whose crop did (`sameCrop`).
	 */
	import { untrack } from 'svelte';
	import type { DreamImage } from '@aspire/contracts';
	import { gridStyle, templateFor, templatesFor } from '$lib/dreams/collage';
	import { cellUrl, matIsBlur, matStyle, photoStyle, tileStyle } from '$lib/dreams/photos';
	import {
		CENTRED,
		MIN_ZOOM,
		focalOf,
		sameCrop,
		toInput,
		type Focal,
		type Size
	} from '$lib/images/focal';
	import FitControls from './FitControls.svelte';
	import Icon from './Icon.svelte';
	import { hands } from './placing';

	interface Props {
		open: boolean;
		/** The ready dreamt photographs, in order; the first is the lead. */
		photos: DreamImage[];
		/** The template the dream has now, 0 to 2. */
		layout: number;
		/** Which photograph is in hand when it opens: the one tapped on the shelf, or the lead. */
		start?: string | null;
		/** What the tile will say over it, so nothing is placed under words. */
		title?: string;
		line?: string;
		busy?: boolean;
		/** Chosen: the template, and every photograph that was moved, with where to. */
		onsave: (layout: number, moved: { image: DreamImage; focal: Focal }[]) => void;
		oncancel: () => void;
	}

	let {
		open,
		photos,
		layout,
		start = null,
		title = '',
		line = '',
		busy = false,
		onsave,
		oncancel
	}: Props = $props();

	let el: HTMLDialogElement | null = $state(null);

	/** The template being tried, every cell's crop, and the photograph in hand. */
	let trying = $state(0);
	let crops = $state<Record<string, Focal>>({});
	let held = $state<string | null>(null);

	/** Each cell as laid out, and each picture's own pixels: what a drag divides by. */
	const boxes: Record<string, HTMLElement | null> = {};
	let pixels = $state<Record<string, Size>>({});

	const templates = $derived(templatesFor(photos.length));
	const template = $derived(templateFor(photos.length, trying));
	const shown = $derived(template ? photos.slice(0, template.cells.length) : []);

	const heldZoom = $derived(held ? (crops[held]?.zoom ?? MIN_ZOOM) : MIN_ZOOM);

	/** The photograph in hand, and its crop, for the fit and the mats. */
	const heldPhoto = $derived(shown.find((photo) => photo.id === held) ?? null);
	const heldCrop = $derived(held ? (crops[held] ?? null) : null);

	$effect(() => {
		const dialog = el;
		if (!dialog) return;
		if (open && !dialog.open) dialog.showModal();
		else if (!open && dialog.open) dialog.close();
	});

	// Each opening starts from what is saved. Only the opening: the dream
	// arriving again while it is up must not throw away what was moved.
	$effect(() => {
		if (!open) return;
		untrack(() => {
			trying = layout;
			crops = Object.fromEntries(photos.map((photo) => [photo.id, focalOf(photo)]));
			held = photos.some((photo) => photo.id === start) ? start : (photos[0]?.id ?? null);
		});
	});

	const placed = hands({
		get: () => (held ? crops[held] : undefined) ?? { ...CENTRED },
		set: (focal) => {
			if (held) crops[held] = focal;
		},
		frame: () => {
			const box = held ? boxes[held]?.getBoundingClientRect() : null;
			return { width: box?.width ?? 0, height: box?.height ?? 0 };
		},
		picture: () => (held ? pixels[held] : undefined) ?? { width: 0, height: 0 },
		resting: () => busy
	});

	/**
	 * A picture's own size, read when it loads — and at once, because one the
	 * tile has just shown is already loaded and fires nothing (`CropEditor`
	 * says the long version).
	 */
	function sized(node: HTMLImageElement, id: string) {
		const read = () => {
			if (node.complete && node.naturalWidth > 0) {
				pixels[id] = { width: node.naturalWidth, height: node.naturalHeight };
			}
		};
		read();
		node.addEventListener('load', read);
		return { destroy: () => node.removeEventListener('load', read) };
	}

	/** A finger on a cell takes that cell in hand, unless it is the second finger of a pinch. */
	function grab(id: string, event: PointerEvent) {
		if (!placed.holding()) held = id;
		placed.down(event);
	}

	function centre() {
		if (!held || !crops[held]) return;
		crops[held] = { ...crops[held], x: CENTRED.x, y: CENTRED.y, zoom: MIN_ZOOM };
	}

	function save() {
		const moved = shown
			.filter((photo) => crops[photo.id] && !sameCrop(crops[photo.id], focalOf(photo)))
			.map((photo) => ({ image: photo, focal: { ...crops[photo.id] } }));
		onsave(trying, moved);
	}

	function round(value: number): number {
		return Math.round(value * 100) / 100;
	}
</script>

<dialog
	class="crop"
	bind:this={el}
	aria-label="Upravit koláž"
	oncancel={(event) => {
		event.preventDefault();
		oncancel();
	}}
	onkeydown={placed.keys}
>
	<div class="crop__frame">
		{#if template}
			<div class="dream__collage" style={gridStyle(template)}>
				{#each shown as photo, index (photo.id)}
					<!--
						A button, so a keyboard reaches every cell: focusing one
						takes it in hand, and the arrows move it from there.
					-->
					<button
						type="button"
						class="dream__cell collage__cell"
						class:collage__cell--held={held === photo.id}
						style="grid-area:{template.cells[index]};{matStyle(crops[photo.id] ?? null)}"
						aria-pressed={held === photo.id}
						aria-label={`Fotka ${index + 1}`}
						bind:this={boxes[photo.id]}
						onfocus={() => {
							if (!placed.holding()) held = photo.id;
						}}
					>
						{#if crops[photo.id] && matIsBlur(crops[photo.id])}
							<img
								class="dream__under"
								src={photo.thumbUrl}
								alt=""
								aria-hidden="true"
								style={photoStyle(toInput(crops[photo.id]))}
							/>
						{/if}
						<img
							class="collage__img"
							src={cellUrl(photo)}
							alt=""
							style={tileStyle(toInput(crops[photo.id] ?? focalOf(photo)))}
							draggable="false"
							use:sized={photo.id}
							onpointerdown={(event) => grab(photo.id, event)}
							onpointermove={placed.move}
							onpointerup={placed.up}
							onpointercancel={placed.up}
							onwheel={(event) => {
								if (!placed.holding()) held = photo.id;
								placed.wheel(event);
							}}
						/>
					</button>
				{/each}
			</div>
		{/if}

		<!-- Where the words will be, so nothing is placed under them. -->
		{#if title}
			<div class="crop__words">
				<h2 class="dream__title">{title}</h2>
				{#if line}<p class="dream__why">{line}</p>{/if}
			</div>
		{/if}

		<div class="crop__top">
			<p class="hint glass crop__how" aria-live="polite">
				Klepni na fotku a posuň ji prstem, dvěma ji přiblížíš.
				{#if heldZoom > MIN_ZOOM}<span class="crop__zoom">{round(heldZoom)}×</span>{/if}
			</p>

			{#if templates.length > 1}
				<!--
					The three templates, as the shelf draws them — the grid
					itself, so a diagram cannot drift from the collage it stands
					for — on glass, because this is over the photographs.
				-->
				<div class="glass collage__layouts" role="group" aria-label="Rozložení koláže">
					{#each templates as one, index (index)}
						<button
							type="button"
							class="collage__layout"
							aria-pressed={trying === index}
							aria-label={`Rozložení ${index + 1}`}
							onclick={() => (trying = index)}
							disabled={busy}
						>
							<span class="collage__diagram" style={gridStyle(one)} aria-hidden="true">
								{#each one.cells as area (area)}
									<span style="grid-area:{area}"></span>
								{/each}
							</span>
						</button>
					{/each}
				</div>
			{/if}

			{#if heldPhoto && heldCrop}
				<FitControls
					focal={heldCrop}
					src={cellUrl(heldPhoto)}
					{busy}
					onchange={(chosen) => {
						if (held) crops[held] = chosen;
					}}
				/>
			{/if}
		</div>

		<div class="crop__acts">
			<button type="button" class="btn btn--photo" onclick={oncancel} disabled={busy}>Zrušit</button
			>
			<button type="button" class="btn btn--photo" onclick={centre} disabled={busy || !held}
				>Na střed</button
			>
			<button type="button" class="btn btn--accent" onclick={save} disabled={busy}>
				<Icon name="check" size={18} stroke={2} />
				Hotovo
			</button>
		</div>
	</div>
</dialog>

<style>
	/* A cell is a button only so a keyboard can reach it; it looks like the
	   cell it is on the tile. The ring is over the scrim, so the photograph
	   in hand is plain at the foot of the page as well as at the top. */
	.collage__cell {
		display: block;
		border-radius: 0;
	}

	.collage__cell::after {
		content: '';
		position: absolute;
		inset: 0;
		z-index: 1;
		box-shadow: inset 0 0 0 2px transparent;
		transition: box-shadow var(--dur-fast) var(--ease-out);
		pointer-events: none;
	}

	/* The photograph's own ink, as the chosen mat's ring is: type on a
	   photograph (rule 4), not the interface's accent. */
	.collage__cell--held::after {
		box-shadow: inset 0 0 0 2px var(--photo-ink);
	}

	.collage__cell:focus-visible {
		outline: none;
	}

	.collage__img {
		/* The gesture is ours; the page must not scroll underneath it. */
		touch-action: none;
		user-select: none;
		-webkit-user-select: none;
		cursor: grab;
	}

	.collage__img:active {
		cursor: grabbing;
	}

	.collage__layouts {
		display: flex;
		gap: var(--space-1);
		padding: var(--space-1);
		border-radius: var(--radius-md);
		pointer-events: auto;
	}

	.collage__layout {
		padding: 6px 8px;
		border-radius: var(--radius-sm);
		transition: background var(--dur-fast) var(--ease-out);
	}

	/* Chosen by the lens the glass segment wears (D74), not by the accent:
	   over a photograph the interface steps back. */
	.collage__layout[aria-pressed='true'] {
		background: var(--glass-lens);
	}

	/* The page's own shape, roughly: a phone screen, not the tile's 4:5. */
	.collage__diagram {
		display: grid;
		gap: 2px;
		width: 22px;
		height: 40px;
	}

	.collage__diagram span {
		background: var(--ink-3);
	}

	.collage__layout[aria-pressed='true'] .collage__diagram span {
		background: var(--ink);
	}
</style>
