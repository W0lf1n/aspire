<script lang="ts">
	/**
	 * Nastavení · Tapeta — the lock-screen collage (PLAN.md §3.5).
	 *
	 * Pick up to six dreams, in the order they should appear, and the server
	 * draws them onto a canvas the size of this phone's own screen. Nothing
	 * is stored: the image comes back once and goes wherever the phone puts
	 * it. The point is the dream seen a hundred times a day without opening
	 * anything (D33).
	 *
	 * And the same collage without anybody opening this screen at all: the
	 * board's link is a key the phone's own morning automation can fetch, so
	 * the lock screen changes by itself rather than staying the six somebody
	 * chose in March (D60).
	 */
	import type { Dream } from '@aspire/contracts';
	import { boardLink, listBoard, makeBoardLink, revokeBoardLink, wallpaper } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { pickDaily } from '$lib/dreams/board';
	import { photoOf } from '$lib/dreams/photos';
	import {
		MAX_ON_WALLPAPER,
		canvasFor,
		toggleChosen,
		wallpaperCandidates,
		wallpaperLink,
		wallpaperPick
	} from '$lib/dreams/wallpaper';
	import { connection } from '$lib/offline/status.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let dreams = $state<Dream[]>([]);
	let chosen = $state<string[]>([]);
	let making = $state(false);

	/**
	 * Today's six, kept as they were worked out when the board arrived, so the
	 * pill that puts them back means the same thing all the way down the
	 * screen — and so the pill can hide itself when there is nothing to undo.
	 */
	let today = $state<string[]>([]);

	/** The board's link: undefined until asked, null when there is none (D60). */
	let path = $state<string | null | undefined>(undefined);
	let linking = $state(false);

	/** The last collage made, kept so the preview is what was actually drawn. */
	let made = $state<string | null>(null);

	const candidates = $derived(wallpaperCandidates(dreams));
	const left = $derived(MAX_ON_WALLPAPER - chosen.length);
	const changed = $derived(chosen.join(',') !== today.join(','));

	/**
	 * The whole link, which is what goes into the Shortcut. The origin comes
	 * from the browser, which knows it for certain; the canvas and the offset
	 * are baked in because the link is static and whatever it carries is what
	 * the automation will ask for every morning from now on.
	 */
	const url = $derived(
		path ? wallpaperLink(location.origin, path, canvasNow(), -new Date().getTimezoneOffset()) : ''
	);

	/** This phone's screen in real pixels, which is the wallpaper's size. */
	function canvasNow() {
		return canvasFor(window.screen.width, window.screen.height, devicePixelRatio);
	}

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams: rows }) => {
				if (!live) return;
				dreams = rows;
				// The screen opens on a collage rather than on a question: the
				// day's dream and the ones with the most fuel, already chosen
				// (D58). Worked out once, here, because the pick draws at
				// random among dreams never shown and a derived value that
				// moves is a choice that rearranges itself under a thumb.
				today = wallpaperPick(rows, pickDaily(rows)?.id ?? null).map((dream) => dream.id);
				chosen = today;
			})
			.catch(() => {
				if (live) dreams = [];
			});
		return () => {
			live = false;
		};
	});

	$effect(() => {
		let live = true;
		boardLink()
			.then((link) => {
				if (live) path = link.path;
			})
			// A board whose link cannot be asked for is a board with no link
			// on the screen; the section says how to make one, which is what
			// it would say anyway.
			.catch(() => {
				if (live) path = null;
			});
		return () => {
			live = false;
		};
	});

	// A blob URL outlives the page unless it is let go of.
	$effect(() => () => {
		if (made) URL.revokeObjectURL(made);
	});

	function choose(id: string) {
		const next = toggleChosen(chosen, id);
		if (next === chosen) {
			toast.show(`Víc než ${MAX_ON_WALLPAPER} snů se na tapetu nevejde.`);
			return;
		}
		chosen = next;
	}

	/** Today's six back, after the choice has been played with. */
	function restore() {
		chosen = today;
	}

	/**
	 * A link, or a new one in place of the old — which is the only way to
	 * revoke a key that is its own permission. Said out loud, because from
	 * here the old link looks exactly like the new one.
	 */
	async function link() {
		if (linking) return;
		linking = true;
		try {
			const replacing = path !== null;
			path = (await makeBoardLink()).path;
			toast.show(replacing ? 'Nový odkaz. Ten starý už nefunguje.' : 'Odkaz je hotový.');
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			linking = false;
		}
	}

	async function unlink() {
		if (linking) return;
		linking = true;
		try {
			await revokeBoardLink();
			path = null;
			toast.show('Odkaz zrušený.');
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			linking = false;
		}
	}

	/** The link onto the clipboard, because it is going into another app. */
	async function copy() {
		try {
			await navigator.clipboard.writeText(url);
			toast.show('Odkaz je zkopírovaný.');
		} catch {
			toast.show('Zkopírovat to nešlo. Podrž na odkazu prst a zkopíruj ho ručně.');
		}
	}

	/**
	 * The collage, and then the phone's own way of keeping it: the share
	 * sheet where there is one, because on a phone that is where „Uložit
	 * obrázek“ lives and a download link is not. A browser without it gets
	 * the link instead.
	 */
	async function make() {
		if (making || chosen.length === 0) return;
		making = true;
		try {
			const canvas = canvasNow();
			const image = await wallpaper(chosen, canvas);

			if (made) URL.revokeObjectURL(made);
			made = URL.createObjectURL(image);

			const file = new File([image], 'aspire-tapeta.jpg', { type: 'image/jpeg' });
			if (navigator.canShare?.({ files: [file] })) {
				await navigator.share({ files: [file] });
			} else {
				const link = document.createElement('a');
				link.href = made;
				link.download = 'aspire-tapeta.jpg';
				link.click();
			}
		} catch (e) {
			// A share the person backed out of is not a failure worth a sentence.
			if (e instanceof DOMException && e.name === 'AbortError') return;
			toast.show(describeError(e));
		} finally {
			making = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — tapeta</title>
</svelte:head>

<main class="page">
	<AppBar title="Tapeta" back="/nastaveni" />

	{#if !connection.online}
		<p class="hint">Bez připojení. Tapetu skládá server, takže tahle počká na signál.</p>
	{:else if candidates.length === 0}
		<section class="card empty">
			<span class="circle circle--lg circle--sky"><Icon name="image" size={24} stroke={1.8} /></span
			>
			<h2 class="empty__title">Zatím není z čeho</h2>
			<p class="hint">Na tapetu jdou sny, které mají fotku. Přidej ji některému a vrať se sem.</p>
		</section>
	{:else}
		<div class="lede">
			<p class="hint">
				Dnešní sny už jsou vybrané — ťuknutím je měníš. Skládají se v pořadí, ve kterém je ťukneš,
				na plochu velkou jako displej tohohle telefonu.
			</p>
			<!-- Only when there is something to put back. -->
			{#if changed}
				<button type="button" class="btn btn--quiet btn--sm" onclick={restore}>Dnešních šest</button
				>
			{/if}
		</div>

		<!--
			The choice: the photographs themselves, small and square, because
			this is a screen about pictures and a list of titles would be a
			screen about words. The number on a chosen one is its place on the
			collage, which is the only thing the order means.
		-->
		<section class="pick">
			{#each candidates as dream (dream.id)}
				{@const photo = photoOf(dream, 'dreamt')}
				{@const place = chosen.indexOf(dream.id)}
				<button
					type="button"
					class="tile"
					class:tile--on={place >= 0}
					aria-pressed={place >= 0}
					aria-label={dream.title}
					onclick={() => choose(dream.id)}
				>
					{#if photo}
						<img class="tile__img" src={photo.thumbUrl} alt="" loading="lazy" decoding="async" />
					{/if}
					{#if place >= 0}
						<span class="badge tile__place">{place + 1}</span>
					{/if}
				</button>
			{/each}
		</section>

		<div class="actions actions--fill">
			<button
				type="button"
				class="btn btn--accent"
				disabled={making || chosen.length === 0}
				onclick={make}
			>
				<Icon name="image" size={18} stroke={1.8} />
				{making ? 'Skládám…' : 'Udělat tapetu'}
			</button>
		</div>
		<p class="hint">
			{chosen.length === 0
				? 'Ťukni na fotku.'
				: left > 0
					? `Vybráno ${chosen.length}, vejde se ještě ${left}.`
					: 'Plno. Ťuknutím na vybranou ji zase sundáš.'}
		</p>

		{#if made}
			<section class="card">
				<p class="label">Hotová tapeta</p>
				<img class="made" src={made} alt="Složená tapeta" />
				<p class="hint">
					Podrž na ní prst a ulož ji do fotek. Pak ji telefon nastaví jako pozadí sám.
				</p>
			</section>
		{/if}

		<!--
			The lock screen that changes by itself (D60). Six dreams chosen in
			March are wallpaper by April — the same habituation the reel's
			shuffle and the daily pick fight, on the surface that habituates
			fastest — so the phone fetches a new collage every morning and
			nobody has to remember this screen exists.
		-->
		<section class="card">
			<p class="label">Každé ráno sama</p>

			{#if path === undefined}
				<p class="hint">Moment…</p>
			{:else if path === null}
				<p class="hint">
					Telefon si tapetu umí stáhnout sám každé ráno — dnešních šest, vždycky čerstvých.
					Potřebuje k tomu odkaz.
				</p>
				<div class="actions">
					<button type="button" class="btn btn--card" disabled={linking} onclick={link}>
						<Icon name="link" size={18} stroke={1.8} />
						Vyrobit odkaz
					</button>
				</div>
			{:else}
				<p class="well url">{url}</p>
				<div class="actions actions--fill">
					<button type="button" class="btn btn--card" onclick={copy}>Zkopírovat</button>
					<button type="button" class="btn btn--quiet" disabled={linking} onclick={link}>
						Nový
					</button>
					<button type="button" class="btn btn--danger" disabled={linking} onclick={unlink}>
						Zrušit
					</button>
				</div>

				<ol class="steps">
					<li>Zkratky → Automatizace → Nová → Denně, 6:55.</li>
					<li>Akce „Získat obsah URL“ a do ní ten odkaz.</li>
					<li>Akce „Nastavit tapetu“ — zamčená obrazovka, bez náhledu.</li>
					<li>Vypnout „Zeptat se před spuštěním“.</li>
				</ol>

				<p class="hint">
					Tapeta musí být obyčejná fotka, ne prolínačka, jinak ji zkratka nepřepíše. Stáhne se kolem
					megabajtu přes to, co telefon zrovna má. Kdo ten odkaz má, vidí každé ráno šest fotek z
					týhle nástěnky a nic víc — „Nový“ ho vymění, „Zrušit“ ho zabije.
				</p>
			{/if}
		</section>
	{/if}
</main>

<TabBar />

<style>
	.empty {
		align-items: flex-start;
		gap: var(--space-3);
		padding: var(--space-5) var(--space-4);
	}

	.empty__title {
		font-size: var(--text-xl);
		font-weight: 600;
		letter-spacing: var(--track-xl);
	}

	/* Three across on a phone, and as many as fit above that: the tiles are
	   the thumbnails the media store already makes, so the grid costs one
	   small file each. */
	.pick {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(6.5rem, 1fr));
		gap: var(--space-2);
	}

	.tile {
		position: relative;
		aspect-ratio: 1;
		overflow: hidden;
		border-radius: var(--radius-md);
		background: var(--surface-3);
		transition:
			transform var(--dur-fast) var(--ease-out),
			outline-color var(--dur-fast) var(--ease-out);
		outline: 2px solid transparent;
		outline-offset: -2px;
	}

	.tile:active {
		transform: scale(0.98);
	}

	/* Chosen is the ember outline and the number; the photograph is not
	   dimmed, because dimming the picture is the one thing this screen is
	   not allowed to do to it. */
	.tile--on {
		outline-color: var(--signal);
	}

	.tile__img {
		width: 100%;
		height: 100%;
		object-fit: cover;
	}

	.tile__place {
		position: absolute;
		top: var(--space-2);
		left: var(--space-2);
		background: var(--signal);
		color: var(--signal-ink);
	}

	/* The hint and the pill that puts today's six back, on one line where
	   there is room and two where there is not. */
	.lede {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		justify-content: space-between;
		gap: var(--space-2);
	}

	.lede .hint {
		flex: 1 1 14rem;
	}

	/* The link itself: long, and never to be broken in the wrong place — it
	   is going to be read back by somebody typing it into another app. */
	.url {
		font-size: var(--text-sm);
		line-height: var(--leading-base);
		color: var(--ink-2);
		overflow-wrap: anywhere;
		user-select: all;
	}

	/* The steps, numbered by the browser: this is a recipe, and a recipe is
	   the one place on this screen where an order is the meaning. */
	.steps {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		margin: 0;
		padding-left: var(--space-5);
		font-size: var(--text-sm);
		line-height: var(--leading-base);
		color: var(--ink-2);
	}

	.steps li::marker {
		color: var(--ink-3);
		font-weight: 600;
	}

	/* The result, at the shape it will actually be. */
	.made {
		width: 100%;
		max-width: 14rem;
		align-self: center;
		border-radius: var(--radius-md);
	}
</style>
