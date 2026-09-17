import { afterEach, describe, expect, it } from 'vitest';
import { flushSync, mount, unmount } from 'svelte';
import type { Dream, DreamImage } from '@aspire/contracts';
import { connection } from '$lib/offline/status.svelte';
import ReelTile from './ReelTile.svelte';

/**
 * The tile, rendered. The rules it reads are tested in `dreams/photos.ts`
 * and `dreams/board.ts`; what is asked here is the thing only a component
 * can get wrong — which element ends up on the page, and whether the one
 * that writes is dead without a signal (D68).
 */

function image(id: string, ready: boolean, kind: DreamImage['kind'] = 'dreamt'): DreamImage {
	return {
		id,
		sortOrder: 0,
		kind,
		width: 1600,
		height: 2000,
		ready,
		focusX: 0.5,
		focusY: 0.5,
		zoom: 1,
		fit: 'fill',
		mat: 'night',
		thumbUrl: `/media/d/${id}/thumb.webp`,
		screenUrl: `/media/d/${id}/screen.webp`,
		largeUrl: `/media/d/${id}/full.webp`
	};
}

function dream(over: Partial<Dream> = {}): Dream {
	return {
		id: 'd1',
		title: 'Naučit se surfovat',
		why: 'Postavit se na první vlně.',
		affirmation: '',
		status: 'dreaming',
		category: null,
		sortOrder: 0,
		focusRank: null,
		targetYear: null,
		layout: 0,
		likes: 0,
		achievedAt: null,
		lastShownAt: null,
		createdAt: '2026-01-01T00:00:00Z',
		updatedAt: '2026-01-01T00:00:00Z',
		images: [],
		...over
	};
}

let live: Record<string, unknown> | null = null;
let host: HTMLElement | null = null;

function render(props: Partial<Parameters<typeof ReelTile>[1]> & { dream: Dream }) {
	host = document.createElement('div');
	document.body.appendChild(host);
	live = mount(ReelTile, {
		target: host,
		props: { onlike: () => {}, onpick: () => {}, ...props }
	});
	flushSync();
	return host;
}

afterEach(() => {
	if (live) unmount(live);
	host?.remove();
	live = null;
	host = null;
	connection.set(true);
});

