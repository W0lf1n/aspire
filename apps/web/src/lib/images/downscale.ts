/**
 * The photograph, made small enough to send from a phone on mobile data
 * (PLAN.md §4): the longest edge to 2048 px, as a JPEG, turned the way the
 * camera meant it. The server makes its own three sizes from this one and
 * never sees the twelve-megapixel original.
 */

/** What the server keeps at most; sending more is sending for nothing. */
export const MAX_EDGE = 2048;
const JPEG_QUALITY = 0.86;

/** The size that fits inside `max`, keeping the ratio and never growing. */
export function fitWithin(
	width: number,
	height: number,
	max: number
): { width: number; height: number } {
	const longest = Math.max(width, height);
	if (longest <= max) return { width, height };
	const scale = max / longest;
	return {
		width: Math.max(1, Math.round(width * scale)),
		height: Math.max(1, Math.round(height * scale))
	};
}

/**
 * Several files picked at once, each made small, in the order they were
 * picked (D84). One that is not a photograph after all is left out and
 * counted rather than costing the others, so the screen can say so and still
 * send the rest. One at a time, because five twelve-megapixel bitmaps decoded
 * together is a lot to ask of a phone's memory.
 */
export async function downscaleAll(
	files: Blob[],
	read: (file: Blob) => Promise<Blob> = downscale
): Promise<{ photos: Blob[]; unreadable: number }> {
	const photos: Blob[] = [];
	let unreadable = 0;
	for (const file of files) {
		try {
			photos.push(await read(file));
		} catch {
			unreadable += 1;
		}
	}
	return { photos, unreadable };
}

/** The sentence for picked files that were not photographs: all of them, or some. */
export function unreadableSentence(unreadable: number, picked: number): string {
	return unreadable >= picked
		? 'Tohle se nepodařilo přečíst jako fotku.'
		: 'Některou z fotek se nepodařilo přečíst, ostatní beru.';
}

export async function downscale(file: Blob, max = MAX_EDGE): Promise<Blob> {
	// `from-image` applies the EXIF orientation, so the bitmap is upright and
	// the JPEG that leaves here carries no orientation tag to get wrong.
	const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
	try {
		const { width, height } = fitWithin(bitmap.width, bitmap.height, max);
		const canvas = document.createElement('canvas');
		canvas.width = width;
		canvas.height = height;
		const context = canvas.getContext('2d');
		if (!context) throw new Error('Bez plátna.');
		context.drawImage(bitmap, 0, 0, width, height);
		return await new Promise<Blob>((resolve, reject) => {
			canvas.toBlob(
				(blob) => (blob ? resolve(blob) : reject(new Error('Fotku se nepodařilo zmenšit.'))),
				'image/jpeg',
				JPEG_QUALITY
			);
		});
	} finally {
		bitmap.close();
	}
}
