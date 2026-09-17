<script lang="ts">
	/**
	 * The sheet: a form that rises from the bottom edge over the screen that
	 * asked for it, instead of a screen you navigate to and come back from
	 * (D44). The look is `.sheet` in `app.css`; this is the behaviour.
	 *
	 * A native `<dialog>`, opened with `showModal`, which is the whole reason
	 * to use one: the top layer puts it over the floating tab bar with no
	 * z-index to argue with, the escape key closes it, focus is trapped inside
	 * it and the screen behind it goes inert — none of which is worth
	 * hand-rolling for a hundred lines and a bundle budget.
	 *
	 * Open is the screen's state, never the sheet's: escape, the dim and the
	 * pull all ask to close and the screen decides, so a sheet in the middle of
	 * saving can stay up. The dialog element is told, after the fact, what that
	 * state is.
	 *
	 * **It is pulled down to put it away**, as Prosper's is (D76). The handle —
	 * the grab bar and the title — takes the gesture, and the body under it is
	 * a sibling with its own scroll, so there is nothing to arbitrate: a pull
	 * cannot start inside the form and a scroll cannot be stolen by the sheet.
	 * How far is far enough is `ui/pull.ts`, which has the test. A pull is not
	 * something every assistive technology can produce and a touch screen
	 * reader has no escape key, so the last thing in the panel is a „Zavřít“
	 * nobody sees.
	 */
	import type { Snippet } from 'svelte';
	import { dismisses, pulledBy } from './pull';

	interface Props {
		/** Whether it is up. Owned by the screen. */
		open: boolean;
		/** The sheet's name: its heading, and what a screen reader announces. */
		title: string;
		/** Asked for by the escape key, by the dim and by the pull. */
		onclose: () => void;
		children: Snippet;
	}

	let { open, title, onclose, children }: Props = $props();

	let el: HTMLDialogElement | null = $state(null);
	let panel: HTMLElement | null = $state(null);

	$effect(() => {
		const dialog = el;
		if (!dialog) return;
		// `showModal` on an open dialog throws; `close` on a closed one fires
		// a second `close` event. Ask the element what it already is.
		if (open && !dialog.open) dialog.showModal();
		else if (!open && dialog.open) dialog.close();
	});

	// ── the pull ────────────────────────────────────────────────────────────

	/** How far down the panel sits under the finger, in pixels. */
	let pulled = $state(0);
	let dragging = $state(false);

	/** Where the finger went down, when, and how tall the panel was then. */
	let from = 0;
	let since = 0;
	let span = 0;

	function grab(event: PointerEvent) {
		if (event.button > 0) return;
		try {
			// Keeps the pull alive once the finger is below the handle, which
			// is where a pull goes. One the browser has let go of throws.
			(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
		} catch {
			/* no capture; still draggable while the finger is on the handle */
		}
		dragging = true;
		from = event.clientY;
		since = event.timeStamp;
		span = panel?.offsetHeight ?? 0;
	}

	function move(event: PointerEvent) {
		if (dragging) pulled = pulledBy(event.clientY - from);
	}

	function release(event: PointerEvent) {
		if (!dragging) return;
		dragging = false;

		const going = dismisses(pulled, event.timeStamp - since, span);
		// Back to the stylesheet's own value either way. Refused, that is the
		// panel settling home; taken, the screen closes in the same frame and
		// the panel carries on down from where the finger left it.
		pulled = 0;
		if (going) onclose();
	}
</script>

<!--
	`cancel` is the escape key. Prevented, because closing is the screen's to
	do — otherwise the element shuts and the state that says it is open does
	not, and the next open is a no-op.
-->
<dialog
	class="sheet"
	bind:this={el}
	aria-label={title}
	oncancel={(event) => {
		event.preventDefault();
		onclose();
	}}
>
	<!--
		The dim, which is a way out on a phone the way the escape key is one on
		a laptop. Presentational: the hidden „Zavřít“ is the announced way out,
		and a second one in the reading order would only be noise.
	-->
	<div class="sheet__dim" role="presentation" onclick={onclose}></div>

	<div
		class="sheet__panel"
		class:sheet__panel--held={dragging}
		bind:this={panel}
		style:translate={pulled === 0 ? null : `0 ${pulled}px`}
	>
		<div
			class="sheet__handle"
			role="presentation"
			onpointerdown={grab}
			onpointermove={move}
			onpointerup={release}
			onpointercancel={release}
		>
			<span class="sheet__grab" aria-hidden="true"></span>
			<h2 class="sheet__title">{title}</h2>
		</div>

		<div class="sheet__body">
			{@render children()}
		</div>

		<button type="button" class="visually-hidden" onclick={onclose}>Zavřít</button>
	</div>
</dialog>
