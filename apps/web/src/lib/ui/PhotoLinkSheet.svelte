<script lang="ts">
	/**
	 * Fotka z odkazu — the sheet that turns a pasted link into a photograph
	 * (D56). The server fetches it, because the phone cannot read another
	 * origin's image and cannot read a pin's page at all, and what comes back
	 * is handed on exactly like a file somebody picked: already downscaled,
	 * ready to be placed and sent.
	 *
	 * It was part of `PhotoPicker` until a dream could have five photographs
	 * (D82) and a second place needed to take one from a link.
	 *
	 * A failure is said inside the sheet, not on the screen underneath it:
	 * while the sheet is up that is a sentence nobody can see, and a failure
	 * belongs where the thing that failed was asked for.
	 */
	import { fetchImageFromUrl } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { downscale } from '$lib/images/downscale';
	import Sheet from './Sheet.svelte';

	interface Props {
		open: boolean;
		/**
		 * A link to start with — a pin shared to the app from another one. It
		 * fills the field; it never fetches on its own, because anything on
		 * the phone can hand us one and a fetch is the server opening a
		 * connection.
		 */
		start?: string;
		/** The photograph behind the link, downscaled, ready to send. */
		onphoto: (photo: Blob) => void;
		onclose: () => void;
	}

	let { open, start = '', onphoto, onclose }: Props = $props();

	let link = $state('');
	let problem = $state('');
	let reading = $state(false);

	// Every opening starts clean, or with the link it was opened for.
	$effect(() => {
		if (!open) return;
		link = start;
		problem = '';
	});

	function close() {
		if (!reading) onclose();
	}

	async function take() {
		if (reading || link.trim().length === 0) return;

		reading = true;
		try {
			const fetched = await fetchImageFromUrl(link);
			// Already 2048 from the server, so this only settles the format.
			onphoto(await downscale(fetched));
		} catch (e) {
			problem = describeError(e);
		} finally {
			reading = false;
		}
	}
</script>

<Sheet {open} title="Fotka z odkazu" onclose={close}>
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
		<button type="button" class="btn" onclick={close} disabled={reading}>Zrušit</button>
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
