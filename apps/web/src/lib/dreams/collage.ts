/**
 * The collage: how a tile is divided when a dream has more than one
 * photograph (D82).
 *
 * A dream can have up to five dreamt photographs — the house, the garden,
 * the view — and the two surfaces big enough to show them together, the reel
 * and the dream's own tile, show them as one picture cut into cells. Every
 * small surface shows the first photograph alone: a 40 px circle divided in
 * five is not a picture of anything.
 *
 * A template is a CSS grid and nothing else: tracks, and one `grid-area` per
 * photograph. That is the whole reason it works on a phone screen and on a 4:5
 * print without a second set — a grid has no shape of its own, it takes the
 * frame's. Each cell then crops its photograph to the photograph's own point
 * (D54), so a face kept in view on the reel is kept in view in a cell a
 * quarter the size.
 *
 * Three templates for every count, and a dream stores which — 0, 1 or 2 —
 * rather than a name (`Dream.layout`): the second template for three
 * photographs is not the second for five, and a number survives a photograph
 * being added or taken away where a name would have to be translated.
 *
 * The first photograph is the lead. In every template that has a big cell it
 * is the first cell, so „Jako první“ on the dream's screen is also „make this
 * the big one“.
 */

/** How many dreamt photographs a dream can have. Mirrors `Dream.PhotosMax`. */
export const PHOTOS_MAX = 5;

/** How many templates every count has. Mirrors `Dream.LayoutsMax`. */
export const LAYOUTS = 3;

export interface Template {
	/** `grid-template-columns`. */
	cols: string;
	/** `grid-template-rows`. */
	rows: string;
	/** One `grid-area` per photograph: row start / column start / row end / column end. */
	cells: string[];
}

const TEMPLATES: Record<number, Template[]> = {
	2: [
		// Stacked, because the reel is tall: two landscapes, one over the other.
		{ cols: '1fr', rows: '1fr 1fr', cells: ['1/1/2/2', '2/1/3/2'] },
		// Side by side: two portraits.
		{ cols: '1fr 1fr', rows: '1fr', cells: ['1/1/2/2', '1/2/2/3'] },
		// The lead and a strip under it.
		{ cols: '1fr', rows: '2fr 1fr', cells: ['1/1/2/2', '2/1/3/2'] }
	],
	3: [
		// The lead across the top, two under it.
		{ cols: '1fr 1fr', rows: '3fr 2fr', cells: ['1/1/2/3', '2/1/3/2', '2/2/3/3'] },
		// Three bands.
		{ cols: '1fr', rows: '1fr 1fr 1fr', cells: ['1/1/2/2', '2/1/3/2', '3/1/4/2'] },
		// The lead down the left, two stacked on the right.
		{ cols: '3fr 2fr', rows: '1fr 1fr', cells: ['1/1/3/2', '1/2/2/3', '2/2/3/3'] }
	],
	4: [
		// Two by two.
		{ cols: '1fr 1fr', rows: '1fr 1fr', cells: ['1/1/2/2', '1/2/2/3', '2/1/3/2', '2/2/3/3'] },
		// The lead across the top, three under it.
		{
			cols: '1fr 1fr 1fr',
			rows: '3fr 2fr',
			cells: ['1/1/2/4', '2/1/3/2', '2/2/3/3', '2/3/3/4']
		},
		// The lead down the left, three stacked on the right.
		{
			cols: '3fr 2fr',
			rows: '1fr 1fr 1fr',
			cells: ['1/1/4/2', '1/2/2/3', '2/2/3/3', '3/2/4/3']
		}
	],
	5: [
		// The lead across the top, two by two under it.
		{
			cols: '1fr 1fr',
			rows: '2fr 1fr 1fr',
			cells: ['1/1/2/3', '2/1/3/2', '2/2/3/3', '3/1/4/2', '3/2/4/3']
		},
		// Two over three, on sixths so both rows come out even.
		{
			cols: 'repeat(6, 1fr)',
			rows: '3fr 2fr',
			cells: ['1/1/2/4', '1/4/2/7', '2/1/3/3', '2/3/3/5', '2/5/3/7']
		},
		// The lead down the left, four stacked on the right.
		{
			cols: '3fr 2fr',
			rows: 'repeat(4, 1fr)',
			cells: ['1/1/5/2', '1/2/2/3', '2/2/3/3', '3/2/4/3', '4/2/5/3']
		}
	]
};

/** The templates there are for this many photographs; none for fewer than two. */
export function templatesFor(count: number): Template[] {
	return TEMPLATES[Math.min(Math.trunc(count), PHOTOS_MAX)] ?? [];
}

/**
 * The template a tile uses: the dream's chosen variant among the ones for
 * however many photographs it has now, or nothing when it has one or none —
 * a single photograph is not a collage, it is the photograph.
 *
 * A variant out of range is the first. It cannot be stored that way, and a
 * board remembered from before the field existed has none at all.
 */
export function templateFor(count: number, layout: number | undefined): Template | null {
	const all = templatesFor(count);
	if (all.length === 0) return null;
	return all[Number.isInteger(layout) && layout! >= 0 && layout! < all.length ? layout! : 0];
}

/** The grid itself, as the style of the element that holds the cells. */
export function gridStyle(template: Template): string {
	return `grid-template-columns:${template.cols};grid-template-rows:${template.rows};`;
}
