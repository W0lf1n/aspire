/**
 * Moving a dream to another line of the Seznam (D79).
 *
 * There are two ways to do it on the screen — drag the row, or type over its
 * number — and they are the same operation: this dream, to that line, and
 * everything between where it was and where it is now moves over by one. Make
 * the third line the first, and what was first is second and what was second
 * is third.
 *
 * It is worked out here as well as on the server (`DreamService.PlaceAsync`)
 * so the row moves under the finger rather than after a round trip, and both
 * number the board the same way: whole numbers from nought, every time, so an
 * order never has gaps to drift in.
 */

import type { Dream } from '@aspire/contracts';
import { listOrder } from './board';

/**
 * The board with one dream moved to `place`, counted from one, and every
 * `sortOrder` counted off again from nought. A place past either end is the
 * end. A dream that is not on the board changes nothing but the numbering,
 * which is what the server does with one it cannot find among the rows.
 */
export function placed(dreams: Dream[], id: string, place: number): Dream[] {
	const rows = listOrder(dreams);
	const from = rows.findIndex((dream) => dream.id === id);

	if (from >= 0) {
		const [moving] = rows.splice(from, 1);
		rows.splice(clamp(Math.trunc(place) - 1, 0, rows.length), 0, moving);
	}

	return rows.map((dream, index) =>
		dream.sortOrder === index ? dream : { ...dream, sortOrder: index }
	);
}

/**
 * The place to ask the server for, given the line the person dropped it on.
 *
 * The two are the same number except for six seconds at a time: a dream
 * inside its undo window is off the screen and still on the server (D65), so
 * the screen's third line can be the server's fourth. Rather than count what
 * is hidden, this finds the dream that will be *under* the moved one on the
 * screen and asks for its line on the server — the neighbour is the same
 * dream in both lists, whatever is hiding between the rows.
 *
 * `seen` is the list as the screen shows it; `held` is everything the server
 * still has.
 */
export function serverPlace(seen: Dream[], held: Dream[], id: string, place: number): number {
	const rows = seen.filter((dream) => dream.id !== id);
	const all = listOrder(held).filter((dream) => dream.id !== id);

	const under = rows[clamp(Math.trunc(place) - 1, 0, rows.length)];
	if (!under) return all.length + 1;

	const at = all.findIndex((dream) => dream.id === under.id);
	return (at < 0 ? all.length : at) + 1;
}

/**
 * What was typed over a line's number, as a place — or null when it is not
 * one, and the row should stay where it is.
 *
 * Forgiving about everything but meaning: spaces, a trailing dot the way
 * Czech writes an ordinal („3.“), a number past the end. Nothing, a word, a
 * zero and a minus are not places, and the same number it already has is not
 * a move.
 */
export function parsePlace(typed: string, current: number, count: number): number | null {
	const text = typed.trim().replace(/\.$/, '');
	if (!/^\d{1,6}$/.test(text)) return null;

	if (Number(text) < 1) return null;

	const place = clamp(Number(text), 1, Math.max(1, count));
	return place === current ? null : place;
}

function clamp(value: number, low: number, high: number): number {
	return Math.min(high, Math.max(low, value));
}
