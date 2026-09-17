<script lang="ts">
	/**
	 * Vyplnit · Celá, and the mats a photograph shown whole stands on (D81).
	 *
	 * One set of controls for both editors: a photograph on its own in
	 * `CropEditor`, and a collage's cell in `CollageEditor`, where a cell can
	 * now show all of its photograph too (D88). They sit in the editor's
	 * `.crop__top`, over the photograph, so they are glass — the segment with
	 * the board's lens (D74), and a pill of swatches whose chosen one wears a
	 * ring in the photograph's own ink rather than the interface's accent.
	 */
	import { PHOTO_MATS, type PhotoMat } from '@aspire/contracts';
	import { fitted, type Focal } from '$lib/images/focal';

	/** What each mat is called when it is read out; on the screen it is a colour. */
	const MAT_LABEL: Record<PhotoMat, string> = {
		night: 'Noc',
		charcoal: 'Uhel',
		umber: 'Umbra',
		dusk: 'Soumrak',
		ember: 'Žár',
		blur: 'Rozmazaná fotka'
	};

	interface Props {
		/** The crop being edited. */
		focal: Focal;
		/** The picture, for the swatch that is its own blur. */
		src: string;
		busy?: boolean;
		onchange: (focal: Focal) => void;
	}

	let { focal, src, busy = false, onchange }: Props = $props();
</script>

<div
	class="seg seg--glass fit"
	style:--slot={focal.fit === 'fill' ? 0 : 1}
	role="group"
	aria-label="Jak fotka vyplní dlaždici"
>
	<!-- The lens, as the board's segment has one (D74). -->
	<span class="seg__lens" aria-hidden="true"></span>
	<button
		type="button"
		class="seg__item"
		aria-pressed={focal.fit === 'fill'}
		onclick={() => onchange(fitted(focal, 'fill'))}
		disabled={busy}
	>
		Vyplnit
	</button>
	<button
		type="button"
		class="seg__item"
		aria-pressed={focal.fit === 'whole'}
		onclick={() => onchange(fitted(focal, 'whole'))}
		disabled={busy}
	>
		Celá
	</button>
</div>

{#if focal.fit === 'whole'}
	<div class="mats glass" role="group" aria-label="Pozadí kolem fotky">
		{#each PHOTO_MATS as one (one)}
			<button
				type="button"
				class="mat"
				class:mat--blur={one === 'blur'}
				style={one === 'blur' ? `background-image:url("${src}")` : `background:var(--mat-${one})`}
				aria-pressed={focal.mat === one}
				aria-label={MAT_LABEL[one]}
				onclick={() => onchange({ ...focal, mat: one })}
				disabled={busy}
			></button>
		{/each}
	</div>
{/if}

<style>
	/* The editor's top column lets taps through to the picture; these take them. */
	.fit,
	.mats {
		pointer-events: auto;
	}

	/* The mats: a swatch each, the colour being the label. The chosen one
	   wears a ring in the photograph's own ink — white in both themes, because
	   this is type on a photograph (rule 4) and not the interface's accent. */
	.mats {
		display: flex;
		gap: var(--space-2);
		padding: var(--space-2);
		border-radius: var(--radius-full);
	}

	.mat {
		width: 32px;
		height: 32px;
		border-radius: var(--radius-full);
		box-shadow: inset 0 0 0 1px var(--glass-edge);
		transition: box-shadow var(--dur-fast) var(--ease-out);
	}

	.mat--blur {
		background-size: cover;
		background-position: center;
	}

	.mat[aria-pressed='true'] {
		box-shadow:
			inset 0 0 0 2px var(--mat-night),
			0 0 0 2px var(--photo-ink);
	}
</style>
