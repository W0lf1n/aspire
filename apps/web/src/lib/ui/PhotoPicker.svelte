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
	 *
	 * Where a dream has no photograph yet, one pick can choose up to five
	 * (D84) — `several`. The tile shows the first, which is the cover, and says
	 * how many came with it; the editor is not opened on a pick of several,
	 * because the first is about to become one cell of a collage and each is
	 * placed afterwards on the dream's shelf.
	 */
	import { matIsBlur, matStyle, photoStyle, tileStyle } from '$lib/dreams/photos';
	import { roomFor } from '$lib/dreams/upload';
	import { downscaleAll, unreadableSentence } from '$lib/images/downscale';
	import { CENTRED, toInput, type Focal } from '$lib/images/focal';
	import CropEditor from './CropEditor.svelte';
	import Icon from './Icon.svelte';
	import PhotoLinkSheet from './PhotoLinkSheet.svelte';

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
		/**
		 * Whether one pick may choose up to five (D84): a dream that has no
		 * photograph yet. Otherwise a pick is the one photograph that replaces.
		 */
		several?: boolean;
		/**
		 * The downscaled photograph, ready to send, where it is looked at, and
		 * any picked with it, in the order they were picked.
		 */
		onpick: (photo: Blob, focal: Focal, more: Blob[]) => void;
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
		several = false,
		onpick,
		onmove,
		onproblem
	}: Props = $props();

	let preview = $state<string | null>(null);
	let reading = $state(false);

	/** How many photographs the last pick here chose, and how many are being read now. */
	let picked = $state(0);
	let readingCount = $state(0);
	let placing = $state(false);

	/** Whether the link sheet is up, and the link it opens with, if any. */
	let asking = $state(false);
	let link = $state('');

	/** A link shared in from another app opens the sheet with it ready to take. */
	$effect(() => {
		const shared = sharedLink?.trim();
		if (shared) {
			link = shared;
			asking = true;
		}
	});

	const shown = $derived(preview ?? current);
	const label = $derived.by(() => {
		if (reading) return readingCount > 1 ? 'Čtu fotky…' : 'Čtu fotku…';
		if (shown) return picked > 1 ? 'Vyměnit fotky' : 'Vyměnit fotku';
		return several ? 'Vybrat fotky' : 'Vybrat fotku';
	});

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

	/** The picture as a tile shows it: filling, or whole on its mat (D81). */
	const style = $derived(tileStyle(toInput(at)));
	const mat = $derived(shown ? matStyle(at) : '');

	/**
	 * Whether the editor is up on a picture nobody has placed yet, which is
	 * when it may open a landscape one whole (`startsWhole`).
	 */
	let fresh = $state(false);

	async function pick(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const files = [...(input.files ?? [])];
		input.value = '';
		if (files.length === 0) return;

		// A dream with no photograph has room for all five; a replacement is one.
		const { taken: fit, note } = several
			? roomFor(files, 0)
			: { taken: files.slice(0, 1), note: null };
		reading = true;
		readingCount = fit.length;
		try {
			const { photos, unreadable } = await downscaleAll(fit);
			const said = note ?? (unreadable > 0 ? unreadableSentence(unreadable, fit.length) : null);
			if (said) onproblem(said);

			const [photo, ...more] = photos;
			if (!photo) return;

			if (preview) URL.revokeObjectURL(preview);
			preview = URL.createObjectURL(photo);
			picked = photos.length;
			// A new picture starts in the middle, and one picked alone is
			// offered the frame it will really be seen in straight away.
			at = { ...CENTRED };
			onpick(photo, at, more);
			if (more.length === 0) {
				fresh = true;
				placing = true;
			}
		} finally {
			reading = false;
		}
	}

	function placed(chosen: Focal) {
		at = chosen;
		placing = false;
		fresh = false;
		onmove?.(chosen);
	}

	/**
	 * The picture behind a pasted link (D56), from `PhotoLinkSheet`. It is
	 * treated exactly like a picked file, down to opening the editor on it.
	 */
	function taken(photo: Blob) {
		if (preview) URL.revokeObjectURL(preview);
		preview = URL.createObjectURL(photo);
		picked = 1;
		at = { ...CENTRED };
		onpick(photo, at, []);
		asking = false;
		link = '';
		fresh = true;
		placing = true;
	}

	$effect(() => () => {
		if (preview) URL.revokeObjectURL(preview);
	});
</script>

<article class="dream picker" class:dream--sky={!shown} class:dream--wide={wide} style={mat}>
	{#if shown}
		{#if matIsBlur(at)}
			<img
				class="dream__under"
				src={shown}
				alt=""
				aria-hidden="true"
				style={photoStyle(toInput(at))}
			/>
		{/if}
		<img class="dream__img" src={shown} alt="" {style} />
		{#if preview && picked > 1}
			<!-- The cover, and how many came with it: the rest are not on this tile. -->
			<span class="badge dream__tag">{picked} {picked < 5 ? 'fotky' : 'fotek'}</span>
		{/if}
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
					multiple={several}
					onchange={pick}
					disabled={busy || reading}
				/>
			</label>

			{#if shown}
				<button
					type="button"
					class="btn btn--photo"
					onclick={() => {
						fresh = false;
						placing = true;
					}}
					disabled={busy || reading}
				>
					<Icon name="image" size={18} stroke={1.8} />
					Umístit
				</button>
			{/if}

			<button
				type="button"
				class="btn btn--photo"
				onclick={() => {
					link = '';
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
		{fresh}
		{title}
		line={why}
		{busy}
		onsave={placed}
		oncancel={() => {
			placing = false;
			fresh = false;
		}}
	/>
{/if}

<PhotoLinkSheet open={asking} start={link} onphoto={taken} onclose={() => (asking = false)} />

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
