/**
 * Smazat, as every screen that has one does it (D65): the dream is hidden at
 * once, the request is held for as long as the toast stands, and „Vrátit“
 * cancels it before it is ever sent.
 *
 * The dream's own screen was the only place a dream could be let go of until
 * the Seznam's rows got a Smazat behind a swipe (D78). Two screens with the
 * same six seconds, the same two sentences and the same way back are one
 * function — and the next list that grows a Smazat gets the undo for free
 * rather than by remembering.
 */

import type { Dream } from '@aspire/contracts';
import { deleteDream } from '$lib/api/client';
import { describeError } from '$lib/api/errors';
import { toast } from '$lib/ui/toast.svelte';
import { UNDO_MS, deleting } from './deleting.svelte';

export function letGo(dream: Pick<Dream, 'id' | 'title'>): void {
	deleting.hold(dream.id, async () => {
		try {
			await deleteDream(dream.id, true);
		} catch (e) {
			// The server still has it, so hiding it would be a lie.
			deleting.keep(dream.id);
			toast.show(describeError(e));
		}
	});

	toast.show(`„${dream.title}“ je pryč`, {
		ms: UNDO_MS,
		action: {
			label: 'Vrátit',
			run: () => {
				deleting.keep(dream.id);
				toast.show(`„${dream.title}“ je zpátky`);
			}
		}
	});
}
