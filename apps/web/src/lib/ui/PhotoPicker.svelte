<script lang="ts">
	/**
	 * The photograph's place on a screen: the dream's tile with the picture in
	 * it, or the sky where the picture will be, and one photo pill that opens
	 * the phone's own picker — camera or library, the phone's choice. The file
	 * is downscaled here (`downscale.ts`) before anyone sees it, so the preview
	 * is exactly what the server will get.
	 */
	import { downscale } from '$lib/images/downscale';
	import Icon from './Icon.svelte';

	interface Props {
		/** What is there now: the saved photograph's URL, or nothing. */
		current?: string | null;
		title?: string;
		why?: string;
		/** 16:10 on a form, where the words are below; 4:5 on a dream's own screen. */
		wide?: boolean;
		busy?: boolean;
		/** The downscaled photograph, ready to send. */
		onpick: (photo: Blob) => void;
		/** A sentence when the picture could not be read. */
		onproblem: (sentence: string) => void;
	}

	let {
		current = null,
		title = '',
		why = '',
		wide = false,
		busy = false,
		onpick,
		onproblem
	}: Props = $props();

	let preview = $state<string | null>(null);
	let reading = $state(false);
	const shown = $derived(preview ?? current);
	const label = $derived(reading ? 'Čtu fotku…' : shown ? 'Vyměnit fotku' : 'Vybrat fotku');

	async function pick(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		input.value = '';
		if (!file) return;

		reading = true;
		try {
			const photo = await downscale(file);
			if (preview) URL.revokeObjectURL(preview);
			preview = URL.createObjectURL(photo);
			onpick(photo);
		} catch {
			onproblem('Tohle se nepodařilo přečíst jako fotku.');
		} finally {
			reading = false;
		}
	}

	$effect(() => () => {
		if (preview) URL.revokeObjectURL(preview);
	});
</script>

<article class="dream picker" class:dream--sky={!shown} class:dream--wide={wide}>
	{#if shown}
		<img class="dream__img" src={shown} alt="" />
	{/if}
	<div class="dream__body">
		{#if title}
			<h2 class="dream__title dream__title--sm">{title}</h2>
		{/if}
		{#if why}
			<p class="dream__why">{why}</p>
		{/if}
		<label class="btn btn--photo" class:picker__pill--busy={busy || reading}>
			<Icon name="camera" size={18} stroke={1.8} />
			{label}
			<input
				class="picker__input"
				type="file"
				accept="image/*"
				onchange={pick}
				disabled={busy || reading}
			/>
		</label>
	</div>
</article>

<style>
	.picker {
		flex: none;
	}

	/* On a desktop a 4:5 tile would push the card under the bar; it gives
	   up its ratio before the card gives up its place. */
	@media (min-width: 35rem) {
		.picker:not(.dream--wide) {
			max-height: 24rem;
		}
	}

	.picker__pill--busy {
		opacity: 0.6;
	}

	/* The real input, kept for the picker it opens and hidden from the eye;
	   the label is the pill. */
	.picker__input {
		position: absolute;
		width: 1px;
		height: 1px;
		opacity: 0;
		pointer-events: none;
	}
</style>
