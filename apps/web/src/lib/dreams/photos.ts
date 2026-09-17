/**
 * Which photograph a screen shows.
 *
 * A dream has two: the one that was dreamt, which is the board's picture,
 * and the one taken when it came true. The reel and the dream's tile show
 * the dreamt one; the Síň slávy stands the pair side by side, which is the
 * whole of the proof that it happened (D28).
 *
 * A photograph counts only once its sizes are ready — the row exists from
 * the upload and the WebPs follow a moment later — so a screen that reads
 * these never points an `<img>` at a file that is not there yet.
 */

import type { Dream, DreamImage, DreamImageKind } from '@aspire/contracts';
import { metered } from '$lib/offline/policy';

/**
 * What this device is, as far as a photograph's rung cares (D62, D66): how
 * many device pixels a CSS pixel is, and whether the bytes are on somebody's
 * data plan.
 */
export interface Screen {
	dpr: number;
	/**
	 * True on mobile data or Save Data, false on wifi and ethernet, `null`
	 * where the browser will not say — which on an iPhone is always
	 * (`offline/policy.ts`).
	 */
	metered: boolean | null;
}

/** The screen this is running on; a laptop's worth of pixels where there is none. */
export function thisScreen(): Screen {
	return {
		dpr: typeof window === 'undefined' ? 1 : window.devicePixelRatio || 1,
		metered: metered()
	};
}

/**
 * Which rung the reel reads on a screen (D62, D66). The reel is full-bleed on
 * a portrait phone: a 3:4 photograph at 1280 is 960×1280, and cover on a 3×
 * phone stretches it 2.2×, on a 2× phone 2.0×. The 2048 file is 1.4× and
 * 1.2× on the same screens, which is where Instagram's stories sit. So a
 * phone on wifi reads it — every phone is 2× or 3× — and a laptop at 1×
 * reads 1280, which is already more than its pixels.
 *
 * **A connection somebody pays for reads 1280 whatever the screen** (D66).
 * 350 kB a swipe against 200 is the compromise D62 struck, and it is only
 * worth striking where the bytes are free; `metered` folds Save Data in, so
 * asking the browser to spend less is the same answer. A browser that will
 * not say — Safari, so every iPhone — is not treated as metered, or the
 * device the app is built for would never see the rung that was built for it.
 *
 * One rule here rather than `srcset`, because the prefetch (`offline/cache`)
 * has to ask for the same URL the tile will show: a browser choosing per
 * image would put a file in the cache that nobody asked ahead for, and D39's
 * window is a promise about bytes.
 */
export function rungFor(screen: Screen): 'screen' | 'large' {
	return screen.dpr >= 2 && screen.metered !== true ? 'large' : 'screen';
}

/** The URL the reel shows for this photograph on this screen, or nothing. */
export function reelUrl(
	image: Pick<DreamImage, 'screenUrl' | 'largeUrl'> | null,
	screen: Screen = thisScreen()
): string | null {
	if (!image) return null;
	return rungFor(screen) === 'large' ? image.largeUrl : image.screenUrl;
}

/** The first ready photograph of a kind, or nothing. */
export function photoOf(dream: Pick<Dream, 'images'>, kind: DreamImageKind): DreamImage | null {
	return dream.images.find((image) => image.kind === kind && image.ready) ?? null;
}

/** Both of them at once, for a screen that shows the pair. */
export function photosOf(dream: Pick<Dream, 'images'>): {
	dreamt: DreamImage | null;
	achieved: DreamImage | null;
} {
	return { dreamt: photoOf(dream, 'dreamt'), achieved: photoOf(dream, 'achieved') };
}

/**
 * Whether a photograph of this kind is on its way: a row that exists and has
 * no sizes yet.
 *
 * Between the upload and the resize a dream has a photograph the screens
 * cannot show, and the tile paints the sky — which is also what a dream with
 * no photograph at all looks like (D52). The two are not the same thing and
 * must not read the same: one wants a picture, the other is holding one.
 * A screen asks this to tell them apart.
 */
