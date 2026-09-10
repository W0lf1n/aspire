<script lang="ts">
	/**
	 * Přidat — the add screen: the dream's form with an ember pill, and back
	 * to the board when it is on it. The photograph joins the screen with the
	 * images slice; until then a dream is words, and the board shows it as a
	 * tile with the sky where the picture will be.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import type { DreamInput } from '@aspire/contracts';
	import { createDream } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import AppBar from '$lib/ui/AppBar.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let busy = $state(false);
	let error = $state('');

	async function add(input: DreamInput) {
		busy = true;
		error = '';
		try {
			await createDream(input);
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
	<DreamForm submitLabel="Přidat sen" accent {busy} {error} onsubmit={add} />
</main>
