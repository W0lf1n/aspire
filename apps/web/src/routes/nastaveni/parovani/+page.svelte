<script lang="ts">
	/**
	 * Nastavení · Párování — where this device is handed its token. A stub in
	 * M0: the shape of the screen, with the field it will have, and nothing
	 * behind it yet. The flow itself arrives with M1, when there is something
	 * to fetch with the token.
	 */
	import { readToken } from '$lib/api/token';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';

	const paired = readToken() !== null;
</script>

<svelte:head>
	<title>Aspire — párování</title>
</svelte:head>

<main class="page">
	<AppBar title="Párování" back="/nastaveni" />

	<section class="card">
		{#if paired}
			<p class="hint">
				<strong>Toto zařízení je spárované.</strong> Sny se načítají z tohoto webu.
			</p>
		{:else}
			<label class="field">
				<span class="field__label">Párovací kód</span>
				<input
					class="field__input"
					type="text"
					inputmode="numeric"
					autocomplete="one-time-code"
					placeholder="12 číslic ze serveru"
					disabled
				/>
				<span class="field__hint">
					Kód je nastavený na serveru a napíšeš ho jednou. Telefon pak dostane vlastní klíč, který
					nikdy nevyprší.
				</span>
			</label>
			<div class="actions">
				<button type="button" class="btn btn--primary" disabled>Spárovat</button>
			</div>
			<p class="hint">Párování přijde s dalším milníkem.</p>
		{/if}
	</section>
</main>

<TabBar />
