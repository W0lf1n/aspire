/**
 * Pulling a sheet down to put it away (D76) — Prosper's gesture, with
 * Prosper's numbers, so the two apps let go of a sheet at the same point.
 *
 * The arithmetic is here and the pointer events are in `Sheet.svelte`: how
 * far a finger's travel moves the panel, and whether where it was let go
 * counts as putting the sheet away. Two ways it does — far enough, or fast
 * enough — because a sheet dragged slowly to its halfway point and a sheet
 * flicked an inch are both somebody who is done with it.
 */

/** A pull counts once it has gone this share of the panel's own height… */
export const FAR_ENOUGH = 0.3;

/** …and never less than this, so a short sheet is not dismissed by a twitch. */
export const AT_LEAST = 72;

/** Pixels per millisecond. Past this a pull is a flick, however short. */
export const FLICK = 0.5;

/** A flick still has to have gone somewhere: a tap on the handle is not one. */
export const FLICK_FROM = 12;

/**
 * Where the panel sits for a finger that has travelled `dy` from where it
 * went down. Downward it follows the finger exactly; upward it gives a sixth,
 * which says „there is nothing up here“ without refusing to move at all.
 */
export function pulledBy(dy: number): number {
	if (!Number.isFinite(dy)) return 0;
	return dy >= 0 ? dy : dy / 6;
}

/**
 * Whether letting go here puts the sheet away.
 *
 * The speed is the whole pull over the whole gesture rather than the last few
 * samples: it is one subtraction, and a pull that started slow and ended fast
 * has also gone far.
 */
export function dismisses(pulled: number, elapsedMs: number, panelHeight: number): boolean {
	if (!(pulled > 0)) return false;

	const enough = Math.max(AT_LEAST, panelHeight * FAR_ENOUGH);
	if (pulled > enough) return true;

	const speed = pulled / Math.max(1, elapsedMs);
	return pulled > FLICK_FROM && speed > FLICK;
}