describe('ReelTile', () => {
	it('paints the sky and offers the pill when there is no photograph (D52)', () => {
		const el = render({ dream: dream() });

		expect(el.querySelector('.dream--sky')).not.toBeNull();
		expect(el.querySelector('.dream__img')).toBeNull();
		expect(el.querySelector('.reel__file')).not.toBeNull();
		expect(el.querySelector('.reel__pick')?.textContent).toContain('Přidat fotku');
	});

	it('says the photograph is being made instead of asking for it again', () => {
		const el = render({ dream: dream({ images: [image('i1', false)] }) });

		// The same sky as a dream with none, and this is the only thing that
		// tells the two apart.
		expect(el.querySelector('.dream--sky')).not.toBeNull();
		expect(el.querySelector('.reel__pick')?.textContent).toContain('Zpracovává se');
		// No way to send a second copy of the photograph that is already there.
		expect(el.querySelector('.reel__file')).toBeNull();
	});

	it('shows the photograph with the thumb under it once the sizes are there (D63)', () => {
		const el = render({ dream: dream({ images: [image('i1', true)] }) });

		expect(el.querySelector('.dream--sky')).toBeNull();
		expect(el.querySelector('.dream__img')?.getAttribute('src')).toBe('/media/d/i1/screen.webp');
		expect(el.querySelector('.dream__under')?.getAttribute('src')).toBe('/media/d/i1/thumb.webp');
		// The photograph is there, so nothing asks for one.
		expect(el.querySelector('.reel__pick')).toBeNull();
	});

	it('stands a photograph shown whole on its mat, with no thumb to show through it (D81)', () => {
		const whole: DreamImage = { ...image('i1', true), fit: 'whole', mat: 'dusk' };
		const el = render({ dream: dream({ images: [whole] }) });

		expect(el.querySelector('.reel__tile')?.getAttribute('style')).toContain('var(--mat-dusk)');
		// happy-dom reads the style back with a space after the colon.
		expect(el.querySelector<HTMLElement>('.dream__img')?.style.objectFit).toBe('contain');
		// The thumb covers the frame and the picture no longer does.
		expect(el.querySelector('.dream__under')).toBeNull();
	});

	it('puts the blurred thumb back under it when that is the mat', () => {
		const whole: DreamImage = { ...image('i1', true), fit: 'whole', mat: 'blur' };
		const el = render({ dream: dream({ images: [whole] }) });

		expect(el.querySelector('.dream__under')?.getAttribute('src')).toBe('/media/d/i1/thumb.webp');
		expect(el.querySelector('.reel__tile')?.getAttribute('style') ?? '').not.toContain('--mat-');
	});

	it('cuts two photographs or more into a collage, one cell each (D82)', () => {
		const el = render({
			dream: dream({
				layout: 1,
				images: [image('i1', true), image('i2', true), image('i3', true)]
			})
		});

		const cells = [...el.querySelectorAll('.dream__cell img')];
		expect(cells.map((cell) => cell.getAttribute('src'))).toEqual([
			'/media/d/i1/screen.webp',
			'/media/d/i2/screen.webp',
			'/media/d/i3/screen.webp'
		]);
		// The second template for three: three bands.
		expect(el.querySelector<HTMLElement>('.dream__collage')?.style.gridTemplateRows).toBe(
			'1fr 1fr 1fr'
		);
		// The collage is the picture: no single image, and no blur under one.
		expect(el.querySelector('.dream__img')).toBeNull();
		expect(el.querySelector('.dream__under')).toBeNull();
		expect(el.querySelector('.reel__pick')).toBeNull();
	});

	it('shows one photograph as the photograph while a second is still being made', () => {
		const el = render({ dream: dream({ images: [image('i1', true), image('i2', false)] }) });

		expect(el.querySelector('.dream__collage')).toBeNull();
		expect(el.querySelector('.dream__img')).not.toBeNull();
	});

	it('shows the picked file before the server has it, and hides the blur under it', () => {
		const el = render({ dream: dream(), preview: 'blob:pretend' });

		expect(el.querySelector('.dream__img')?.getAttribute('src')).toBe('blob:pretend');
		expect(el.querySelector('.dream__under')).toBeNull();
		expect(el.querySelector('.reel__pick')?.textContent).toContain('Ukládám');
	});

	it('fills the heart only once the dream has been fuelled (D58)', () => {
		const cold = render({ dream: dream({ likes: 0 }) });
		expect(cold.querySelector('.reel__fuel--lit')).toBeNull();
		// No number on the tile: the count is a measurement, and a measurement
		// on a photograph is what §2's third principle keeps off the board.
		expect(cold.querySelector('.reel__fuel')?.textContent?.trim()).toBe('');

		unmount(live!);
		host!.remove();
		live = null;

		const lit = render({ dream: dream({ likes: 3 }) });
		expect(lit.querySelector('.reel__fuel--lit')).not.toBeNull();
	});

	it('kills the heart and the file input without a signal, and revives them (D67)', () => {
		const el = render({ dream: dream(), canPick: true });
		const heart = el.querySelector<HTMLButtonElement>('.reel__fuel')!;
		const file = el.querySelector<HTMLInputElement>('.reel__file')!;
		expect(heart.disabled).toBe(false);
		expect(file.disabled).toBe(false);

		connection.set(false);
		flushSync();
		expect(heart.disabled).toBe(true);
		expect(file.disabled).toBe(true);

		connection.set(true);
		flushSync();
		expect(heart.disabled).toBe(false);
	});

	it('keeps the file input dead while another upload is in flight', () => {
		const el = render({ dream: dream(), canPick: false });

		expect(el.querySelector<HTMLInputElement>('.reel__file')!.disabled).toBe(true);
	});

	it('hands the tapped dream back to the board', () => {
		const liked: string[] = [];
		const el = render({ dream: dream({ id: 'surf' }), onlike: (d: Dream) => liked.push(d.id) });

		el.querySelector<HTMLButtonElement>('.reel__fuel')!.click();
		flushSync();

		expect(liked).toEqual(['surf']);
	});

	it('says the status and the area on the badge, and the whole tile opens the dream', () => {
		const el = render({
			dream: dream({ id: 'surf', status: 'in-progress', category: 'do', title: 'Surf' })
		});

		expect(el.querySelector('.dream__tag')?.textContent?.trim()).toBe('plním · Dělat');
		expect(el.querySelector('.reel__open')?.getAttribute('href')).toContain('surf');
		expect(el.querySelector('.reel__open')?.getAttribute('aria-label')).toBe('Surf');
	});
});
