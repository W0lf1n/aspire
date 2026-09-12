<script lang="ts">
	/**
	 * The photograph's place on a screen: the dream's tile with the picture in
	 * it, or the sky where the picture will be, and one photo pill that opens
	 * the phone's own picker — camera or library, the phone's choice. The file
	 * is downscaled here (`downscale.ts`) before anyone sees it, so the preview
	 * is exactly what the server will get.
	 *
	 * Beside it, once there is a picture, a second pill that says where it is
	 * looked at (D54). It opens the editor on the reel's own shape, and
	 * answers with a focal point the screen either sends with the upload — a
	 * picture picked here is positioned before the dream may even exist — or
	 * saves against the photograph that is already there.
	 */
	import { fetchImageFromUrl } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { photoStyle } from '$lib/dreams/photos';
	import { downscale } from '$lib/images/downscale';
	import { CENTRED, type Focal } from '$lib/images/focal';
	import CropEditor from './CropEditor.svelte';
	import Icon from './Icon.svelte';
	import Sheet from './Sheet.svelte';

	interface Props {
		/** What is there now: the saved photograph's URL, or nothing. */
		current?: string | null;
		/**
		 * A link to start the sheet with — a pin shared to the app from
		 * another one (D56). It fills the field and opens the sheet; it never
		 * fetches on its own, because anything on the phone can hand us one
		 * and a fetch is the server opening a connection.
		 */
		sharedLink?: string | null;
		/** Where the saved photograph is looked at (D54). */
		focal?: Focal;
		title?: string;
		why?: string;
		/** 16:10 on a form, where the words are below; 4:5 on a dream's own screen. */
		wide?: boolean;
		busy?: boolean;
		/** The downscaled photograph, ready to send, and where it is looked at. */
		onpick: (photo: Blob, focal: Focal) => void;
		/** The crop, after the editor. For a picture already saved, save it. */
		onmove?: (focal: Focal) => void;
		/** A sentence when the picture could not be read. */
		onproblem: (sentence: string) => void;
	}

	let {
		current = null,
		sharedLink = null,
		focal = CENTRED,
		title = '',
		why = '',
		wide = false,
		busy = false,
		onpick,
		onmove,
		onproblem
	}: Props = $props();

	let preview = $state<string | null>(null);
	let reading = $state(false);
	let placing = $state(false);

	/** Whether the link sheet is up, and what is typed in it. */
	let asking = $state(false);
	let link = $state('');

	/**
	 * Why the last link gave nothing, said inside the sheet.
	 *
	 * Not through `onproblem`: that sentence lands on the screen underneath,
	 * which while the sheet is up is a sentence nobody can see. A failure
	 * belongs where the thing that failed was asked for.
	 */
	let problem = $state('');

	/** A link shared in from another app opens the sheet with it ready to take. */
	$effect(() => {
		const shared = sharedLink?.trim();
		if (shared) {
			link = shared;
			problem = '';
			asking = true;
		}
	});

	const shown = $derived(preview ?? current);
	const label = $derived(reading ? 'Čtu fotku…' : shown ? 'Vyměnit fotku' : 'Vybrat fotku');

	/** Where the picture on this tile is looked at, as it is being decided. */
	let at = $state<Focal>({ ...CENTRED });

	/**
	 * While nothing has been picked here, this follows the saved photograph:
	 * the dream arrives after the first paint, and its crop with it. Once a
	 * file is picked the local one wins — the saved crop belongs to a picture
	 * that is about to be replaced.
	 */
	$effect(() => {
		if (!preview) at = { ...focal };
	});

	const style = $derived(photoStyle({ focusX: at.x, focusY: at.y, zoom: at.zoom }));

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
			// A new picture starts in the middle, and is offered the frame it
			// will really be seen in straight away.
			at = { ...CENTRED };
			onpick(photo, at);
			placing = true;
		} catch {
			onproblem('Tohle se nepodařilo přečíst jako fotku.');
		} finally {
			reading = false;
		}
	}

	function placed(chosen: Focal) {
		at = chosen;
		placing = false;
		onmove?.(chosen);
	}

	/**
	 * The picture behind a pasted link (D56). The server fetches it — the
	 * phone cannot read another origin's image, and cannot read a pin's page
	 * at all — and what comes back is treated exactly like a picked file,
	 * down to opening the editor on it.
	 */
	async function take() {
		if (reading || link.trim().length === 0) return;

		reading = true;
		try {
			const fetched = await fetchImageFromUrl(link);
			// Already 2048 from the server, so this only settles the format.
			const photo = await downscale(fetched);
			if (preview) URL.revokeObjectURL(preview);
			preview = URL.createObjectURL(photo);
			at = { ...CENTRED };
			onpick(photo, at);
			asking = false;
			link = '';
			placing = true;
		} catch (e) {
			problem = describeError(e);
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
		<img class="dream__img" src={shown} alt="" {style} />
	{/if}
	<div class="dream__body">
		{#if title}
			<h2 class="dream__title dream__title--sm">{title}</h2>
		{/if}
		{#if why}
			<p class="dream__why">{why}</p>
		{/if}
		<div class="picker__acts">
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

			{#if shown}
				<button
					type="button"
					class="btn btn--photo"
					onclick={() => (placing = true)}
					disabled={busy || reading}
				>
					<Icon name="image" size={18} stroke={1.8} />
					Posunout
				</button>
			{/if}

			<button
				type="button"
				class="btn btn--photo"
				onclick={() => {
					link = '';
					problem = '';
					asking = true;
				}}
				disabled={busy || reading}
			>
				<Icon name="link" size={18} stroke={1.8} />
				Z odkazu
			</button>
		</div>
	</div>
</article>

{#if shown}
	<CropEditor
		open={placing}
		src={shown}
		focal={at}
		{title}
		line={why}
		{busy}
		onsave={placed}
		oncancel={() => (placing = false)}
	/>
{/if}

<Sheet open={asking} title="Fotka z odkazu" onclose={() => (asking = false)}>
	<label class="field">
		<span class="field__label">Odkaz na obrázek nebo pin</span>
		<input
			class="field__input"
			type="url"
			inputmode="url"
			bind:value={link}
			placeholder="https://cz.pinterest.com/pin/…"
			autocomplete="off"
			autocapitalize="off"
			spellcheck="false"
			disabled={reading}
		/>
	</label>

	{#if problem}
		<p class="note" role="alert">{problem}</p>
	{:else}
		<p class="hint">
			Zkopíruj odkaz na pin nebo přímo na obrázek. Fotku stáhne server a uloží ji k tobě — ze
			stránky si nebere nic jiného.
		</p>
	{/if}

	<div class="actions actions--fill">
		<button type="button" class="btn" onclick={() => (asking = false)} disabled={reading}>
			Zrušit
		</button>
		<button
			type="button"
			class="btn btn--accent"
			onclick={take}
			disabled={reading || link.trim().length === 0}
		>
			{reading ? 'Stahuju…' : 'Vzít'}
		</button>
	</div>
</Sheet>

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

	/* The pill that picks and, once there is a picture, the one that moves it.
	   A row, wrapping on a narrow phone. */
	.picker__acts {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
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
