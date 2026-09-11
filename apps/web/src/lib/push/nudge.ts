/**
 * The morning nudge from this side (PLAN.md §3.6): what the browser can do,
 * what it has agreed to, and turning it on or off.
 *
 * The rules that can be tested without a browser live in `schedule.ts`
 * beside this; what is here talks to `Notification`, `PushManager` and the
 * API, none of which exist in a test.
 */

import type { NudgeInput, NudgeMode, NudgeSettings } from '@aspire/contracts';
import { nudgeKey, readNudge, saveNudge } from '$lib/api/client';

/** Whether this browser can do notifications at all. */
export function supported(): boolean {
	return (
		typeof window !== 'undefined' &&
		'Notification' in window &&
		'serviceWorker' in navigator &&
		'PushManager' in window
	);
}

/**
 * Why the nudge cannot be offered here, or nothing when it can.
 *
 * On iOS a web app can only ask for notifications once it has been added to
 * the home screen (PLAN.md §7), and the sentence says so rather than
 * letting the person tap a switch that silently does nothing.
 */
export function unavailable(): string | null {
	if (!supported()) {
		return 'Tenhle prohlížeč upozornění neumí. Na iPhonu je přidej na plochu.';
	}

	if (Notification.permission === 'denied') {
		return 'Upozornění máš pro tuhle stránku zakázaná v nastavení prohlížeče.';
	}

	return null;
}

/** This device's subscription, or null when it has none. */
async function existing(): Promise<PushSubscription | null> {
	const registration = await navigator.serviceWorker.ready;
	return registration.pushManager.getSubscription();
}

/** What the server has for this device: off until it says otherwise. */
export async function current(): Promise<NudgeSettings> {
	if (!supported()) return { mode: 'off', atMinutes: 7 * 60 };

	const subscription = await existing();
	return readNudge(subscription?.endpoint ?? null);
}

/**
 * Turn it on, or move it. Asks the browser first — for permission, then for
 * a subscription — and only then tells the server, so a refusal never
 * leaves a row promising something the browser will not deliver.
 *
 * The offset goes with it every time, so a device that has moved or changed
 * season is right again from the next morning (D34).
 */
export async function turnOn(
	mode: Exclude<NudgeMode, 'off'>,
	atMinutes: number
): Promise<NudgeSettings> {
	const permission = await Notification.requestPermission();
	if (permission !== 'granted') throw new Error('denied');

	const registration = await navigator.serviceWorker.ready;
	const subscription =
		(await registration.pushManager.getSubscription()) ??
		(await registration.pushManager.subscribe({
			userVisibleOnly: true,
			applicationServerKey: await serverKey()
		}));

	return saveNudge(toInput(subscription, mode, atMinutes));
}

/**
 * Turn it off: the browser's subscription goes and so does the server's row
 * (D34). The server is told first, because a subscription dropped here that
 * the server still holds is a notification nobody can stop.
 */
export async function turnOff(): Promise<NudgeSettings> {
	const subscription = await existing();
	const settings = await saveNudge(toInput(subscription, 'off', 7 * 60));
	await subscription?.unsubscribe();
	return settings;
}

/** The subscription as the API wants it. */
function toInput(
	subscription: PushSubscription | null,
	mode: NudgeMode,
	atMinutes: number
): NudgeInput {
	return {
		endpoint: subscription?.endpoint ?? '',
		p256dh: keyOf(subscription, 'p256dh'),
		auth: keyOf(subscription, 'auth'),
		mode,
		atMinutes,
		// Minutes *ahead* of UTC, which is the opposite sign to the one
		// `getTimezoneOffset` gives.
		utcOffsetMinutes: -new Date().getTimezoneOffset()
	};
}

function keyOf(subscription: PushSubscription | null, name: 'p256dh' | 'auth'): string {
	const key = subscription?.getKey(name);
	return key ? base64Url(key) : '';
}

/**
 * The server's VAPID public key, as `PushManager.subscribe` wants it: the
 * raw 65 bytes, not the base64url they travel as. An `ArrayBuffer` rather
 * than a view, because `BufferSource` will not take a `Uint8Array` whose
 * buffer might be shared.
 */
async function serverKey(): Promise<ArrayBuffer> {
	const { publicKey } = await nudgeKey();
	if (!publicKey) throw new Error('unconfigured');

	const padded = publicKey.replace(/-/g, '+').replace(/_/g, '/');
	const binary = atob(padded.padEnd(Math.ceil(padded.length / 4) * 4, '='));
	const bytes = new Uint8Array(binary.length);
	for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
	return bytes.buffer;
}

function base64Url(buffer: ArrayBuffer): string {
	let binary = '';
	for (const byte of new Uint8Array(buffer)) binary += String.fromCharCode(byte);
	return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
