<script lang="ts">
	/**
	 * Nastavení · Upozornění — the morning nudge (PLAN.md §3.6).
	 *
	 * Off, every day, or only on working days, and the time. One dream
	 * arrives at the hour chosen; tapping it opens that dream. The browser is
	 * asked for permission only when the person picks something other than
	 * off, so nothing is ever demanded on the way in (D35).
	 */
	import type { NudgeMode } from '@aspire/contracts';
	import { NUDGE_MODES } from '@aspire/contracts';
	import { describeError } from '$lib/api/errors';
	import { DEFAULT_AT_MINUTES, MODE_LABEL, fromClock, toClock } from '$lib/push/schedule';
	import { current, turnOff, turnOn, unavailable } from '$lib/push/nudge';
	import { connection } from '$lib/offline/status.svelte';
	import { writes } from '$lib/offline/writes.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let mode = $state<NudgeMode>('off');
	let clock = $state(toClock(DEFAULT_AT_MINUTES));
	let busy = $state(false);

	/**
	 * Whether the screen knows yet. Until it does the controls are drawn and
	 * disabled rather than withheld: the shape of the screen is the same
	 * either way, and a switch that cannot be tapped for half a second is
	 * quieter than a sentence that appears and goes.
	 */
	let loaded = $state(false);

	/**
	 * Why there is nothing to offer here, or nothing — this browser, this
	 * server, and what the browser has already been asked. Answered before
	 * the switch is drawn rather than after it is tapped, because tapping it
	 * spends the one notification prompt a browser will ever give.
	 */
	let blocked = $state<string | null>(null);

	$effect(() => {
		let live = true;
		Promise.all([current(), unavailable()])
			.then(([settings, why]) => {
				if (!live) return;
				mode = settings.mode;
				clock = toClock(settings.atMinutes);
				blocked = why;
			})
			.catch(() => undefined)
			.finally(() => {
				if (live) loaded = true;
			});
		return () => {
			live = false;
		};
	});

	/**
	 * Saving is what the switch does — there is no separate pill, because
	 * the choice is the action. A refusal puts the switch back where it was,
	 * so the screen never claims something the browser did not agree to.
	 */
	async function choose(next: NudgeMode) {
		if (busy || next === mode) return;
		await apply(next, fromClock(clock));
	}

	async function retime(value: string) {
		clock = value;
		if (mode !== 'off') await apply(mode, fromClock(value));
	}

	async function apply(next: NudgeMode, atMinutes: number) {
		const before = mode;
		busy = true;
		try {
			const settings = next === 'off' ? await turnOff() : await turnOn(next, atMinutes);
			mode = settings.mode;
			clock = toClock(settings.atMinutes);
			if (next !== 'off') toast.show(`Sen ti přijde v ${toClock(settings.atMinutes)}`);
		} catch (e) {
			mode = before;
			toast.show(
				e instanceof Error && e.message === 'denied'
					? 'Prohlížeč upozornění nepovolil.'
					: e instanceof Error && e.message === 'unconfigured'
						? 'Server zatím upozornění posílat neumí.'
						: describeError(e)
			);
			// A refusal is a new fact about this browser: a second tap would
			// not even raise the prompt, so the switch gives way to the
			// sentence that says where to change it.
			blocked = await unavailable();
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>Aspire — upozornění</title>
</svelte:head>

<main class="page">
	<AppBar title="Upozornění" back="/nastaveni" />

	<section class="card">
		{#if blocked}
			<p class="hint">{blocked}</p>
		{:else}
			<div class="seg seg--soft" role="group" aria-label="Upozornění">
				{#each NUDGE_MODES as value (value)}
					<button
						type="button"
						class="seg__item"
						aria-pressed={mode === value}
						use:writes={() => busy || !loaded}
						onclick={() => choose(value)}
					>
						{MODE_LABEL[value]}
					</button>
				{/each}
			</div>

			<label class="field">
				<span class="field__label">Kdy</span>
				<input
					class="field__input"
					type="time"
					value={clock}
					use:writes={() => busy || !loaded}
					onchange={(event) => retime(event.currentTarget.value)}
				/>
				<span class="field__hint">
					Ráno, než začne den. Jeden sen, ten dnešní — ťuknutím se otevře.
				</span>
			</label>

			{#if !connection.online}
				<p class="hint">Bez připojení. Upozornění nastavíš, až bude signál.</p>
			{/if}
		{/if}
	</section>
</main>

<TabBar />
