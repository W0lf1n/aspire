<script lang="ts">
	/**
	 * Přidat — the add screen: the photograph's tile, the dream's form with an
	 * ember pill, and back to the board when it is on it. The dream is made
	 * first and the photograph sent after, so a failed upload leaves a dream
	 * with the sky rather than nothing; a second tap then only sends the
	 * photograph.
	 *
	 * The board is fetched alongside, only so the form can say when the dream
	 * being written is already written down (D50). It is a note and not a
	 * gate, so a fetch that fails is a form that does not mention it rather
	 * than a screen that cannot be used.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { Dream, DreamInput } from '@aspire/contracts';
	import { createDream, listBoard, updateDream, uploadImage } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import AppBar from '$lib/ui/AppBar.svelte';
	import DreamForm from '$lib/ui/DreamForm.svelte';
	import PhotoPicker from '$lib/ui/PhotoPicker.svelte';
	import { toast } from '$lib/ui/toast.svelte';
	import { connection } from '$lib/offline/status.svelte';
	import { cannot } from '$lib/offline/writes.svelte';
	import { CENTRED, toInput, type Focal } from '$lib/images/focal';

	const locked = $derived(cannot(connection.online, 'add'));
	let photo = $state<Blob | null>(null);

	/**
	 * A link shared into the app from another one (D56). Android's share sheet
	 * lands here through `share_target` in the manifest; on a phone that has
	 * no such thing, pasting into the sheet is the same road. `text` is the
	 * fallback because some apps put the link there rather than in `url`.
	 */
	const shared = $derived(
		page.url.searchParams.get('url') ?? page.url.searchParams.get('text') ?? null
	);

	/** Where the picked photograph is looked at, chosen before it is sent (D54). */
	let focal = $state<Focal>({ ...CENTRED });
	let busy = $state(false);
	let error = $state('');

	/** What is already on the board, for the duplicate note. Empty until it is. */
	let existing = $state<Dream[]>([]);

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams }) => {
				if (live) existing = dreams;
			})
			.catch(() => {
				// No board to compare against, so the form simply does not check.
			});
		return () => {
			live = false;
		};
	});

	/** The dream already made by an earlier tap, if the upload failed after it. */
	let createdId: string | null = null;

	async function add(input: DreamInput) {
		busy = true;
		error = '';
		try {
			if (createdId) await updateDream(createdId, input);
			else createdId = (await createDream(input)).id;
			if (photo) {
				await uploadImage(createdId, photo, 'dreamt', toInput(focal));
			}
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
	<PhotoPicker
		wide
		sharedLink={shared}
		busy={busy || !!locked}
		onpick={(picked, at) => {
			photo = picked;
			focal = at;
		}}
		onmove={(at) => (focal = at)}
		onproblem={(s) => (error = s)}
	/>
	<DreamForm submitLabel="Přidat sen" accent {existing} {busy} {error} {locked} onsubmit={add} />
</main>
