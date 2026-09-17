import { describe, expect, it } from 'vitest';
import { MAX_EDGE, downscaleAll, fitWithin, unreadableSentence } from './downscale';

describe('fitWithin', () => {
	it('shrinks the longest edge to the limit and keeps the ratio', () => {
		expect(fitWithin(4000, 3000, MAX_EDGE)).toEqual({ width: 2048, height: 1536 });
		expect(fitWithin(3000, 4000, MAX_EDGE)).toEqual({ width: 1536, height: 2048 });
	});

	it('never grows a small picture', () => {
		expect(fitWithin(800, 600, MAX_EDGE)).toEqual({ width: 800, height: 600 });
		expect(fitWithin(2048, 100, MAX_EDGE)).toEqual({ width: 2048, height: 100 });
	});

	it('keeps a sliver at least one pixel wide', () => {
		expect(fitWithin(100000, 10, 2048)).toEqual({ width: 2048, height: 1 });
	});
});

describe('downscaleAll (D84)', () => {
	const file = (name: string) => new Blob([name], { type: 'image/jpeg' });

	it('reads every file in the order picked, and leaves out what is not a photograph', async () => {
		const read = async (blob: Blob) => {
			const name = await blob.text();
			if (name === 'pdf') throw new Error('not an image');
			return new Blob([`small ${name}`]);
		};

		const { photos, unreadable } = await downscaleAll(
			[file('one'), file('pdf'), file('two')],
			read
		);

		expect(await Promise.all(photos.map((photo) => photo.text()))).toEqual([
			'small one',
			'small two'
		]);
		expect(unreadable).toBe(1);
	});

	it('says whether none of them could be read, or only some', () => {
		expect(unreadableSentence(1, 1)).toBe('Tohle se nepodařilo přečíst jako fotku.');
		expect(unreadableSentence(1, 3)).toBe('Některou z fotek se nepodařilo přečíst, ostatní beru.');
	});
});
