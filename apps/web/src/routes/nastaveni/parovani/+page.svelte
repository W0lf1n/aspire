<script lang="ts">
	/**
	 * Nastavení · Párování — where this device is handed its token. Prosper's
	 * screen without the address field: the client is always the same origin
	 * as the API, so there is the code, a name for the device list, and one
	 * pill. Paired, the card says so and offers to forget.
	 */
	import { DEFAULT_DEVICE_NAME, pairDevice, pairingError, unpairDevice } from '$lib/api/pairing';
	import { readToken } from '$lib/api/token';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let paired = $state(readToken() !== null);
	let code = $state('');
	let deviceName = $state('');
	let busy = $state(false);
	let error = $state('');

	async function runPair(event: SubmitEvent) {
		event.preventDefault();
		busy = true;
		error = '';
		try {
			await pairDevice(code, deviceName);
			code = '';
			paired = true;
			toast.show('Spárováno');
		} catch (e) {
			error = pairingError(e);
		} finally {
			busy = false;
		}
	}

	function runUnpair() {
		unpairDevice();
		paired = false;
		toast.show('Odpojeno');
	}
</script>

<svelte:head>
	<title>Aspire — párování</title>
</svelte:head>

<main class="page">
	<AppBar title="Párování" back="/nastaveni" />

	{#if paired}
		<section class="card">
			<p class="hint">
				<strong>Toto zařízení je spárované.</strong> Sny se načítají z tohoto webu.
			</p>
			<div class="actions">
				<button type="button" class="btn btn--quiet" onclick={runUnpair}>Odpojit</button>
			</div>
		</section>
	{:else}
		<form class="card" onsubmit={runPair}>
			<label class="field">
				<span class="field__label">Párovací kód</span>
				<input
					class="field__input"
					type="text"
					inputmode="numeric"
					autocomplete="one-time-code"
					placeholder="12 číslic ze serveru"
					bind:value={code}
					disabled={busy}
				/>
				<span class="field__hint">
					Kód je nastavený na serveru a napíšeš ho jednou. Telefon pak dostane vlastní klíč, který
					nikdy nevyprší.
				</span>
			</label>
			<label class="field">
				<span class="field__label">Název zařízení</span>
				<input
					class="field__input"
					type="text"
					autocomplete="off"
					placeholder={DEFAULT_DEVICE_NAME}
					bind:value={deviceName}
					disabled={busy}
				/>
			</label>
			{#if error}
				<p class="error-text" role="alert">{error}</p>
			{/if}
			<div class="actions">
				<button type="submit" class="btn btn--primary" disabled={busy || !code.trim()}>
					{busy ? 'Páruji…' : 'Spárovat'}
				</button>
			</div>
		</form>
	{/if}
</main>

<TabBar />
