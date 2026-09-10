<script lang="ts">
	/**
	 * The dream's form: a title, the why, the affirmation, a status, an area,
	 * a year, and one pill.
	 * Přidat and Upravit are this card with a different pill; the photograph
	 * is the screen's own business, above or below it. The form checks the
	 * fields on the way out (`rules.ts`) and shows the server's sentence when
	 * the server disagrees.
	 */
	import type { DreamCategory, DreamInput, DreamStatus } from '@aspire/contracts';
	import { DREAM_CATEGORIES, DREAM_STATUSES } from '@aspire/contracts';
	import {
		AFFIRMATION_MAX,
		CATEGORY_LABEL,
		STATUS_LABEL,
		TITLE_MAX,
		WHY_MAX,
		toFields,
		toInput
	} from '$lib/dreams/rules';

	interface Props {
		/** The saved values, when editing. */
		initial?: DreamInput;
		/** What the pill says saving means here. */
		submitLabel: string;
		/** Ember for the one action a screen is about; ink otherwise. */
		accent?: boolean;
		busy: boolean;
		/** The server's sentence, or nothing. */
		error: string;
		/** Why saving is not possible right now — offline — or nothing. */
		locked?: string;
		onsubmit: (input: DreamInput) => void;
	}

	let {
		initial,
		submitLabel,
		accent = false,
		busy,
		error,
		locked = '',
		onsubmit
	}: Props = $props();

	// Read once, on purpose: the form seeds from what was saved and then owns
	// its fields; a dream changing under it is a screen's business, not this.
	// svelte-ignore state_referenced_locally
	const start = initial ? toFields(initial) : null;
	let title = $state(start?.title ?? '');
	let why = $state(start?.why ?? '');
	let affirmation = $state(start?.affirmation ?? '');
	let status = $state<DreamStatus>(start?.status ?? 'dreaming');
	let category = $state<DreamCategory | null>(start?.category ?? null);
	let year = $state(start?.year ?? '');
	let problem = $state('');

	function submit(event: SubmitEvent) {
		event.preventDefault();
		const read = toInput({ title, why, affirmation, status, category, year });
		if ('problem' in read) {
			problem = read.problem;
			return;
		}
		problem = '';
		onsubmit(read.input);
	}
</script>

<form class="card" onsubmit={submit}>
	<label class="field">
		<span class="field__label">Název</span>
		<input
			class="field__input"
			type="text"
			bind:value={title}
			maxlength={TITLE_MAX}
			placeholder="Pár slov, jak tomu říkáš"
			autocomplete="off"
			disabled={busy}
		/>
	</label>

	<label class="field">
		<span class="field__label">Proč</span>
		<textarea
			class="field__input field__input--area"
			bind:value={why}
			maxlength={WHY_MAX}
			placeholder="Jedna věta, kterou uvidíš každý den."
			disabled={busy}></textarea>
		<span class="field__hint">Krátce. Je to připomínka, ne esej.</span>
	</label>

	<label class="field">
		<span class="field__label">Afirmace <span>nepovinné</span></span>
		<input
			class="field__input"
			type="text"
			bind:value={affirmation}
			maxlength={AFFIRMATION_MAX}
			placeholder="Bydlím u lesa."
			autocomplete="off"
			disabled={busy}
		/>
		<span class="field__hint"
			>Řekni to, jako by to už platilo. Na nástěnce ji uvidíš místo proč.</span
		>
	</label>

	<div class="field">
		<span class="field__label">Stav</span>
		<div class="seg seg--soft" role="group" aria-label="Stav">
			{#each DREAM_STATUSES as value (value)}
				<button
					type="button"
					class="seg__item"
					aria-pressed={status === value}
					disabled={busy}
					onclick={() => (status = value)}
				>
					{STATUS_LABEL[value]}
				</button>
			{/each}
		</div>
	</div>

	<div class="field">
		<span class="field__label">Oblast <span>nepovinné</span></span>
		<!--
			Nine areas do not fit a segmented pill, so they are chips that wrap.
			Pressing the one already chosen takes it off again, which is the
			whole of „no area“ — an extra „žádná“ chip would be a tenth thing to
			read for a state the other nine already say.
		-->
		<div class="areas" role="group" aria-label="Oblast">
			{#each DREAM_CATEGORIES as value (value)}
				<button
					type="button"
					class="chip chip--soft"
					class:chip--on={category === value}
					aria-pressed={category === value}
					disabled={busy}
					onclick={() => (category = category === value ? null : value)}
				>
					{CATEGORY_LABEL[value]}
				</button>
			{/each}
		</div>
	</div>

	<label class="field">
		<span class="field__label">Kdy <span>nepovinné</span></span>
		<input
			class="field__input"
			type="text"
			inputmode="numeric"
			bind:value={year}
			placeholder="2030"
			autocomplete="off"
			disabled={busy}
		/>
	</label>

	{#if error || problem}
		<p class="error-text" role="alert">{error || problem}</p>
	{:else if locked}
		<p class="hint">{locked}</p>
	{/if}

	<div class="actions actions--fill">
		<button
			type="submit"
			class="btn {accent ? 'btn--accent' : 'btn--primary'}"
			disabled={busy || !!locked || !title.trim()}
		>
			{busy ? 'Ukládám…' : submitLabel}
		</button>
	</div>
</form>

<style>
	/* Nine chips wrap rather than scroll: in a form they are a set to read
	   through once, not a rail to swipe along. */
	.areas {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
	}
</style>
