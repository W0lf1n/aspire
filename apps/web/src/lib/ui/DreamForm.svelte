<script lang="ts">
	/**
	 * The dream's form: a title, the why, the affirmation, a status, an area,
	 * a year, and one pill.
	 * Přidat and Upravit are this card with a different pill; the photograph
	 * is the screen's own business, above or below it. The form checks the
	 * fields on the way out (`rules.ts`) and shows the server's sentence when
	 * the server disagrees.
	 *
	 * The sheet on Seznam is the same form with the status left out and a way
	 * back out beside the pill (D44): a dream written straight into a list is
	 * one you are only just having, and „Sním“ is the state it is in.
	 *
	 * Where the screen hands it the board, the title is checked against it as
	 * it is typed and a note says which dream it looks like. It is a note and
	 * never a stop: the pill stays live and says the same word it always
	 * said, because writing the same dream down twice is allowed — this is
	 * his list, and all the app has is the observation (D50).
	 */
	import type { Dream, DreamCategory, DreamInput, DreamStatus } from '@aspire/contracts';
	import { DREAM_CATEGORIES, DREAM_STATUSES } from '@aspire/contracts';
	import { similarDreams } from '$lib/dreams/duplicates';
	import {
		AFFIRMATION_MAX,
		CATEGORY_LABEL,
		STATUS_LABEL,
		TITLE_MAX,
		WHY_MAX,
		listLine,
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
		/**
		 * Whether the status is asked for. Off where the answer is always
		 * „sním“ — a dream being written for the first time.
		 */
		withStatus?: boolean;
		/**
		 * The dreams already written down, for the duplicate note. Empty —
		 * the default — is a form that does not check, which is what a screen
		 * with no board in its hands gets.
		 */
		existing?: Dream[];
		/** The dream being edited, so it is not a duplicate of itself. */
		exceptId?: string | null;
		onsubmit: (input: DreamInput) => void;
		/** The way out, where the form is in a sheet rather than on a screen. */
		oncancel?: () => void;
	}

	let {
		initial,
		submitLabel,
		accent = false,
		busy,
		error,
		locked = '',
		withStatus = true,
		existing = [],
		exceptId = null,
		onsubmit,
		oncancel
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

	/** What is already written down that this looks like, as it is typed. */
	const similar = $derived(
		existing.length === 0 ? [] : similarDreams({ title, affirmation }, existing, exceptId)
	);

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

	{#if similar.length > 0}
		<!--
			A live region rather than an alert: it appears while the title is
			being typed, so it has to be said politely, after the letter — and
			it is news, not a problem. Nothing below it changes.
		-->
		<div class="note" role="status">
			<p>
				{similar.length === 1 ? 'Podobný sen už v seznamu máš:' : 'Podobné sny už v seznamu máš:'}
			</p>
			<ul>
				{#each similar as dream (dream.id)}
					<li><strong>{dream.title}</strong> · {listLine(dream)}</li>
				{/each}
			</ul>
			<p>{initial ? 'Uložit ho můžeš i tak.' : 'Přidat ho můžeš i tak.'} Je to tvůj seznam.</p>
		</div>
	{/if}

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

	{#if withStatus}
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
	{/if}

	<div class="field">
		<span class="field__label">Oblast <span>nepovinné</span></span>
		<!--
			Three chips rather than a segmented pill, because a segment is a
			choice you have to make and this one is optional: pressing the chip
			already chosen takes the area off again, which is the whole of „no
			area“ — a fourth „žádná“ chip would be one more thing to read for a
			state the three already say.
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
		{#if oncancel}
			<button type="button" class="btn btn--quiet" disabled={busy} onclick={oncancel}>
				Zrušit
			</button>
		{/if}
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
	/* The chips wrap rather than scroll: in a form they are a set to read
	   through once, not a rail to swipe along. Three fit a row on any phone;
	   the wrap is what keeps them readable at the largest type sizes. */
	.areas {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
	}
</style>
