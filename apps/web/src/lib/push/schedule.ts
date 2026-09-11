/**
 * The nudge's time, as the screen reads and writes it (PLAN.md §3.6).
 *
 * The server keeps it as minutes past local midnight, because that is what
 * a schedule with no timezone database can hold (D34); an `<input
 * type="time">` speaks `HH:MM`. These two turn one into the other, and they
 * have the test — a rule gets one before it gets a screen.
 */

import type { NudgeMode } from '@aspire/contracts';

/** 07:00, PLAN.md §3.6's default. */
export const DEFAULT_AT_MINUTES = 7 * 60;

/** What the segment says: the three states, in Czech. */
export const MODE_LABEL: Record<NudgeMode, string> = {
	off: 'Vypnuto',
	daily: 'Každý den',
	weekdays: 'Ve všední dny'
};

/** Minutes past midnight as `HH:MM`, which is what a time field wants. */
export function toClock(atMinutes: number): string {
	const minutes = clamp(atMinutes);
	const hour = Math.floor(minutes / 60);
	return `${pad(hour)}:${pad(minutes % 60)}`;
}

/**
 * `HH:MM` back to minutes. Anything that is not a time of day — a cleared
 * field, a browser that gave something else — falls back to seven, because
 * a nudge at a time nobody chose is better than a screen that cannot save.
 */
export function fromClock(clock: string): number {
	const match = /^(\d{1,2}):(\d{2})$/.exec(clock.trim());
	if (!match) return DEFAULT_AT_MINUTES;

	// Each half is checked on its own: `12:60` is not a time, and adding it
	// up first would quietly make it one o'clock.
	const hours = Number(match[1]);
	const minutes = Number(match[2]);
	if (hours > 23 || minutes > 59) return DEFAULT_AT_MINUTES;

	return hours * 60 + minutes;
}

function clamp(minutes: number): number {
	if (!Number.isFinite(minutes)) return DEFAULT_AT_MINUTES;
	return Math.min(24 * 60 - 1, Math.max(0, Math.round(minutes)));
}

function pad(value: number): string {
	return String(value).padStart(2, '0');
}
