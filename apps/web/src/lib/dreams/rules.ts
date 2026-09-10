/**
 * What a dream may be, as the form checks it before the server does. The
 * server's `DreamService.Problem` says the same things in the same order;
 * these exist so the sentence appears on the keystroke, not on the round trip.
 */

import type { DreamInput, DreamStatus } from '@aspire/contracts';

export const TITLE_MAX = 120;
export const WHY_MAX = 500;
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

export const STATUS_CLASS: Record<DreamStatus, string> = {
	dreaming: 'badge--dreaming',
	'in-progress': 'badge--progress',
	achieved: 'badge--achieved'
};

/** The form's fields, as typed. The year is text until it is checked. */
export interface DreamFields {
	title: string;
	why: string;
	status: DreamStatus;
	year: string;
}

/** The fields as the server wants them, or the sentence that stops them. */
export function toInput(fields: DreamFields): { input: DreamInput } | { problem: string } {
	const title = fields.title.trim();
	if (title.length === 0) return { problem: 'Napiš název.' };
	if (title.length > TITLE_MAX) return { problem: `Název má nejvýš ${TITLE_MAX} znaků.` };

	const why = fields.why.trim();
	if (why.length > WHY_MAX) return { problem: `Proč má nejvýš ${WHY_MAX} znaků.` };

	const yearText = fields.year.trim();
	let targetYear: number | null = null;
	if (yearText.length > 0) {
		if (!/^\d{4}$/.test(yearText)) return { problem: 'Rok napiš čtyřmi číslicemi.' };
		targetYear = Number(yearText);
		if (targetYear < YEAR_MIN || targetYear > YEAR_MAX) {
			return { problem: `Rok napiš mezi ${YEAR_MIN} a ${YEAR_MAX}.` };
		}
	}

	return { input: { title, why, status: fields.status, targetYear } };
}

/** A dream's saved values back into the form. */
export function toFields(
	input: Pick<DreamInput, 'title' | 'why' | 'status' | 'targetYear'>
): DreamFields {
	return {
		title: input.title,
		why: input.why,
		status: input.status,
		year: input.targetYear === null ? '' : String(input.targetYear)
	};
}
