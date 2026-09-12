/**
 * The API's wire types, as TypeScript.
 *
 * This package is the one place the client and the server agree on a shape.
 * The client imports it directly; `apps/api` mirrors it in C# in
 * `Contracts.cs`. A field renamed on one side breaks the other's build rather
 * than silently dropping data. A type here is a wire format, and a wire
 * format is allowed to be dull.
 */

// ── GET /api/v1/health ──────────────────────────────────────────────────────

export interface HealthResponse {
	ok: boolean;
	version: string;
}

// ── POST /api/v1/pair ───────────────────────────────────────────────────────

export interface PairRequest {
	/** The code set on the server, typed once on this device. */
	code: string;
	/** Free text, so a device list is readable. */
	deviceName: string;
}

export interface PairResponse {
	deviceId: string;
	/** Bearer token. Device-bound, no expiry. */
	token: string;
}

// ── GET /api/v1/dreams ──────────────────────────────────────────────────────

export const DREAM_STATUSES = ['dreaming', 'in-progress', 'achieved'] as const;

export type DreamStatus = (typeof DREAM_STATUSES)[number];

/**
 * The three areas of PLAN.md §3.1: whether the dream is something to want,
 * something to be, or something to do (D43, in place of D32's nine). A dream
 * may belong to none of them.
 */
export const DREAM_CATEGORIES = ['want', 'be', 'do'] as const;

export type DreamCategory = (typeof DREAM_CATEGORIES)[number];

/** One dream as the board reads it. Images arrive with M1. */
export interface Dream {
	id: string;
	title: string;
	why: string;
	/**
	 * The dream said as though it were already true, in the person's own
	 * words, or empty. The reel shows it in the why's place when there is
	 * one: the why explains the dream, this one states it (D27).
	 */
	affirmation: string;
	status: DreamStatus;
	/** One of the three, or null. */
	category: DreamCategory | null;
	sortOrder: number;
	targetYear: number | null;
	/** Taps on the heart, counted. Within the board, never across. */
	likes: number;
	/** ISO datetime, or null while the dream is still a dream. */
	achievedAt: string | null;
	/**
	 * ISO datetime of the day this dream was the board's first tile, or null
	 * while it has never been one. The daily pick reads it: least recently
	 * shown first, so the board opens on a different dream each day.
	 */
	lastShownAt: string | null;
	/** ISO datetime. */
	createdAt: string;
	/** Both kinds, in board order; `photosOf` picks the one a screen wants. */
	images: DreamImage[];
}

export const DREAM_IMAGE_KINDS = ['dreamt', 'achieved'] as const;

/**
 * What a photograph is of: the dream, or the dream come true. The board and
 * the reel show the dreamt one; the Síň slávy stands the two side by side,
 * which is the whole of the proof that it happened (D28).
 */
export type DreamImageKind = (typeof DREAM_IMAGE_KINDS)[number];

/**
 * One photograph, as URLs under `/media/`. `ready` is false for the moment
 * between the upload and the resize; the board shows the sky and asks again.
 */
export interface DreamImage {
	id: string;
	sortOrder: number;
	kind: DreamImageKind;
	width: number;
	height: number;
	ready: boolean;
	thumbUrl: string;
	screenUrl: string;
	fullUrl: string;
}

// ── GET/PUT /api/v1/nudge ─────────────────────────────────────

export const NUDGE_MODES = ['off', 'daily', 'weekdays'] as const;

/** Off, every day, or only on working days (PLAN.md §3.6). */
export type NudgeMode = (typeof NUDGE_MODES)[number];

/** A device's standing nudge, as it reads it back. No keys come out. */
export interface NudgeSettings {
	mode: NudgeMode;
	/** Minutes past midnight where the device is. */
	atMinutes: number;
}

/** What a device sends to be nudged: the browser's subscription, and when. */
export interface NudgeInput extends NudgeSettings {
	endpoint: string;
	p256dh: string;
	auth: string;
	/**
	 * Minutes ahead of UTC. An offset rather than a zone name, because the
	 * server has no zone database; the device sends it again every time the
	 * app opens, so it is right the morning after a clock change (D34).
	 */
	utcOffsetMinutes: number;
}

/**
 * Where a device is now, sent every time the app opens (D34, D51).
 *
 * Its own verb rather than writing the whole subscription again: a `PUT` with
 * no mode and no time would write „daily at seven“ over whatever the person
 * chose, and opening the app must never move the hour. This carries the
 * offset and nothing else.
 */
export interface NudgeOffsetInput {
	endpoint: string;
	/** Minutes ahead of UTC, the opposite sign to `getTimezoneOffset`. */
	utcOffsetMinutes: number;
}

/** The VAPID public key, or empty when the server cannot send at all. */
export interface PushKeyResponse {
	publicKey: string;
}

// ── POST /api/v1/dreams · PUT /api/v1/dreams/{id} ──────────────────────────

/** What the client sends to make or change a dream. */
export interface DreamInput {
	title: string;
	why: string;
	affirmation: string;
	status: DreamStatus;
	category: DreamCategory | null;
	targetYear: number | null;
}
