/**
 * How much of the board this device keeps for when there is no signal.
 *
 * A board is meant to reach a hundred dreams (D30) and a screen-size
 * photograph is a couple of hundred kilobytes, so the whole board is tens of
 * megabytes. That is nothing for a phone to *store* — a photo library is
 * gigabytes — and it is real money to *fetch* on a metered plan, in the
 * minute somebody is looking at the first tile. So the question is never how
 * much room there is; it is which connection the megabytes arrive over, and
 * that is what this chooses (D39).
 *
 * Three answers, because there are exactly three honest ones: what you swipe
 * past, the whole board when the connection is free, or the whole board
 * whatever the connection. Only the browser that knows what a connection
 * *is* can be offered the middle one.
 */

export type OfflinePolicy = 'window' | 'wifi' | 'all';

/** On the control, where three have to fit across a phone. */
export const POLICY_LABEL: Record<OfflinePolicy, string> = {
	window: 'Co prolistuješ',
	wifi: 'Na wifi',
	all: 'Vždy celá'
};

/** On the Nastavení hub, where there is a line to spend. */
export const POLICY_SUMMARY: Record<OfflinePolicy, string> = {
	window: 'jen co prolistuješ',
	wifi: 'celá nástěnka na wifi',
	all: 'vždy celá nástěnka'
};

export const OFFLINE_KEY = 'offline';

/**
 * How many photographs a windowing device keeps.
 *
 * The reel is shuffled on every open (D30), so the window is a different
 * handful every morning and a cache that only ever grew would arrive at the
 * whole board anyway — just slower, and in the order least likely to help.
 * Forty dreams' worth — the reel's picture and the thumb under it, two files
 * a dream (D63) — is on the order of fifteen megabytes: enough to hold
 * several mornings of swiping, few enough to stay well under the size at
 * which a browser starts choosing for itself what to evict.
 */
export const WINDOW_KEEP = 80;

/** What this device has been told, or nothing yet. */
export function readPolicy(): OfflinePolicy | null {
	if (typeof localStorage === 'undefined') return null;
	try {
		const value = localStorage.getItem(OFFLINE_KEY);
		return value === 'window' || value === 'wifi' || value === 'all' ? value : null;
	} catch {
		return null;
	}
}

export function writePolicy(policy: OfflinePolicy): void {
	try {
		localStorage.setItem(OFFLINE_KEY, policy);
	} catch {
		/* private mode: the choice lasts the session */
	}
}

/**
 * The policy in force: what was chosen, or the best default this browser can
 * actually keep.
 *
 * A browser that can see the connection defaults to the whole board on wifi,
 * which is the promise D24 made and costs nothing to keep. One that cannot
 * defaults to the window, because the alternative is spending somebody's
 * mobile data on a guess.
 */
export function effectivePolicy(stored: OfflinePolicy | null, detects: boolean): OfflinePolicy {
	if (stored !== null) return stored;
	return detects ? 'wifi' : 'window';
}

/**
 * Whether the whole board is worth fetching now.
 *
 * `metered` is the connection as `metered()` reports it, `null` when the
 * browser does not say. Unknown counts as metered: a wrong guess one way
 * costs a few megabytes of cache, and the other way costs somebody's data.
 */
export function wantsWholeBoard(policy: OfflinePolicy, metered: boolean | null): boolean {
	if (policy === 'all') return true;
	if (policy === 'window') return false;
	return metered === false;
}

/** The ceiling in photographs, or `null` when the board is its own ceiling. */
export function capFor(policy: OfflinePolicy): number | null {
	return policy === 'window' ? WINDOW_KEEP : null;
}

/**
 * The Network Information API, which is Chromium's and not Safari's.
 *
 * `type` is the only member that says what the connection *is*;
 * `effectiveType` is a speed estimate and cannot tell wifi from good
 * cellular, so it is read in one direction only — definitely slow means
 * treat it as metered, fast means nothing either way.
 */
interface NetworkInformation {
	saveData?: boolean;
	type?: string;
	effectiveType?: string;
}

function connection(): NetworkInformation | undefined {
	if (typeof navigator === 'undefined') return undefined;
	return (navigator as Navigator & { connection?: NetworkInformation }).connection;
}

/**
 * Whether the person has asked the browser to spend less — Save Data, where
 * the browser will say. The reel reads the smaller rung then (D62).
 */
export function savesData(): boolean {
	return connection()?.saveData === true;
}

/** Whether this browser can tell wifi from mobile data at all. */
export function detects(): boolean {
	return typeof connection()?.type === 'string';
}

/**
 * Whether the connection is one to be careful with: true for mobile data or
 * Save Data, false for wifi and ethernet, `null` where the browser will not
 * say — which on an iPhone is always.
 */
export function metered(): boolean | null {
	const link = connection();
	if (!link) return null;
	if (link.saveData === true) return true;

	switch (link.type) {
		case 'wifi':
		case 'ethernet':
			return false;
		case 'cellular':
		case 'none':
			return true;
	}

	const slow = link.effectiveType === 'slow-2g' || link.effectiveType === '2g';
	return slow ? true : null;
}

/**
 * What the app takes up on this phone, as the screen says it: the whole
 * origin, because that is the number somebody looking at their storage
 * settings would see. `null` where the browser does not keep a figure.
 */
export async function usage(): Promise<number | null> {
	if (typeof navigator === 'undefined' || !navigator.storage?.estimate) return null;
	try {
		const { usage: bytes } = await navigator.storage.estimate();
		return bytes ?? null;
	} catch {
		return null;
	}
}

const MEGABYTES = new Intl.NumberFormat('cs-CZ', { maximumFractionDigits: 1 });

/** `12,4 MB`. Under a tenth of one, the honest answer is that there is nothing. */
export function formatUsage(bytes: number): string {
	const mb = bytes / 1024 / 1024;
	return mb < 0.05 ? 'nic' : `${MEGABYTES.format(mb)} MB`;
}
