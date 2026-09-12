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
	 * arithmetic is `images/focal.ts`, which has the tests; what is here is
	 * the gestures and the frame.
	 */
	import { CENTRED, MIN_ZOOM, dragged, spread, zoomed, type Focal } from '$lib/images/focal';
	import Icon from './Icon.svelte';

	interface Props {
		open: boolean;
		/** The picture being positioned: a saved URL or an object URL. */
		src: string;
		/** Where it is looked at now. */
		focal: Focal;
		/** What the tile will say over it, so nothing is hidden behind words. */
		title?: string;
		line?: string;
		busy?: boolean;
		/** Chosen. The screen saves it and closes. */
		onsave: (focal: Focal) => void;
		oncancel: () => void;
	}

	let { open, src, focal, title = '', line = '', busy = false, onsave, oncancel }: Props = $props();

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
		}
	}

	$effect(() => {
		// Re-read when the picture changes, and when it is opened on a new one.
		void src;
		void open;
		measure();
	});

	/** Where the gesture began, and the crop it began from. */
	let from: { x: number; y: number; focal: Focal; gap: number; zoom: number } | null = null;

	/**
	 * Every finger currently down, so two of them can be a pinch. A plain
	 * array rather than a reactive collection: nothing on the screen is drawn
	 * from it, and the crop it produces is what `now` holds.
	 */
	let touches: { id: number; x: number; y: number }[] = [];

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
	const style = $derived(
		`object-position:${pc(now.x)}% ${pc(now.y)}%;` +
			`transform:scale(${round(now.zoom)});transform-origin:${pc(now.x)}% ${pc(now.y)}%;`
	);

	function pc(unit: number): number {
		return Math.round(unit * 10000) / 100;
	}

	function round(value: number): number {
		return Math.round(value * 100) / 100;
	}

	function measured(): { width: number; height: number } {
		const box = frame?.getBoundingClientRect();
		return { width: box?.width ?? 0, height: box?.height ?? 0 };
	}

	function put(event: PointerEvent) {
		touches = [
			...touches.filter((one) => one.id !== event.pointerId),
			{ id: event.pointerId, x: event.clientX, y: event.clientY }
		];
	}

	function down(event: PointerEvent) {
		if (busy) return;
		put(event);
		try {
			// Keeps the gesture on this element once a finger leaves it. A
			// pointer the browser has already let go of throws here, and a
			// drag that works without capture is better than one that stops.
			(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
		} catch {
			/* no capture; the move and up handlers still fire on this element */
		}

		from = {
			x: event.clientX,
			y: event.clientY,
			focal: { ...now },
			gap: touches.length === 2 ? spread(touches[0], touches[1]) : 0,
			zoom: now.zoom
		};
	}

	function move(event: PointerEvent) {
		if (!from || !touches.some((one) => one.id === event.pointerId)) return;
		put(event);

		if (touches.length === 2 && from.gap > 0) {
			// A pinch: the zoom is the distance between the fingers against the
			// distance they started at, so letting go and starting again does
			// not jump.
			now = zoomed({ ...now, zoom: from.zoom }, spread(touches[0], touches[1]) / from.gap);
			return;
		}

		// The whole drag from where the finger went down, not frame by frame,
		// so a gesture that hits an edge and comes back ends where it should.
		now = dragged(from.focal, event.clientX - from.x, event.clientY - from.y, measured(), picture);
	}

	function up(event: PointerEvent) {
		touches = touches.filter((one) => one.id !== event.pointerId);
		if (touches.length === 0) {
			from = null;
			return;
		}

		// A finger lifted off a pinch: whatever is left starts a new drag from
		// where it is, rather than sending the picture across the screen.
		const left = touches[0];
		from = { x: left.x, y: left.y, focal: { ...now }, gap: 0, zoom: now.zoom };
	}

	function wheel(event: WheelEvent) {
		if (busy) return;
		event.preventDefault();
		now = zoomed(now, event.deltaY < 0 ? 1.08 : 1 / 1.08);
	}

	function keys(event: KeyboardEvent) {
		const step = 24;
		const by: Record<string, [number, number]> = {
			ArrowLeft: [-step, 0],
			ArrowRight: [step, 0],
			ArrowUp: [0, -step],
			ArrowDown: [0, step]
		};
		const nudge = by[event.key];
		if (nudge) {
			event.preventDefault();
			now = dragged(now, nudge[0], nudge[1], measured(), picture);
			return;
		}
		if (event.key === '+' || event.key === '=') {
			event.preventDefault();
			now = zoomed(now, 1.1);
		} else if (event.key === '-') {
			event.preventDefault();
			now = zoomed(now, 1 / 1.1);
		}
	}
</script>

<dialog
	class="crop"
	bind:this={el}
	aria-label="Umístit fotku"
	oncancel={(event) => {
		event.preventDefault();
		oncancel();
	}}
	onkeydown={keys}
>
	<div class="crop__frame" bind:this={frame}>
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
			onpointerdown={down}
			onpointermove={move}
			onpointerup={up}
			onpointercancel={up}
			onwheel={wheel}
		/>

		<!-- Where the words will be, so nothing is positioned under them. -->
		{#if title}
			<div class="crop__words">
				<h2 class="dream__title">{title}</h2>
				{#if line}<p class="dream__why">{line}</p>{/if}
			</div>
		{/if}

		<p class="hint glass crop__how" aria-live="polite">
			Posuň fotku prstem, dvěma ji přiblížíš. Šipkami taky.
			{#if now.zoom > MIN_ZOOM}<span class="crop__zoom">{round(now.zoom)}×</span>{/if}
		</p>

		<div class="crop__acts">
			<button type="button" class="btn btn--photo" onclick={oncancel} disabled={busy}>Zrušit</button
			>
			<button
				type="button"
				class="btn btn--photo"
				onclick={() => (now = { ...CENTRED })}
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
	/* The whole screen, and no dialog of its own: the frame *is* the surface
	   being positioned for. The app's own column width, so on a desktop this
	   is the same shape the reel is rather than the shape of the monitor. */
	.crop {
		width: 100%;
		max-width: none;
		height: 100dvh;
		max-height: none;
		margin: 0;
		padding: 0;
		border: 0;
		background: var(--ground);
		overflow: hidden;
	}

	.crop::backdrop {
		background: rgb(0 0 0 / 60%);
	}

	.crop__frame {
		position: relative;
		width: 100%;
		max-width: 34rem;
		height: 100dvh;
		margin-inline: auto;
		overflow: hidden;
		background: var(--surface-3);
		isolation: isolate;
	}

	/* The scrim the reel's own tile wears, so the words read the same here. */
	.crop__frame::after {
		content: '';
		position: absolute;
		inset: 0;
		background: var(--scrim-tall);
		pointer-events: none;
	}

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

	.crop__words {
		position: absolute;
		left: 0;
		right: 0;
		bottom: calc(var(--space-8) + var(--space-7));
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: var(--space-2);
		padding-inline: var(--space-4);
		color: var(--photo-ink);
		pointer-events: none;
	}

	.crop__how {
		position: absolute;
		top: calc(var(--space-3) + env(safe-area-inset-top, 0px));
		left: var(--space-4);
		right: var(--space-4);
		padding: var(--space-2) var(--space-3);
		border-radius: var(--radius-lg);
		pointer-events: none;
	}

	.crop__zoom {
		font-variant-numeric: tabular-nums;
		font-weight: 600;
	}

	.crop__acts {
		position: absolute;
		left: var(--space-4);
		right: var(--space-4);
		bottom: calc(var(--space-4) + env(safe-area-inset-bottom, 0px));
		display: flex;
		gap: var(--space-2);
	}

	.crop__acts > :last-child {
		margin-left: auto;
	}
</style>
