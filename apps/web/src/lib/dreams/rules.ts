/**
 * What a dream may be, as the form checks it before the server does. The
 * server's `DreamService.Problem` says the same things in the same order;
 * these exist so the sentence appears on the keystroke, not on the round trip.
 */

import type { Dream, DreamCategory, DreamInput, DreamStatus } from '@aspire/contracts';

export const TITLE_MAX = 120;
export const WHY_MAX = 500;
export const AFFIRMATION_MAX = 120;
export const YEAR_MIN = 2000;
export const YEAR_MAX = 2100;

/** The segment's labels: what the person is doing about the dream. */
export const STATUS_LABEL: Record<DreamStatus, string> = {
	dreaming: 'Sním',
	'in-progress': 'Plním',
	achieved: 'Splněno'
};

/** The badge's text: the same three, as a state rather than a choice. */
export const STATUS_BADGE: Record<DreamStatus, string> = {
	dreaming: 'sním',
	'in-progress': 'plním',
	achieved: 'splněno'
};

/** The three areas, in Czech. What the dream is: want, be, do (D43). */
export const CATEGORY_LABEL: Record<DreamCategory, string> = {
	want: 'Chtít',
	be: 'Být',
	do: 'Dělat'
};

/**
 * How many dreams can be on Teď at once (D53). Mirrors `Dream.FocusMax`.
 *
 * Ten is what makes the second reel worth having: a list you reach the end
 * of. The client knows the number so the pill can say so on the tap rather
 * than after a round trip, and the server enforces it.
 */
export const FOCUS_MAX = 10;

/** What Teď is called where a dream says what it is: in the list, and in a search. */
export const FOCUS_BADGE = 'teď';

/**
 * The line under a dream's title in the Seznam: whether it is on Teď, the
 * state it is in, the area it is in, the year it is for — the sheet's own
 * questions, answered back in the order it asked them, with the one thing
 * the sheet never asks in front.
 *
 * „teď“ leads because it is the only part of the line that says what the
 * person is doing about the dream this month rather than what the dream is,
 * and the Seznam is the one screen that shows the ten among everything else
 * (D53).
 *
 * A field left empty says nothing rather than saying „—“: a list is read
 * down the left edge, and a column of dashes is a column of noise. The state
 * is always there, so the line never is empty.
 */
export function listLine(
	dream: Pick<Dream, 'status' | 'category' | 'targetYear' | 'focusRank'>
): string {
	return [
		dream.focusRank === null ? '' : FOCUS_BADGE,
		STATUS_BADGE[dream.status],
		dream.category === null ? '' : CATEGORY_LABEL[dream.category],
		dream.targetYear === null ? '' : String(dream.targetYear)
	]
		.filter((part) => part.length > 0)
		.join(' · ');
}

export const STATUS_CLASS: Record<DreamStatus, string> = {
	dreaming: 'badge--dreaming',
	'in-progress': 'badge--progress',
	achieved: 'badge--achieved'
};

/** The form's fields, as typed. The year is text until it is checked. */
export interface DreamFields {
	title: string;
	why: string;
	affirmation: string;
	status: DreamStatus;
	/** One of the three, or none. */
	category: DreamCategory | null;
	year: string;
}

/** The fields as the server wants them, or the sentence that stops them. */
export function toInput(fields: DreamFields): { input: DreamInput } | { problem: string } {
	const title = fields.title.trim();
	if (title.length === 0) return { problem: 'Napiš název.' };
	if (title.length > TITLE_MAX) return { problem: `Název má nejvýš ${TITLE_MAX} znaků.` };

	const why = fields.why.trim();
	if (why.length > WHY_MAX) return { problem: `Proč má nejvýš ${WHY_MAX} znaků.` };

	const affirmation = fields.affirmation.trim();
	if (affirmation.length > AFFIRMATION_MAX) {
		return { problem: `Afirmace má nejvýš ${AFFIRMATION_MAX} znaků.` };
	}

	const yearText = fields.year.trim();
	let targetYear: number | null = null;
	if (yearText.length > 0) {
		if (!/^\d{4}$/.test(yearText)) return { problem: 'Rok napiš čtyřmi číslicemi.' };
		targetYear = Number(yearText);
		if (targetYear < YEAR_MIN || targetYear > YEAR_MAX) {
			return { problem: `Rok napiš mezi ${YEAR_MIN} a ${YEAR_MAX}.` };
		}
	}

	return {
		input: { title, why, affirmation, status: fields.status, category: fields.category, targetYear }
	};
}

/** A dream's saved values back into the form. */
export function toFields(
	input: Pick<DreamInput, 'title' | 'why' | 'affirmation' | 'status' | 'category' | 'targetYear'>
): DreamFields {
	return {
		title: input.title,
		why: input.why,
		affirmation: input.affirmation,
		status: input.status,
		category: input.category,
		year: input.targetYear === null ? '' : String(input.targetYear)
	};
}
