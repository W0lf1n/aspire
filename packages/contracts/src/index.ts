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
	/**
	 * Board order, lowest first: the order the Seznam is in, which is the
	 * person's own (D79). A key to sort by and not a line number — the screen
	 * counts the lines — so it may be negative and may have gaps.
	 */
	sortOrder: number;
	/**
	 * Where this dream stands on Teď, the second reel, or null when it is not
	 * on it (D53). An ordering key rather than a position: the screen numbers
	 * the ten by their place in the list, so a gap costs nothing.
	 */
	focusRank: number | null;
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
	/**
	 * ISO datetime of the last time the dream itself was changed: its words,
	 * its status, its area, its year or one of its photographs (D77). A heart,
	 * the day's „shown“ stamp and a place in an order are about the board and
	 * do not move it, so the date means „I last worked on this one then“.
	 */
	updatedAt: string;
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
	/**
	 * Where the photograph is looked at, and how close (D54). `focusX` and
	 * `focusY` are `object-position` percentages, 0 to 1; `zoom` is 1 for as
	 * much of the picture as the frame can hold. The middle and all of it —
	 * 0.5, 0.5, 1 — is what every photograph is until somebody moves it.
	 *
	 * Metadata rather than a cropped file, so every shape that shows this
	 * photograph — a screen, a 4:5 print, a 40 px circle, a collage cell —
	 * crops to the same point rather than to one crop made for one of them.
	 */
	focusX: number;
	focusY: number;
	zoom: number;
	thumbUrl: string;
	screenUrl: string;
	/**
	 * The 2048 file, named for what it is to a phone: the rung a 2× or 3×
	 * screen reads on the reel, where 1280 would be drawn at twice its
	 * pixels (D62). `reelUrl` in `dreams/photos.ts` chooses; no screen
	 * reads this field by hand.
	 */
	largeUrl: string;
}

/**
 * Where a photograph is looked at, as it is sent (D54). Everything optional:
 * a field left out keeps what the photograph already has.
 */
export interface FocalInput {
	focusX?: number;
	focusY?: number;
	zoom?: number;
}

// ── GET/PUT /api/v1/nudge ─────────────────────────────────────

export const NUDGE_MODES = ['off', 'daily', 'weekdays'] as const;

/** Off, every day, or only on working days (PLAN.md §3.6). */
export type NudgeMode = (typeof NUDGE_MODES)[number];

/** A device's standing nudge, as it reads it back. No keys come out. */
export interface NudgeSettings {
	mode: NudgeMode;
	/**
	 * When the device wants to hear, as minutes past midnight where it is:
	 * one to `MAX_NUDGE_TIMES` of them, in order and with no repeats (D72).
	 * The server tidies whatever it is sent, so a screen may send them in any
	 * order and read them back sorted.
	 */
	times: number[];
}

/**
 * How many reminders a day a device may ask for (D72). Five, because what is
 * being built is a habit and not an alarm clock, and past about five a
 * notification stops being noticed and starts being dismissed.
 */
export const MAX_NUDGE_TIMES = 5;

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

// ── GET · POST · DELETE /api/v1/board/link ─────────────────────────────────

/**
 * The board's lock-screen link, or none (D60).
 *
 * The path rather than the whole URL: the client is served from this origin
 * and knows it for certain, while the server behind nginx would be reading its
 * own scheme out of a header somebody else can set. The screen puts the two
 * together and adds this phone's canvas and offset, because the link is static
 * and whatever it carries is what the morning automation will ask for.
 */
export interface LinkResponse {
	path: string | null;
}

// ── GET /api/v1/board ───────────────────────────────────────────────────────

/**
 * The board as a whole: how many photographs it holds on the server and
 * what they weigh, against the ceiling (D64). Nastavení → Stahování shows
 * it, so the number is seen long before an upload is refused with a
 * sentence.
 */
export interface BoardResponse {
	name: string;
	photographs: number;
	bytes: number;
	bytesLimit: number;
}

// ── PUT /api/v1/focus ───────────────────────────────────────────────────────

/**
 * The whole of Teď in one request: these dreams, in this order, and nothing
 * else on it (D53). An empty list is a Teď emptied on purpose.
 */
export interface FocusInput {
	dreamIds: string[];
}

// ── PUT /api/v1/dreams/{id}/place ───────────────────────────────────────────

/**
 * Which line of the Seznam a dream is moved to, counted from one (D79). One
 * dream and one number, because both ways of moving a dream — dragging it and
 * typing over its number — are exactly that. A place past the end is the end.
 */
export interface PlaceInput {
	place: number;
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
