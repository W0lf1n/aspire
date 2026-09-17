import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { UNDO_MS, deleting } from './deleting.svelte';

/** Nothing here touches the network; `send` only records that it was called. */
function remover() {
	const sent: string[] = [];
	return {
		sent,
		for: (id: string) => () => {
			sent.push(id);
			return Promise.resolve();
		}
	};
}

beforeEach(() => {
	vi.useFakeTimers();
});

afterEach(async () => {
	// Whatever a test left held goes now, so the next one starts empty.
	await deleting.flush();
	for (const id of [...deleting.ids]) deleting.keep(id);
	vi.useRealTimers();
});

describe('deleting', () => {
	it('hides the dream at once and sends nothing yet', () => {
		const remove = remover();

		deleting.hold('a', remove.for('a'));

		expect(deleting.has('a')).toBe(true);
		expect(remove.sent).toEqual([]);
	});

	it('sends it when the window closes', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		await vi.advanceTimersByTimeAsync(UNDO_MS);

		expect(remove.sent).toEqual(['a']);
	});

	it('keeps it hidden after the request has gone', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		await vi.advanceTimersByTimeAsync(UNDO_MS);

		// A board fetched before the deletion landed still carries the dream,
		// and a tile that comes back for one frame reads as a failure.
		expect(deleting.has('a')).toBe(true);
	});

	it('knows which hidden dream the server still has', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		// Inside the window the server still counts it among the others (D79).
		expect(deleting.holds('a')).toBe(true);
		expect(deleting.holds('b')).toBe(false);

		await vi.advanceTimersByTimeAsync(UNDO_MS);

		// Sent: still hidden, and no longer anything the server has.
		expect(deleting.has('a')).toBe(true);
		expect(deleting.holds('a')).toBe(false);
	});

	it('sends nothing when it is put back inside the window', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		deleting.keep('a');
		await vi.advanceTimersByTimeAsync(UNDO_MS * 2);

		expect(deleting.has('a')).toBe(false);
		expect(remove.sent).toEqual([]);
	});

	it('puts one back that was already sent, for a request that failed', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));
		await vi.advanceTimersByTimeAsync(UNDO_MS);

		deleting.keep('a');

		expect(deleting.has('a')).toBe(false);
		expect(remove.sent).toEqual(['a']);
	});

	it('sends the first when a second deletion starts inside its window', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		await vi.advanceTimersByTimeAsync(UNDO_MS / 2);
		deleting.hold('b', remove.for('b'));

		expect(remove.sent).toEqual(['a']);
		expect(deleting.has('a')).toBe(true);
		expect(deleting.has('b')).toBe(true);

		await vi.advanceTimersByTimeAsync(UNDO_MS);
		expect(remove.sent).toEqual(['a', 'b']);
	});

	it('sends each request exactly once, however the window ended', async () => {
		const remove = remover();
		deleting.hold('a', remove.for('a'));

		await deleting.flush();
		await deleting.flush();
		await vi.advanceTimersByTimeAsync(UNDO_MS * 2);

		expect(remove.sent).toEqual(['a']);
	});

	it('leaves a dream nobody deleted alone', () => {
		expect(deleting.has('never-touched')).toBe(false);
	});
});