export function photoComing(dream: Pick<Dream, 'images'>, kind: DreamImageKind): boolean {
	return dream.images.some((image) => image.kind === kind && !image.ready);
}

/**
 * A dream's photographs of one kind, ready or not. What the screen deletes
 * before it puts a new one in that place: replacing the dreamt photograph
 * must not touch the achieved one, and a row still being resized has to go
 * too or it comes back as a second picture a moment later.
 */
export function photosToReplace(dream: Pick<Dream, 'images'>, kind: DreamImageKind): DreamImage[] {
	return dream.images.filter((image) => image.kind === kind);
}

/** What a style needs of a photograph; a crop still being chosen has the same five. */
type Placed = Pick<DreamImage, 'focusX' | 'focusY' | 'zoom' | 'fit' | 'mat'>;

/**
 * How an `<img>` that **always fills its frame** shows this photograph: where
 * it is looked at, and how close (D54). The Seznam's circle, a pair on the
 * wall, a collage's cell, the blur under the reel's picture — every surface
 * with no room for a mat.
 *
 * `object-position` is the browser's own focal crop and needs no help. Zoom is
 * a `transform` on top of it, with its origin at the same point so the thing
 * being looked at stays where it is while the picture grows around it.
 *
 * A photograph shown whole is its point alone here (D81): its zoom was chosen
 * against the whole picture, and twice that is not twice the crop.
 *
 * A photograph nobody has moved gets no style at all rather than a style that
 * says „the middle, all of it“: the default is what `object-fit: cover`
 * already does, and an identity transform on five full-screen images is five
 * compositing layers bought for nothing.
 */
export function photoStyle(image: Placed | null): string {
	if (!image) return '';
	return placement(image, image.fit === 'whole' ? 1 : image.zoom);
}

/**
 * How an `<img>` on a surface **with room for a mat** shows this photograph:
 * the reel's tile, a dream's own tile, the editor (D81). Filling, it is
 * `photoStyle`. Whole, it is `object-fit: contain` and the same two
 * properties — the point places the picture in the room the frame has left,
 * which is what `object-position` means when there is room, and the zoom
 * scales up from all of it about that point (`images/focal.ts`).
 */
export function tileStyle(image: Placed | null): string {
	if (!image) return '';
	if (image.fit !== 'whole') return photoStyle(image);
	return 'object-fit:contain;' + placement(image, image.zoom);
}

/**
 * What the tile paints behind a photograph shown whole, as a style for the
 * tile: one of the five mats, by the token's name — a colour exists in
 * `tokens.css` and nowhere else, this file included. Nothing for a photograph
 * that fills its frame, and nothing for `blur`, whose mat is a picture
 * (`matIsBlur`).
 */
export function matStyle(image: Pick<DreamImage, 'fit' | 'mat'> | null): string {
	if (!image || image.fit !== 'whole' || image.mat === 'blur') return '';
	return `background:var(--mat-${image.mat});`;
}

/** Whether the mat is the photograph's own thumb, blurred, under the picture. */
export function matIsBlur(image: Pick<DreamImage, 'fit' | 'mat'> | null): boolean {
	return image !== null && image.fit === 'whole' && image.mat === 'blur';
}

function placement(image: Pick<DreamImage, 'focusX' | 'focusY'>, by: number): string {
	const x = percent(image.focusX);
	const y = percent(image.focusY);
	const zoom = Number.isFinite(by) ? Math.max(1, by) : 1;

	const position = x === 50 && y === 50 ? '' : `object-position:${x}% ${y}%;`;
	const scale = zoom === 1 ? '' : `transform:scale(${round(zoom)});transform-origin:${x}% ${y}%;`;
	return position + scale;
}

function percent(value: number): number {
	const unit = Number.isFinite(value) ? Math.min(1, Math.max(0, value)) : 0.5;
	return round(unit * 100);
}

/** Two decimals is finer than any screen can show and shorter than a float. */
function round(value: number): number {
	return Math.round(value * 100) / 100;
}
