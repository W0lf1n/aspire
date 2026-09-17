<script lang="ts">
	/**
	 * One line of the Seznam: its number, the dream, and three things the line
	 * can do that a plain row could not.
	 *
	 * **Swiped to the left** it slides aside to show a tray — the hearts and
	 * their count, Sdílet, Smazat (D78). The count was taken off the reel's
	 * tile because a number on a photograph is a score (D58); a line in an
	 * inventory is where a count belongs, and even here it is behind a swipe
	 * rather than in the column. `ui/swipe.ts` is the arithmetic.
	 *
	 * **Dragged** — by the grip at once, or by the line itself after a long
	 * press — it goes to another line (D79). The row only says that it was
	 * grabbed: the drag belongs to the screen, because every other row has to
	 * step aside for it.
	 *
	 * **Its number is a field.** Tapped, it can be typed over, and the dream
	 * goes to that line — which is the same move as a drag, for a list too long
	 * to drag across and for anybody with no pointer to drag with. That is why
	 * the grip is hidden from a screen reader and out of the tab order: a
	 * button that does nothing when it is pressed is worse than no button, and
	 * the number beside it is the same move said in a way a keyboard can say.
	 *
	 * The dream is a link and the rest are buttons beside it, not inside it: a
	 * control inside an anchor is not valid markup, and a field inside one
	 * cannot be typed in on a phone without following the link.
	 */
	import { resolve } from '$app/paths';
	import type { Dream } from '@aspire/contracts';
	import { parsePlace } from '$lib/dreams/order';
	import { photoOf, photoStyle } from '$lib/dreams/photos';
	import { listLine } from '$lib/dreams/rules';
	import { writes } from '$lib/offline/writes.svelte';
	import Icon from './Icon.svelte';
	import { gestureOf, slidTo, staysOpen } from './swipe';

	interface Props {
		dream: Dream;
		/** Which line this is, counted down the whole list from one (D48). */
		place: number;
		/** How many lines the whole list has, which is as far as a number goes. */
		count: number;
		/** Whether the line can be moved at all: not while the list is narrowed, not offline. */
		movable: boolean;
		/** Whether this line's tray is the one that is open. One at a time; the screen says which. */
		open: boolean;
		/** Being dragged, and how far from its own line, in pixels. */
		lifted?: boolean;
		/** Stepping aside for the row that is, in pixels. */
		aside?: number;
		onopen: (open: boolean) => void;
		onlike: (dream: Dream) => void;
		onshare: (dream: Dream) => void;
		onremove: (dream: Dream) => void;
		onplace: (dream: Dream, place: number) => void;
		ongrab: (dream: Dream, at: { clientY: number }) => void;
	}

	let {
		dream,
		place,
		count,
		movable,
		open,
		lifted = false,
		aside = 0,
		onopen,
		onlike,
		onshare,
		onremove,
		onplace,
		ongrab
	}: Props = $props();

	const photo = $derived(photoOf(dream, 'dreamt'));

	// ── the swipe ───────────────────────────────────────────────────────────

	/** How long a finger rests on the line before the line comes away with it. */
	const HOLD_MS = 420;

	let tray: HTMLElement | null = $state(null);

	/** Where the face sits, in pixels: 0 closed, minus the tray's width open. */
	let at = $state(0);
	let sliding = $state(false);

	/** The gesture in progress, from the finger going down to what it turned out to be. */
	let from: { x: number; y: number; t: number; at: number } | null = null;
	let kind: 'swipe' | 'scroll' | 'drag' | null = null;
	let hold: ReturnType<typeof setTimeout> | undefined;

	/** A gesture has just ended on the link, and the click that follows it is not a tap. */
	let spent = false;

	const width = () => tray?.offsetWidth ?? 0;

	// The screen decides which tray is open, so that one opening shuts the last.
	$effect(() => {
		if (!sliding) at = open ? -width() : 0;
	});

	function down(event: PointerEvent) {
		if (event.button > 0 || editing) return;
		// The grip and the number are controls of their own.
		if ((event.target as HTMLElement).closest('[data-own]')) return;

		from = { x: event.clientX, y: event.clientY, t: event.timeStamp, at };
		kind = null;
		spent = false;

		clearTimeout(hold);
		if (movable && !open) {
			const where = { clientY: event.clientY };
			hold = setTimeout(() => {
				if (!from || kind !== null) return;
				kind = 'drag';
				spent = true;
				navigator.vibrate?.(10);
				ongrab(dream, where);
			}, HOLD_MS);
		}
	}

	function move(event: PointerEvent) {
		if (!from || kind === 'drag' || kind === 'scroll') return;

		const dx = event.clientX - from.x;
		const dy = event.clientY - from.y;

		if (kind === null) {
			kind = gestureOf(dx, dy);
			if (kind === null) return;

			clearTimeout(hold);
			if (kind !== 'swipe') return;

			sliding = true;
			try {
				(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
			} catch {
				/* no capture; the swipe lasts as long as the finger stays on the line */
			}
		}

		at = slidTo(from.at, dx, width());
	}

	function up(event: PointerEvent) {
		clearTimeout(hold);
		const began = from;
		from = null;
		if (!began || kind !== 'swipe') return;

		sliding = false;
		spent = true;

		const speed = (event.clientX - began.x) / Math.max(1, event.timeStamp - began.t);
		const stays = staysOpen(at, width(), speed);
		at = stays ? -width() : 0;
		if (stays !== open) onopen(stays);
	}

	/**
	 * The click that ends a swipe or a long press lands on the link, and must
	 * not follow it. A tap on a line whose tray is open shuts the tray, which
	 * is what a tap anywhere else does too.
	 */
	function tapped(event: MouseEvent) {
		if (!spent && !open) return;
		if ((event.target as HTMLElement).closest('[data-own]')) return;

		event.preventDefault();
		event.stopPropagation();
		spent = false;
		if (open) onopen(false);
	}

	// ── the number ──────────────────────────────────────────────────────────

	let editing = $state(false);
	let typed = $state('');

	function edit() {
		typed = String(place);
		editing = true;
	}

	function commit() {
		if (!editing) return;
		editing = false;

		const to = parsePlace(typed, place, count);
		if (to !== null) onplace(dream, to);
	}

	function keyed(event: KeyboardEvent) {
		if (event.key === 'Enter') {
			event.preventDefault();
			commit();
		} else if (event.key === 'Escape') {
			// Its own escape: inside a sheet this would otherwise close the sheet.
			event.stopPropagation();
			editing = false;
		}
	}

	/** The field takes the focus and offers the old number to be typed over. */
	function ready(node: HTMLInputElement) {
		node.focus();
		node.select();
	}

	function act(run: (dream: Dream) => void) {
		onopen(false);
		run(dream);
	}
</script>

<div
	class="swipe"
	class:swipe--lifted={lifted}
	data-row={dream.id}
	style:translate={aside === 0 ? null : `0 ${aside}px`}
>
	<!--
		Behind the face, and out of reach until it is shown: a tray nobody can
		see must not be something a keyboard can land on.
	-->
	<div class="swipe__tray" bind:this={tray} inert={!open}>
		<button
			type="button"
			class="swipe__act swipe__act--fuel"
			onclick={() => onlike(dream)}
			use:writes
			aria-label={`Palivo: ${dream.likes}`}
		>
			<Icon name="heart" size={20} stroke={2} />
			<span>{dream.likes}</span>
		</button>
		<button type="button" class="swipe__act" onclick={() => act(onshare)}>
			<Icon name="link" size={20} stroke={1.8} />
			<span>Sdílet</span>
		</button>
		<button
			type="button"
			class="swipe__act swipe__act--danger"
			onclick={() => act(onremove)}
			use:writes
		>
			<Icon name="trash" size={20} stroke={1.8} />
			<span>Smazat</span>
		</button>
	</div>

	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div
		class="row swipe__face"
		class:swipe__face--held={sliding}
		style:translate={at === 0 ? null : `${at}px 0`}
		onpointerdown={down}
		onpointermove={move}
		onpointerup={up}
		onpointercancel={up}
		onclickcapture={tapped}
		oncontextmenu={(event) => {
			// A long press on a link is the phone's own menu; here it is a drag.
			if (from) event.preventDefault();
		}}
	>
		{#if editing}
			<input
				class="row__no row__no--field"
				data-own
				type="text"
				inputmode="numeric"
				enterkeyhint="done"
				autocomplete="off"
				maxlength="4"
				bind:value={typed}
				onblur={commit}
				onkeydown={keyed}
				use:ready
				aria-label={`Nové místo pro „${dream.title}“, 1 až ${count}`}
			/>
		{:else if movable}
			<button
				type="button"
				class="row__no row__no--press"
				data-own
				onclick={edit}
				aria-label={`${place}. místo. Změnit`}
			>
				{place}
			</button>
		{:else}
			<span class="row__no">{place}</span>
		{/if}

		<a class="row__link" href={resolve('/sen/[id]', { id: dream.id })} draggable="false">
			{#if photo}
				<img
					class="circle row__shot"
					src={photo.thumbUrl}
					alt=""
					style={photoStyle(photo)}
					draggable="false"
					loading="lazy"
					decoding="async"
				/>
			{:else}
				<!-- No photograph yet: the sky stands in for it, as it does on a tile. -->
				<span class="circle circle--sky" aria-hidden="true"></span>
			{/if}
			<span class="row__body">
				<span class="row__title">{dream.title}</span>
				<span class="row__sub">{listLine(dream)}</span>
			</span>
		</a>

		{#if movable}
			<button
				type="button"
				class="row__grip"
				data-own
				tabindex="-1"
				onpointerdown={(event) => {
					if (event.button > 0) return;
					onopen(false);
					ongrab(dream, { clientY: event.clientY });
				}}
				aria-hidden="true"
			>
				<Icon name="grip" size={18} stroke={2} />
			</button>
		{:else}
			<span class="card__go"><Icon name="chevron-right" size={18} /></span>
		{/if}
	</div>
</div>
