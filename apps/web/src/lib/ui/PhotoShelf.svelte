<script lang="ts">
	/**
	 * Fotky — a dream's photographs laid out in a row, and the things that can
	 * be done to them (D82): add one, up to five; say where one is looked at;
	 * make one the first; take one away; and, from two up, choose which of
	 * three templates the tile cuts them into.
	 *
	 * From two up it also says how the tile shows them (D87): **Koláž**, the
	 * collage and then each photograph swiped across, or **Karusel**, the
	 * photographs alone. The templates are a collage's, so they are only
	 * offered for one; and „Umístit“ follows the choice — a cell is placed in
	 * its collage (D85), a photograph in a carousel is a whole page on its own.
	 *
	 * The ＋ takes as many as there is room for in one pick (D84): the phone's
	 * picker chooses several, the first ones that fit are kept in the order
	 * they were chosen, and the rest are said rather than silently dropped.
	 *
	 * The first photograph is the one that matters twice. It is the cover —
	 * the Seznam's circle, the wallpaper's cell, the shared card and the
	 * notification all show it alone — and in every template with a big cell
	 * it is the big cell. So „Jako první“ is the only reordering there is:
	 * the order of the other four is the order of four small cells, and
	 * nobody has an opinion about that worth five arrows.
	 *
	 * The shelf decides nothing and sends nothing. It says what was asked for
	 * and the dream's screen does it, because the dream is the screen's.
	 */
	import type { Dream, DreamImage, PhotoView } from '@aspire/contracts';
	import { PHOTOS_MAX, gridStyle, templatesFor } from '$lib/dreams/collage';
	import { dreamtCount, dreamtPhotos, photoStyle } from '$lib/dreams/photos';
	import { viewOf } from '$lib/dreams/slides';
	import { roomFor } from '$lib/dreams/upload';
	import { downscaleAll, unreadableSentence } from '$lib/images/downscale';
	import { CENTRED, focalOf, type Focal } from '$lib/images/focal';
	import { writes } from '$lib/offline/writes.svelte';
	import CropEditor from './CropEditor.svelte';
	import Icon from './Icon.svelte';
	import PhotoLinkSheet from './PhotoLinkSheet.svelte';

	interface Props {
		dream: Dream;
		/** A request of this screen's is in flight, and the shelf rests. */
		busy?: boolean;
		/** More photographs, downscaled, in the order picked, and where the first is looked at. */
		onadd: (photos: Blob[], focal: Focal) => void;
		/** Where one is looked at, after the editor. */
		onplace: (image: DreamImage, focal: Focal) => void;
		/**
		 * Placing one of a collage: the screen opens the collage with this
		 * photograph in hand (D85), because a cell is placed for its cell.
		 */
		onarrange: (image: DreamImage) => void;
		/** This one first: the cover, and the big cell. */
		onlead: (image: DreamImage) => void;
		onremove: (image: DreamImage) => void;
		/** Which of the three templates, 0 to 2. */
		onlayout: (layout: number) => void;
		/** The collage and each photograph, or the photographs alone (D87). */
		onview: (view: PhotoView) => void;
		/** A sentence when a picked file could not be read. */
		onproblem: (sentence: string) => void;
	}

	let {
		dream,
		busy = false,
		onadd,
		onplace,
		onarrange,
		onlead,
		onremove,
		onlayout,
		onview,
		onproblem
	}: Props = $props();

	const view = $derived(viewOf(dream));

	const photos = $derived(dreamtPhotos(dream));

	/** Rows still being resized count: the server counts them against the five. */
	const count = $derived(dreamtCount(dream));
	const coming = $derived(count - photos.length);
	const full = $derived(count >= PHOTOS_MAX);

	/** Whether one pick may choose several: only while there is room for more than one. */
	const several = $derived(PHOTOS_MAX - count > 1);

	const templates = $derived(templatesFor(photos.length));

	/** The photograph the row of actions is about. None until one is tapped. */
	let chosenId = $state<string | null>(null);
	const chosen = $derived(photos.find((photo) => photo.id === chosenId) ?? null);

	let reading = $state(false);
	let placing = $state(false);
	let asking = $state(false);

	const resting = $derived(busy || reading);

	async function pick(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const files = [...(input.files ?? [])];
		input.value = '';
		if (files.length === 0) return;

		const { taken: fit, note } = roomFor(files, count);
		reading = true;
		try {
			const { photos: read, unreadable } = await downscaleAll(fit);
			const said = note ?? (unreadable > 0 ? unreadableSentence(unreadable, fit.length) : null);
			if (said) onproblem(said);
			if (read.length > 0) onadd(read, { ...CENTRED });
		} finally {
			reading = false;
		}
	}

	function taken(photo: Blob) {
		asking = false;
		onadd([photo], { ...CENTRED });
	}

	function placed(focal: Focal) {
		placing = false;
		if (chosen) onplace(chosen, focal);
	}
