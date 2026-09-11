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
	 * Open is the screen's state, never the sheet's: escape and the dim ask
	 * to close and the screen decides, so a sheet in the middle of saving can
	 * stay up. The dialog element is told, after the fact, what that state is.
	 */
	import type { Snippet } from 'svelte';

	interface Props {
		/** Whether it is up. Owned by the screen. */
		open: boolean;
		/** The sheet's name: its heading, and what a screen reader announces. */
		title: string;
		/** Asked for by the escape key and by the dim. */
		onclose: () => void;
		children: Snippet;
	}

	let { open, title, onclose, children }: Props = $props();

	let el: HTMLDialogElement | null = $state(null);

	$effect(() => {
		const dialog = el;
		if (!dialog) return;
		// `showModal` on an open dialog throws; `close` on a closed one fires
		// a second `close` event. Ask the element what it already is.
		if (open && !dialog.open) dialog.showModal();
		else if (!open && dialog.open) dialog.close();
	});
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
		a laptop. Presentational: the sheet's own „Zrušit“ is the announced way
		out, and a second one in the reading order would only be noise.
	-->
	<div class="sheet__dim" role="presentation" onclick={onclose}></div>

	<div class="sheet__panel">
		<span class="sheet__grab" aria-hidden="true"></span>
		<h2 class="sheet__title">{title}</h2>
		{@render children()}
	</div>
</dialog>
