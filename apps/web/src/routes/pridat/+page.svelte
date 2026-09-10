<script lang="ts">
	/**
	 * Přidat — the add screen: the photograph's tile, the dream's form with an
	 * ember pill, and back to the board when it is on it. The dream is made
	 * first and the photograph sent after, so a failed upload leaves a dream
	 * with the sky rather than nothing; a second tap then only sends the
	 * photograph.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import type { DreamInput } from '@aspire/contracts';
	import { createDream, updateDream, uploadImage } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import AppBar from '$lib/ui/AppBar.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import PhotoPicker from '$lib/ui/PhotoPicker.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let photo = $state<Blob | null>(null);
	let busy = $state(false);
	let error = $state('');

	/** The dream already made by an earlier tap, if the upload failed after it. */
	let createdId: string | null = null;

	async function add(input: DreamInput) {
		busy = true;
		error = '';
		try {
			if (createdId) await updateDream(createdId, input);
			else createdId = (await createDream(input)).id;
			if (photo) await uploadImage(createdId, photo);
			toast.show('Sen je na nástěnce');
			await goto(resolve('/'));
		} catch (e) {
			error = describeError(e);
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — přidat sen</title>
</svelte:head>

<main class="page">
	<AppBar title="Přidat sen" />
	<PhotoPicker wide {busy} onpick={(picked) => (photo = picked)} onproblem={(s) => (error = s)} />
	<DreamForm submitLabel="Přidat sen" accent {busy} {error} onsubmit={add} />
</main>
