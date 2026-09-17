/**
 * What a tile with several photographs is swiped through (D86, D87).
 *
 * A dream with two photographs or more is a row of slides, on the reel and on
 * its own tile alike: the collage its template cuts (D82) and then each
 * photograph on its own — or, when the dream says so, the photographs alone
 * with no collage in front of them. Which one is `photoView`, chosen per dream
 * on its shelf, because it is a choice about these photographs: a house from
 * five sides reads as one picture, five views from a trip read one at a time.
 *
 * One photograph, or none, is no row at all. That tile is the photograph, as
 * it always was, and the empty list says so.
 */

import type { Dream, DreamImage, PhotoView } from '@aspire/contracts';
import { templateFor, type Template } from './collage';
import { dreamtPhotos } from './photos';

export type Slide =
	| { kind: 'collage'; template: Template; photos: DreamImage[] }
	| { kind: 'photo'; photo: DreamImage };

/** The dream's view, and a collage for a board remembered from before the choice existed. */
export function viewOf(dream: Pick<Dream, 'photoView'>): PhotoView {
	return dream.photoView === 'carousel' ? 'carousel' : 'collage';
}

/** The slides in the order they are swiped through; empty for one photograph or none. */
export function slidesOf(dream: Pick<Dream, 'images' | 'layout' | 'photoView'>): Slide[] {
	const photos = dreamtPhotos(dream);
	const template = templateFor(photos.length, dream.layout);
	if (!template) return [];

	const each: Slide[] = photos.map((photo) => ({ kind: 'photo', photo }));
	return viewOf(dream) === 'carousel' ? each : [{ kind: 'collage', template, photos }, ...each];
}

/** A slide's key in a keyed list: the collage has one, each photograph has its own. */
export function slideKey(slide: Slide): string {
	return slide.kind === 'collage' ? 'collage' : slide.photo.id;
}
