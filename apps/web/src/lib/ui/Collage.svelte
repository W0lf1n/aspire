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
	 * A cell always fills: a photograph set to be shown whole is its point
	 * alone here (`photoStyle`), because a mat around one cell of five is a
	 * hole in the picture.
	 */
	import type { DreamImage } from '@aspire/contracts';
	import { gridStyle, type Template } from '$lib/dreams/collage';
	import { cellUrl, photoStyle } from '$lib/dreams/photos';

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
		<div class="dream__cell" style="grid-area:{template.cells[index]}">
			<img src={cellUrl(photo)} alt="" style={photoStyle(photo)} {loading} decoding="async" />
		</div>
	{/each}
</div>
