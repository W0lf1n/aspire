<script lang="ts">
	/**
	 * Nastavení · Stahování — how much of the board this phone keeps for when
	 * there is no signal (D39).
	 *
	 * A hundred dreams is tens of megabytes. Storing that is nothing for a
	 * phone; fetching it over somebody's mobile data is not, and a cache big
	 * enough is also a cache a browser starts evicting on its own. So the
	 * choice is the connection the megabytes arrive over, not the room they
	 * take.
	 *
	 * Where the browser cannot see the connection — Safari has no Network
	 * Information API, so every iPhone — the wifi choice is not offered at
	 * all. A switch that cannot work is worse than a sentence saying why,
	 * which is how the morning nudge handles a server with no key (§15).
	 */
	import { forgetBoard } from '$lib/offline/cache';
	import {
		POLICY_LABEL,
		detects,
		effectivePolicy,
		formatUsage,
		metered,
		readPolicy,
		usage,
		writePolicy,
		type OfflinePolicy
	} from '$lib/offline/policy';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	/** Whether this browser can tell wifi from mobile data. */
	const knows = detects();

	/** The choices this browser can actually keep. */
	const offered: OfflinePolicy[] = knows ? ['window', 'wifi', 'all'] : ['window', 'all'];

	let policy = $state(effectivePolicy(readPolicy(), knows));

	/** What the app takes up on the phone, once the browser has said. */
	let taken = $state<number | null>(null);

	$effect(() => {
		let live = true;
		void usage().then((bytes) => {
			if (live) taken = bytes;
		});
		return () => {
			live = false;
		};
	});

	/**
	 * The choice is the action; there is no saving pill. It takes effect on
	 * the next board, because the board is what fetches — this screen never
	 * starts a download of its own while somebody is standing on it.
	 */
	function choose(next: OfflinePolicy) {
		if (next === policy) return;
		policy = next;
		writePolicy(next);
	}

	async function forget() {
		await forgetBoard();
		taken = await usage();
		toast.show('Stažené sny jsou smazané.');
	}
</script>

<svelte:head>
	<title>Aspire — stahování</title>
</svelte:head>

<main class="page">
	<AppBar title="Stahování" back="/nastaveni" />

	<section class="card">
		<div class="seg seg--soft" role="group" aria-label="Stahování">
			{#each offered as value (value)}
				<button
					type="button"
					class="seg__item"
					aria-pressed={policy === value}
					onclick={() => choose(value)}
				>
					{POLICY_LABEL[value]}
				</button>
			{/each}
		</div>

		<p class="hint">
			{#if policy === 'window'}
				Stahují se jen sny, ke kterým se prolistuješ, a pár dopředu. Nejmenší účet za data; bez
				signálu máš po ruce to, co jsi viděl naposledy.
			{:else if policy === 'wifi'}
				Na wifi se stáhne celá nástěnka, aby fungovala i bez signálu. Na mobilních datech jen to, co
				prolistuješ.
			{:else}
				Celá nástěnka se stáhne vždycky, i na mobilních datech. Sto snů jsou desítky megabajtů.
			{/if}
		</p>

		{#if !knows}
			<p class="hint">
				Tenhle prohlížeč neumí rozpoznat wifi od mobilních dat — na iPhonu to Safari neprozradí —
				takže volba „na wifi“ tu není. Šetřící režim je ten první.
			</p>
		{:else if metered() === true}
			<p class="hint">Teď jsi na mobilních datech.</p>
		{/if}
	</section>

	<section class="card">
		<dl class="facts">
			<div>
				<dt>Na telefonu</dt>
				<dd>{taken === null ? '—' : formatUsage(taken)}</dd>
			</div>
		</dl>
		<div class="actions">
			<button type="button" class="btn btn--quiet" onclick={forget}>Smazat stažené</button>
		</div>
		<p class="hint">
			Fotky i nástěnka z paměti telefonu. Sny samotné jsou na serveru — smazáním se nic neztratí,
			jen se příště stáhnou znovu.
		</p>
	</section>
</main>

<TabBar />
