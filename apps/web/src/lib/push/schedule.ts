/**
 * The nudge's time, as the screen reads and writes it, and whether there is
 * anything to offer at all (PLAN.md §3.6).
 *
 * The server keeps the time as minutes past local midnight, because that is
 * what a schedule with no timezone database can hold (D34); an `<input
 * type="time">` speaks `HH:MM`. These turn one into the other, and they have
 * the test — a rule gets one before it gets a screen. `nudge.ts` beside this
 * is the half that has to talk to `Notification`, `PushManager` and the API.
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

/** What a device has found out about whether the nudge can be offered. */
export interface NudgeReach {
	/** `Notification`, a service worker and a `PushManager`, all three. */
	browser: boolean;
	/** What this origin has already been asked, or `default` when it has not. */
	permission: NotificationPermission;
	/**
	 * Whether the server has a VAPID pair to send with — `null` when nobody
	 * could ask it, which is not the same answer as no.
	 */
	sends: boolean | null;
}

/**
 * Why the morning nudge cannot be offered here, or null when it can.
 *
 * The screen asks this before it draws a switch, the way Stahování asks
 * before it offers „na wifi“ (D39): a control that cannot work is worse than
 * no control, and it is worse again when tapping it spends the one
 * notification prompt a browser will ever give you. Without this the server
 * half only ever surfaced as a toast — after permission had been asked for,
 * for a server that had nothing to send.
 *
 * In that order, because each one makes the next beside the point: a browser
 * that cannot do notifications makes the server's keys irrelevant, and a
 * server with no keys makes a permission the person could fix irrelevant
 * too. A server nobody could reach is not an answer, so it is not a reason:
 * offline is a state this screen already has a sentence for.
 */
export function outOfReach(reach: NudgeReach): string | null {
	if (!reach.browser) {
		return 'Tenhle prohlížeč upozornění neumí. Na iPhonu je přidej na plochu.';
	}

	if (reach.sends === false) {
		return 'Server zatím upozornění posílat neumí.';
	}

	if (reach.permission === 'denied') {
		return 'Upozornění máš pro tuhle stránku zakázaná v nastavení prohlížeče.';
	}

	return null;
}
