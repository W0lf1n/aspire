<script lang="ts">
	/**
	 * Nastavení · Začít znovu — every dream and every photograph, gone, and
	 * the board left standing to be written on again (D80).
	 *
	 * The one thing in the app that cannot be taken back. Smazat holds its
	 * request for six seconds and offers „Vrátit“ (D65); a whole board cannot
	 * be held in a toast, so this does what Prosper does instead: it says what
	 * goes and what stays, and then asks for a sentence to be typed out.
	 * `dreams/reset.ts` is the phrase, and the server asks for it again.
	 *
	 * It says how many dreams it is about to take, from the board this device
	 * already has. A number is the difference between „I know what this does“
	 * and knowing what it is about to do.
	 */
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { listBoard, resetBoard } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import { readToken } from '$lib/api/token';
	import { RESET_PHRASE, boardHolds, matchesResetPhrase, resetDone } from '$lib/dreams/reset';
	import { connection } from '$lib/offline/status.svelte';
	import { writes } from '$lib/offline/writes.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import Sheet from '$lib/ui/Sheet.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	const paired = readToken() !== null;

	/** How many dreams there are to lose, or null until the board has said. */
	let count = $state<number | null>(null);

	let asking = $state(false);
	let typed = $state('');
	let busy = $state(false);
	let error = $state('');

	const unlocked = $derived(matchesResetPhrase(typed));

	$effect(() => {
		let live = true;
		listBoard()
			.then(({ dreams }) => {
				if (live) count = dreams.length;
			})
			.catch(() => undefined);
		return () => {
			live = false;
		};
	});

	/* A fresh sheet every time it opens: a half-typed phrase left over from a
	   sheet somebody backed out of is a phrase that is already half-passed. */
	function ask() {
		typed = '';
		error = '';
		asking = true;
	}

	function close() {
		if (!busy) asking = false;
	}

	async function run(event: SubmitEvent) {
		event.preventDefault();
		if (!unlocked || busy) return;

		busy = true;
		error = '';
		try {
			const { dreams } = await resetBoard(typed);
			asking = false;
			toast.show(resetDone(dreams));
			await goto(resolve('/'));
		} catch (e) {
			error = describeError(e);
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — začít znovu</title>
</svelte:head>

<main class="page">
	<AppBar title="Začít znovu" back="/nastaveni" />

	{#if !paired}
		<section class="card">
			<p class="hint">Až bude zařízení spárované, bude tu i tohle.</p>
		</section>
	{:else}
		<section class="card">
			<p class="prose">
				Smaže celou nástěnku a nechá ti ji prázdnou — jako první den.
				{#if count !== null}
					<strong>{boardHolds(count)}</strong>
				{/if}
			</p>

			<dl class="facts">
				<div>
					<dt>Zmizí</dt>
					<dd>sny, fotky, „teď“ a sdílené odkazy</dd>
				</div>
				<div>
					<dt>Zůstane</dt>
					<dd>spárování, upozornění, tapeta, vzhled</dd>
				</div>
			</dl>

			<p class="hint">
				Platí to pro všechna zařízení spárovaná s touhle nástěnkou a nejde to vrátit — fotky se
				mažou i ze serveru.
			</p>

			<div class="actions actions--fill">
				<button type="button" class="btn btn--danger btn--lg" onclick={ask} use:writes>
					Začít znovu
				</button>
			</div>

			{#if !connection.online}
				<p class="hint">Bez připojení to nejde. Počká to na signál.</p>
			{/if}
		</section>
	{/if}
</main>

<TabBar />

<Sheet open={asking} title="Opravdu začít znovu?" onclose={close}>
	<form class="sheet__form" onsubmit={run}>
		<p class="hint">
			Pro potvrzení napiš <strong>{RESET_PHRASE}</strong>. Na velkých písmenech ani háčkách
			nezáleží.
		</p>

		<label class="field">
			<span class="field__label">Potvrzení</span>
			<input
				class="field__input"
				type="text"
				bind:value={typed}
				placeholder={RESET_PHRASE}
				autocomplete="off"
				autocapitalize="off"
				spellcheck="false"
				enterkeyhint="done"
				disabled={busy}
			/>
		</label>

		{#if error}
			<p class="note" role="alert">{error}</p>
		{/if}

		<div class="actions actions--fill">
			<button type="button" class="btn" onclick={close} disabled={busy}>Zrušit</button>
			<button type="submit" class="btn btn--danger" use:writes={() => busy || !unlocked}>
				{busy ? 'Mažu…' : 'Smazat všechno'}
			</button>
		</div>
	</form>
</Sheet>

<style>
	/* The sheet's body is a column with a gap; a form inside it is one child,
	   so it carries the same column on. */
	.sheet__form {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}
</style>
