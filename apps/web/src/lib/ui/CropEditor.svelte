<script lang="ts">
	/**
	 * Where a photograph is looked at, chosen by moving it (D54).
	 *
	 * The frame is the reel's own page — the app column, the height of the
	 * screen — because that is the surface this is mostly for, and a
	 * photograph positioned in a 4:5 preview and then shown on a 9:19 screen
	 * is positioned for the wrong shape. The title and the line sit on it
	 * where they will sit, so nothing is placed under words it turns out to
	 * be behind.
	 *
	 * A native `<dialog>` like `Sheet` and for the same reasons: the top layer
	 * clears the floating bar with no z-index to argue with, escape closes,
	 * focus is trapped and the screen behind goes inert.
	 *
	 * One finger moves the picture, two pinch it, a wheel zooms on a laptop,
	 * and the arrow keys nudge it for anybody who has no pointer at all. The
	 * arithmetic is `images/focal.ts` and the fingers are `ui/placing.ts`,
	 * which `CollageEditor` shares (D85); what is here is the frame.
	 *
	 * **Vyplnit or Celá** (D81). A photograph either fills the frame, cropped
	 * to its shape, or stands whole inside it on a mat — which is the answer to
	 * a landscape picture on a phone-shaped reel, where filling keeps a third
	 * of it at twice its pixels. Whole, the same finger moves it within the
	 * room it has and the same pinch brings it closer; the row of swatches is
	 * the five mats in `tokens.css` and the photograph's own blur. Both are
	 * `FitControls`, which a collage's cell has too (D88).
	 */
	import { matIsBlur, matStyle, photoStyle, tileStyle } from '$lib/dreams/photos';
	import { CENTRED, MIN_ZOOM, fitted, startsWhole, toInput, type Focal } from '$lib/images/focal';
	import FitControls from './FitControls.svelte';
	import Icon from './Icon.svelte';
	import { hands } from './placing';

	interface Props {
		open: boolean;
		/** The picture being positioned: a saved URL or an object URL. */
		src: string;
		/** Where it is looked at now. */
		focal: Focal;
		/**
		 * Whether this is a picture picked a moment ago and never placed. A
		 * landscape one then opens whole rather than filling (`startsWhole`);
		 * a photograph somebody has already placed opens as they left it.
		 */
		fresh?: boolean;
		/** What the tile will say over it, so nothing is hidden behind words. */
		title?: string;
		line?: string;
		busy?: boolean;
		/** Chosen. The screen saves it and closes. */
		onsave: (focal: Focal) => void;
		oncancel: () => void;
	}

	let {
		open,
		src,
		focal,
		fresh = false,
		title = '',
		line = '',
		busy = false,
		onsave,
		oncancel
	}: Props = $props();

	let el: HTMLDialogElement | null = $state(null);
	let frame = $state<HTMLElement | null>(null);

	/** The crop being edited. Seeded from the prop each time it opens. */
	let now = $state<Focal>({ ...CENTRED });

	/** The picture's own pixels, once the browser has read them. */
	let picture = $state({ width: 0, height: 0 });
	let img = $state<HTMLImageElement | null>(null);

	/**
	 * How big the picture actually is, which every drag divides by.
	 *
	 * `load` alone is not enough: a photograph already in the cache — which is
	 * every photograph the reel has just shown — has fired it before this
	 * component exists, so the handler never runs, the size stays nought, and
	 * `dragged` finds no overflow to move within. Every gesture is then a
	 * silent no-op. So the element is asked directly as well.
	 */
	function measure() {
		if (img?.complete && img.naturalWidth > 0) {
			picture = { width: img.naturalWidth, height: img.naturalHeight };
			suggest();
		}
	}

	/** Which picture the suggestion below has already been made for. */
	let suggested = '';

	/**
	 * A landscape picture nobody has placed yet opens whole. Once per picture,
	 * and only while it is untouched — turning it back to Vyplnit must not be
	 * undone by the next `load`.
	 */
	function suggest() {
		if (!fresh || !open || suggested === src) return;
		suggested = src;
		if (now.fit === 'fill' && now.zoom === MIN_ZOOM && startsWhole(picture)) {
			now = fitted(now, 'whole');
		}
	}

	$effect(() => {
		// Re-read when the picture changes, and when it is opened on a new one.
		void src;
		void open;
		measure();
	});

	$effect(() => {
		const dialog = el;
		if (!dialog) return;
		if (open && !dialog.open) dialog.showModal();
		else if (!open && dialog.open) dialog.close();
	});

	// A new photograph, or a new opening of the same one: start from what was
	// saved rather than from wherever the last gesture left this component.
	$effect(() => {
		if (open) now = { ...focal };
	});

	/** What the picture wears, which is what a tile will wear (`photos.ts`). */
	const style = $derived(tileStyle(toInput(now)));

	/** The mat behind it, and the blur under it when that is the mat. */
	const mat = $derived(matStyle(now));
	const blurred = $derived(matIsBlur(now));

	function round(value: number): number {
		return Math.round(value * 100) / 100;
	}

	function measured(): { width: number; height: number } {
		const box = frame?.getBoundingClientRect();
		return { width: box?.width ?? 0, height: box?.height ?? 0 };
	}

	/** The fingers on the picture, moving the crop being edited. */
	const placed = hands({
		get: () => now,
		set: (focal) => (now = focal),
		frame: measured,
		picture: () => picture,
		resting: () => busy
	});