</script>

<section class="card shelf">
	<div class="card__head">
		<p class="label">Fotky</p>
		<span class="hint shelf__count">{count} z {PHOTOS_MAX}</span>
	</div>

	<div class="shelf__strip" role="group" aria-label="Fotky snu">
		{#each photos as photo, index (photo.id)}
			<button
				type="button"
				class="shelf__thumb"
				aria-pressed={chosenId === photo.id}
				aria-label={`Fotka ${index + 1}${index === 0 ? ', hlavní' : ''}`}
				onclick={() => (chosenId = chosenId === photo.id ? null : photo.id)}
			>
				<img
					src={photo.thumbUrl}
					alt=""
					style={photoStyle(photo)}
					loading="lazy"
					decoding="async"
				/>
				{#if index === 0 && photos.length > 1}
					<span class="badge badge--photo badge--tiny shelf__lead">hlavní</span>
				{/if}
			</button>
		{/each}

		{#each Array.from({ length: coming }, (_unused, index) => index) as index (index)}
			<!-- On the server, its sizes not made yet: a place held, so the row
			     does not jump when the picture arrives. -->
			<span class="shelf__thumb shelf__thumb--coming" aria-label="Fotka se zpracovává"></span>
		{/each}

		{#if !full}
			<label class="shelf__thumb shelf__add" class:shelf__add--resting={resting}>
				<Icon name="plus" size={22} stroke={2} />
				<span class="visually-hidden">{several ? 'Přidat fotky' : 'Přidat fotku'}</span>
				<input
					class="shelf__file"
					type="file"
					accept="image/*"
					multiple={several}
					onchange={pick}
					use:writes={() => resting}
				/>
			</label>
		{/if}
	</div>

	{#if chosen}
		<div class="actions">
			<button
				type="button"
				class="btn btn--sm"
				onclick={() =>
					photos.length > 1 && view === 'collage' ? onarrange(chosen) : (placing = true)}
				use:writes={() => resting}
			>
				<Icon name="image" size={16} stroke={1.8} />
				Umístit
			</button>
			{#if photos[0]?.id !== chosen.id}
				<button
					type="button"
					class="btn btn--sm"
					onclick={() => onlead(chosen)}
					use:writes={() => resting}
				>
					Jako první
				</button>
			{/if}
			<button
				type="button"
				class="btn btn--sm btn--danger"
				onclick={() => {
					const going = chosen;
					chosenId = null;
					onremove(going);
				}}
				use:writes={() => resting}
			>
				Smazat
			</button>
		</div>
	{:else if !full}
		<div class="actions">
			<button
				type="button"
				class="btn btn--sm btn--quiet"
				onclick={() => (asking = true)}
				use:writes={() => resting}
			>
				<Icon name="link" size={16} stroke={1.8} />
				Přidat z odkazu
			</button>
		</div>
	{/if}

	{#if photos.length > 1}
		<div class="seg seg--soft" role="group" aria-label="Jak se fotky ukážou">
			<button
				type="button"
				class="seg__item"
				aria-pressed={view === 'collage'}
				onclick={() => onview('collage')}
				use:writes={() => resting}
			>
				Koláž
			</button>
			<button
				type="button"
				class="seg__item"
				aria-pressed={view === 'carousel'}
				onclick={() => onview('carousel')}
				use:writes={() => resting}
			>
				Karusel
			</button>
		</div>
	{/if}

	{#if templates.length > 0 && view === 'collage'}
		<div class="shelf__layouts" role="group" aria-label="Rozložení koláže">
			{#each templates as template, index (index)}
				<button
					type="button"
					class="shelf__layout"
					aria-pressed={(dream.layout ?? 0) === index}
					aria-label={`Rozložení ${index + 1}`}
					onclick={() => onlayout(index)}
					use:writes={() => resting}
				>
					<span class="shelf__grid" style={gridStyle(template)} aria-hidden="true">
						{#each template.cells as area (area)}
							<span style="grid-area:{area}"></span>
						{/each}
					</span>
				</button>
			{/each}
		</div>
	{/if}

	<p class="hint">
		{#if photos.length > 1 && view === 'carousel'}
			Na nástěnce se fotky listují do strany, jedna po druhé. První je hlavní: jen ona je vidět v
			seznamu, na tapetě a ve sdíleném odkazu.
		{:else if photos.length > 1}
			Na nástěnce je koláž a za ní každá fotka zvlášť. První fotka je hlavní: je největší v koláži a
			jen ona je vidět v seznamu, na tapetě a ve sdíleném odkazu.
		{:else if full}
			Víc než {PHOTOS_MAX} fotek se na dlaždici nevejde.
		{:else}
			Přidej další — až {PHOTOS_MAX} fotek se na nástěnce složí do koláže.
		{/if}
	</p>
</section>

{#if chosen}
	<CropEditor
		open={placing}
		src={chosen.screenUrl}
		focal={focalOf(chosen)}
		title={dream.title}
		line={dream.why}
		busy={resting}
		onsave={placed}
		oncancel={() => (placing = false)}
	/>
{/if}

<PhotoLinkSheet open={asking} onphoto={taken} onclose={() => (asking = false)} />

<style>
	.shelf__count {
		font-variant-numeric: tabular-nums;
	}

	/* Five across and never six: four photographs and the ＋, or five and no
	   ＋ because the shelf is full. 52 px is what makes five of them and their
	   gaps 292 px, which is one line inside a card on a 360 px phone. It wraps
	   rather than scrolls on anything narrower: a row that scrolls sideways
	   inside a page that scrolls down is a row that fights. */
	.shelf__strip {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
	}

	.shelf__thumb {
		position: relative;
		flex: none;
		width: 52px;
		height: 52px;
		overflow: hidden;
		border-radius: var(--radius-sm);
		background: var(--surface-3);
		transition: box-shadow var(--dur-fast) var(--ease-out);
	}

	.shelf__thumb img {
		width: 100%;
		height: 100%;
		object-fit: cover;
	}

	/* Chosen: a ring in the accent that acts, because what follows is a row of
	   actions about this photograph. Outside the thumb, so the picture keeps
	   its corners. */
	.shelf__thumb[aria-pressed='true'] {
		box-shadow:
			0 0 0 2px var(--surface),
			0 0 0 4px var(--signal);
	}

	.shelf__thumb--coming {
		background: var(--dawn);
		opacity: 0.5;
	}

	.shelf__lead {
		position: absolute;
		left: 3px;
		bottom: 3px;
	}

	.shelf__add {
		display: grid;
		place-items: center;
		color: var(--ink-2);
		cursor: pointer;
	}

	.shelf__add--resting {
		opacity: 0.5;
	}

	/* The real input, kept for the picker it opens and hidden from the eye;
	   the label is the tile, as it is in `PhotoPicker`. */
	.shelf__file {
		position: absolute;
		width: 1px;
		height: 1px;
		opacity: 0;
		pointer-events: none;
	}

	/* The three templates, each drawn with its own grid: the diagram cannot
	   drift from the collage, because it is the collage with nothing in it. */
	.shelf__layouts {
		display: flex;
		gap: var(--space-2);
	}

	.shelf__layout {
		padding: 6px;
		border-radius: var(--radius-sm);
		background: var(--surface-3);
		transition: box-shadow var(--dur-fast) var(--ease-out);
	}

	.shelf__layout[aria-pressed='true'] {
		box-shadow: 0 0 0 2px var(--signal);
	}

	.shelf__grid {
		display: grid;
		gap: 2px;
		width: 32px;
		height: 44px;
	}

	.shelf__grid span {
		background: var(--ink-3);
	}

	.shelf__layout[aria-pressed='true'] .shelf__grid span {
		background: var(--ink);
	}
</style>
