<script lang="ts">
	/**
	 * Nastavení · Upozornění — the morning nudge (PLAN.md §3.6).
	 *
	 * Off, every day, or only on working days, and the times. One dream
	 * arrives at each hour chosen — up to five a day (D72) — and tapping it
	 * opens that dream. The browser is asked for permission only when the
	 * person picks something other than off, so nothing is ever demanded on
	 * the way in (D35).
	 *
	 * The times are a list of fields rather than one. Each row moves its own
	 * hour and takes itself away; the last one cannot be taken away, because
	 * „no reminders“ is what Vypnuto means and two ways to say it would
	 * disagree with each other.
	 */
	import type { NudgeMode } from '@aspire/contracts';
	import { MAX_NUDGE_TIMES, NUDGE_MODES } from '@aspire/contracts';
	import { describeError } from '$lib/api/errors';
	import {
		DEFAULT_TIMES,
		MODE_LABEL,
		fromClock,
		savedSentence,
		tidyTimes,
		toClock,
		withAnotherTime,
		withTimeMoved,
		withoutTime
	} from '$lib/push/schedule';
	import Icon from '$lib/ui/Icon.svelte';
	import { current, turnOff, turnOn, unavailable } from '$lib/push/nudge';
	import { connection } from '$lib/offline/status.svelte';
	import { writes } from '$lib/offline/writes.svelte';
	import AppBar from '$lib/ui/AppBar.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { toast } from '$lib/ui/toast.svelte';

	let mode = $state<NudgeMode>('off');

	/** When the dream arrives, in order: one to five (D72). */
	let times = $state<number[]>([...DEFAULT_TIMES]);
	let busy = $state(false);

	/** Whether there is room for another. */
	const room = $derived(times.length < MAX_NUDGE_TIMES);

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
				times = tidyTimes(settings.times);
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
		await apply(next, times);
	}

	/** One row moved to another hour. */
	async function retime(from: number, value: string) {
		const next = withTimeMoved(times, from, fromClock(value));
		times = next;
		if (mode !== 'off') await apply(mode, next);
	}

	/** One more reminder, at the first free hour after the last one. */
	async function add() {
		const next = withAnotherTime(times);
		if (next.length === times.length) return;
		times = next;
		if (mode !== 'off') await apply(mode, next);
	}

	/** One reminder gone — never the last, which is what Vypnuto is for. */
	async function drop(at: number) {
		const next = withoutTime(times, at);
		if (next.length === times.length) return;
		times = next;
		if (mode !== 'off') await apply(mode, next);
	}

	async function apply(next: NudgeMode, wanted: number[]) {
		const before = mode;
		const had = times;
		busy = true;
		try {
			const settings = next === 'off' ? await turnOff() : await turnOn(next, wanted);
			mode = settings.mode;
			times = tidyTimes(settings.times);
			if (next !== 'off') toast.show(savedSentence(settings.times));
		} catch (e) {
			times = had;
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

			<div class="field">
				<span class="field__label">Kdy</span>
				{#each times as at (at)}
					<div class="when">
						<input
							class="field__input when__at"
							type="time"
							value={toClock(at)}
							aria-label="Čas upozornění"
							use:writes={() => busy || !loaded}
							onchange={(event) => retime(at, event.currentTarget.value)}
						/>
						{#if times.length > 1}
							<button
								type="button"
								class="round round--sm"
								aria-label={`Odebrat ${toClock(at)}`}
								use:writes={() => busy || !loaded}
								onclick={() => drop(at)}
							>
								<Icon name="close" size={18} stroke={2} />
							</button>
						{/if}
					</div>
				{/each}

				{#if room}
					<button
						type="button"
						class="btn btn--quiet btn--sm when__add"
						use:writes={() => busy || !loaded}
						onclick={add}
					>
						<Icon name="plus" size={16} stroke={2} />
						Přidat čas
					</button>
				{/if}

				<span class="field__hint">
					Jeden sen pokaždé, ten dnešní — ťuknutím se otevře. Nejvýš {MAX_NUDGE_TIMES} za den.
				</span>
			</div>

			{#if !connection.online}
				<p class="hint">Bez připojení. Upozornění nastavíš, až bude signál.</p>
			{/if}
		{/if}
	</section>
</main>

<TabBar />

<style>
	/* A reminder: its hour, and the way to take it away. The field keeps its
	   full width until there is more than one, so a single reminder looks
	   exactly as it did before there could be several. */
	.when {
		display: flex;
		align-items: center;
		gap: var(--space-2);
	}

	.when__at {
		flex: 1;
		min-width: 0;
	}

	/* The ＋ is a quiet pill under the list rather than another row: it adds a
	   reminder, it is not one. */
	.when__add {
		align-self: flex-start;
	}
</style>