</script>

<dialog
	class="crop"
	bind:this={el}
	aria-label="Umístit fotku"
	oncancel={(event) => {
		event.preventDefault();
		oncancel();
	}}
	onkeydown={placed.keys}
>
	<div class="crop__frame" bind:this={frame} style={mat}>
		{#if blurred}
			<img class="dream__under" {src} alt="" aria-hidden="true" style={photoStyle(toInput(now))} />
		{/if}

		<!--
			The picture, wearing exactly what a tile will wear. `touch-action:
			none` only here, so the browser hands over the gesture instead of
			scrolling the page with it — and nowhere else, so every other
			scroll region in the app keeps its own.
		-->
		<img
			class="crop__img"
			{src}
			alt=""
			{style}
			draggable="false"
			bind:this={img}
			onload={measure}
			onpointerdown={placed.down}
			onpointermove={placed.move}
			onpointerup={placed.up}
			onpointercancel={placed.up}
			onwheel={placed.wheel}
		/>

		<!-- Where the words will be, so nothing is positioned under them. -->
		{#if title}
			<div class="crop__words">
				<h2 class="dream__title">{title}</h2>
				{#if line}<p class="dream__why">{line}</p>{/if}
			</div>
		{/if}

		<div class="crop__top">
			<p class="hint glass crop__how" aria-live="polite">
				Posuň fotku prstem, dvěma ji přiblížíš. Šipkami taky.
				{#if now.zoom > MIN_ZOOM}<span class="crop__zoom">{round(now.zoom)}×</span>{/if}
			</p>

			<FitControls focal={now} {src} {busy} onchange={(chosen) => (now = chosen)} />
		</div>

		<div class="crop__acts">
			<button type="button" class="btn btn--photo" onclick={oncancel} disabled={busy}>Zrušit</button
			>
			<button
				type="button"
				class="btn btn--photo"
				onclick={() => (now = { ...now, x: CENTRED.x, y: CENTRED.y, zoom: MIN_ZOOM })}
				disabled={busy}>Na střed</button
			>
			<button
				type="button"
				class="btn btn--accent"
				onclick={() => onsave({ ...now })}
				disabled={busy}
			>
				<Icon name="check" size={18} stroke={2} />
				Hotovo
			</button>
		</div>
	</div>
</dialog>

<style>
	.crop__img {
		position: absolute;
		inset: 0;
		width: 100%;
		height: 100%;
		object-fit: cover;
		/* The gesture is ours; the page must not scroll underneath it. */
		touch-action: none;
		user-select: none;
		-webkit-user-select: none;
		cursor: grab;
	}

	.crop__img:active {
		cursor: grabbing;
	}
</style>
