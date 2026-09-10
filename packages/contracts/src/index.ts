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
	/** In board order; the first is the one the tile shows. */
	images: DreamImage[];
}

/**
 * One photograph, as URLs under `/media/`. `ready` is false for the moment
 * between the upload and the resize; the board shows the sky and asks again.
 */
export interface DreamImage {
	id: string;
	sortOrder: number;
	width: number;
	height: number;
	ready: boolean;
	thumbUrl: string;
	screenUrl: string;
	fullUrl: string;
}

// ── POST /api/v1/dreams · PUT /api/v1/dreams/{id} ──────────────────────────

/** What the client sends to make or change a dream. */
export interface DreamInput {
	title: string;
	why: string;
	affirmation: string;
	status: DreamStatus;
	targetYear: number | null;
}
