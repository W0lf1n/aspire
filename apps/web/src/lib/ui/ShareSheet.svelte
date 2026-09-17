<script lang="ts">
	/**
	 * Sdílet sen — the sheet with a link that is its own key (D61), for
	 * somebody who has no app and never will.
	 *
	 * It was the dream's own screen's until the Seznam's rows got a Sdílet of
	 * their own (D78); two screens opening the same sheet is one sheet, here.
	 *
	 * The link is asked for the first time the sheet is opened for a dream —
	 * not when the screen is, because nearly every dream is never shared and a
	 * request nobody asked for is a request nobody should make.
	 */
	import type { Dream } from '@aspire/contracts';
	import { dreamLink, makeDreamLink, revokeDreamLink } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { shareText, shareUrl } from '$lib/dreams/share';
	import { writes } from '$lib/offline/writes.svelte';
	import Icon from './Icon.svelte';
	import Sheet from './Sheet.svelte';
	import { toast } from './toast.svelte';

	interface Props {
		/** The dream being shared, or nothing while no row has asked. */
		dream: Pick<Dream, 'id' | 'title'> | null;
		open: boolean;
		onclose: () => void;
	}

	let { dream, open, onclose }: Props = $props();

	let linking = $state(false);

	/** Undefined until the sheet has asked; null when the dream is not shared. */
	let path = $state<string | null | undefined>(undefined);

	/** Whose link `path` is, so a second dream does not show the first one's. */
	let asked: string | null = null;

	const link = $derived(path ? shareUrl(location.origin, path) : '');

	$effect(() => {
		if (!open || !dream || asked === dream.id) return;

		const id = dream.id;
		asked = id;
		path = undefined;
		dreamLink(id)
			.then((found) => {
				if (asked === id) path = found.path;
			})
			.catch(() => {
				// A link that cannot be asked for is a dream that is not shared as
				// far as this sheet knows; it offers to make one, which is what it
				// would have offered anyway.
				if (asked === id) path = null;
			});
	});

	/**
	 * A link, or a new one in place of the old — which is the only way to
	 * revoke a key that is its own permission. Said out loud, because from
	 * here the new link looks exactly like the one it replaced.
	 */
	async function share() {
		if (!dream || linking) return;
		linking = true;
		try {
			const replacing = path !== null;
			path = (await makeDreamLink(dream.id)).path;
			toast.show(replacing ? 'Nový odkaz. Ten starý už nefunguje.' : 'Odkaz je hotový.');
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			linking = false;
		}
	}

	async function unshare() {
		if (!dream || linking) return;
		linking = true;
		try {
			await revokeDreamLink(dream.id);
			path = null;
			toast.show('Sen už nikdo nevidí.');
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			linking = false;
		}
	}

	/**
	 * The phone's own share sheet where there is one, because that is where
	 * the conversation this is going into already is; the clipboard where
	 * there is not.
	 */
	async function send() {
		if (!dream || !link) return;
		try {
			if (navigator.share) {
				await navigator.share({ title: dream.title, text: shareText(dream.title), url: link });
				return;
			}
			await navigator.clipboard.writeText(link);
			toast.show('Odkaz je zkopírovaný.');
		} catch (e) {
			// Backing out of the share sheet is not a failure worth a sentence.
			if (e instanceof DOMException && e.name === 'AbortError') return;
			toast.show('Poslat to nešlo. Podrž na odkazu prst a zkopíruj ho ručně.');
		}
	}
</script>

<Sheet {open} title="Sdílet sen" {onclose}>
	{#if path === undefined}
		<p class="hint">Moment…</p>
	{:else if path === null}
		<p class="hint">
			Odkaz ukáže tenhle jeden sen — fotku, název a afirmaci. Kdo ho dostane, nepotřebuje aplikaci
			ani kód a na zbytek nástěnky se z něj nedostane.
		</p>
		<div class="actions actions--fill">
			<button type="button" class="btn btn--accent" onclick={share} use:writes={() => linking}>
				<Icon name="link" size={18} stroke={1.8} />
				Vyrobit odkaz
			</button>
		</div>
	{:else}
		<p class="well url">{link}</p>
		<div class="actions actions--fill">
			<button type="button" class="btn btn--accent" onclick={send}>Poslat</button>
			<button type="button" class="btn btn--quiet" onclick={share} use:writes={() => linking}>
				Nový
			</button>
			<button type="button" class="btn btn--danger" onclick={unshare} use:writes={() => linking}>
				Zrušit
			</button>
		</div>
		<p class="hint">
			Sen je vidět, dokud odkaz nezrušíš. „Nový“ ho vymění za jiný a ten starý přestane platit.
		</p>
	{/if}
</Sheet>

<style>
	/* The link itself: long, and never broken in the wrong place — it is
	   going to be read back by somebody pasting it into another app. */
	.url {
		font-size: var(--text-sm);
		line-height: var(--leading-base);
		color: var(--ink-2);
		overflow-wrap: anywhere;
		user-select: all;
	}
</style>
