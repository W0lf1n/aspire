<script lang="ts">
	/**
	 * Nastavení · Párování — where this device is handed its token. Prosper's
	 * screen without the address field: the client is always the same origin
	 * as the API, so there is the code, a name for the device list, and one
	 * pill. Paired, the card says so and offers to disconnect, and under it
	 * are the board's other devices, each of which can be taken off it (D93).
	 */
	import type { DeviceDto } from '@aspire/contracts';
	import { page } from '$app/state';
	import { listDevices, removeDevice } from '$lib/api/client';
	import { describeError } from '$lib/api/errors';
	import {
		DEFAULT_DEVICE_NAME,
		UNPAIR_OFFLINE,
		pairDevice,
		pairingError,
		unpairDevice
	} from '$lib/api/pairing';
	import { readToken } from '$lib/api/token';
	import { formatWhen } from '$lib/dreams/format';
	import { connection } from '$lib/offline/status.svelte';
	import { writes } from '$lib/offline/writes.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let paired = $state(readToken() !== null);
	/** The board took this device off it, and the board screen sent it here. */
	const disconnected = page.state.disconnected === true;
	let devices = $state<DeviceDto[]>([]);
	let removing = $state<string | null>(null);

	// The list needs the server; offline there is no list, and nothing to remove.
	$effect(() => {
		if (!paired || !connection.online) return;
		let live = true;
		listDevices()
			.then((rows) => {
				if (live) devices = rows;
			})
			.catch(() => {
				if (live) devices = [];
			});
		return () => {
			live = false;
		};
	});

	const others = $derived(devices.filter((device) => !device.current));

	/** When a device was last here, or when it paired if it never came back. */
	function seen(device: DeviceDto): string {
		return device.lastSeenAt
			? `naposledy ${formatWhen(device.lastSeenAt)}`
			: `spárováno ${formatWhen(device.pairedAt)}`;
	}
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

	async function runUnpair() {
		busy = true;
		try {
			await unpairDevice();
			paired = false;
			devices = [];
			toast.show('Odpojeno');
		} catch (e) {
			toast.show(e instanceof TypeError ? UNPAIR_OFFLINE : describeError(e));
		} finally {
			busy = false;
		}
	}

	async function runRemove(device: DeviceDto) {
		removing = device.id;
		try {
			await removeDevice(device.id);
			devices = devices.filter((one) => one.id !== device.id);
			toast.show(`${device.name} je odpojené`);
		} catch (e) {
			toast.show(describeError(e));
		} finally {
			removing = null;
		}
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
				<button type="button" class="btn btn--quiet" use:writes={() => busy} onclick={runUnpair}
					>Odpojit</button
				>
			</div>
		</section>

		{#if others.length > 0}
			<section class="card card--list" aria-label="Další zařízení">
				<p class="label devices__label">Další zařízení</p>
				{#each others as device (device.id)}
					<div class="row">
						<span class="row__body">
							<span class="row__title">{device.name}</span>
							<span class="row__sub">{seen(device)}</span>
						</span>
						<span class="row__end">
							<button
								type="button"
								class="btn btn--quiet btn--sm"
								aria-label={`Odpojit ${device.name}`}
								use:writes={() => removing !== null}
								onclick={() => runRemove(device)}>Odpojit</button
							>
						</span>
					</div>
				{/each}
			</section>
			<p class="hint">
				Odpojené zařízení nástěnku hned přestane vidět a nechodí mu upozornění. Vrátit ho jde jen
				párovacím kódem.
			</p>
		{/if}
	{:else}
		{#if disconnected}
			<p class="hint" role="status">
				<strong>Toto zařízení bylo odpojeno.</strong> Spáruj ho znovu kódem.
			</p>
		{/if}
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

<style>
	/* A list card's rows carry their own padding; the label above them does not. */
	.devices__label {
		padding-top: var(--space-3);
	}
</style>
