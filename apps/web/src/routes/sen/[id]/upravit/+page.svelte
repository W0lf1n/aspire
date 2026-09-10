<script lang="ts">
	/**
	 * Upravit — the same form as Přidat, holding what was saved, with an
	 * ink pill. Back is the dream, not the board.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream, DreamInput } from '@aspire/contracts';
	import { getDream, updateDream } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import AppBar from '$lib/ui/AppBar.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import { toast } from '$lib/ui/toast.svelte';
	import { connection } from '$lib/offline/status.svelte';

	const locked = $derived(connection.online ? '' : 'Bez připojení se sen nedá uložit.');
	let dream = $state<Dream | null>(null);
	let busy = $state(false);
	let error = $state('');

	const id = $derived(page.params.id ?? '');

	$effect(() => {
		const wanted = id;
		let live = true;
		getDream(wanted)
			.then((found) => {
				if (live) dream = found;
			})
			.catch((e: unknown) => {
				if (live) error = describeError(e);
			});
		return () => {
			live = false;
		};
	});

	async function save(input: DreamInput) {
		busy = true;
		error = '';
		try {
			await updateDream(id, input);
			toast.show('Uloženo');
			await goto(resolve('/sen/[id]', { id }));
		} catch (e) {
			error = describeError(e);
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — upravit sen</title>
</svelte:head>

<main class="page">
	<AppBar title="Upravit sen" back={{ dream: id }} />

	{#if dream}
		<DreamForm initial={dream} submitLabel="Uložit" {busy} {error} {locked} onsubmit={save} />
	{:else if error}
		<p class="error-text" role="alert">{error}</p>
	{/if}
</main>
