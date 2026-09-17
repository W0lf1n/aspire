<script lang="ts">
	/**
	 * A dream's photographs as one picture cut into cells (D82): the grid is
	 * the template's, the crop in every cell is the photograph's own point
	 * (D54), and the seam between two cells is the darkest mat.
	 *
	 * It fills whatever it is put in — absolute, edge to edge, no height of its
	 * own — so on the reel it adds nothing to the page's arithmetic (rule 14)
	 * and on a 4:5 print it is a 4:5 collage without a second set of templates.
	 * The look is `.dream__collage` in `app.css`, beside the tile it lives in.
	 *
	 * A cell shows its photograph as a tile does (D88): filling it, or whole on
	 * its mat — one of the colours, painted by the cell, or its own thumb
	 * blurred under it. A cell used to always fill, and a landscape photograph
	 * in a tall cell was a sliver of itself with no way to see the rest.
	 */
	import type { DreamImage } from '@aspire/contracts';
	import { gridStyle, type Template } from '$lib/dreams/collage';
	import { cellUrl, matIsBlur, matStyle, photoStyle, tileStyle } from '$lib/dreams/photos';

	interface Props {
		/** The ready dreamt photographs, in order; the first is the lead. */
		photos: DreamImage[];
		template: Template;
		/** Whether the tile is on the screen or beside it, as the reel decides. */
		loading?: 'eager' | 'lazy';
	}

	const { photos, template, loading = 'lazy' }: Props = $props();
</script>

<div class="dream__collage" style={gridStyle(template)} aria-hidden="true">
	{#each photos.slice(0, template.cells.length) as photo, index (photo.id)}
		<div class="dream__cell" style="grid-area:{template.cells[index]};{matStyle(photo)}">
			{#if matIsBlur(photo)}
				<img class="dream__under" src={photo.thumbUrl} alt="" style={photoStyle(photo)} {loading} />
			{/if}
			<img src={cellUrl(photo)} alt="" style={tileStyle(photo)} {loading} decoding="async" />
		</div>
	{/each}
</div>
