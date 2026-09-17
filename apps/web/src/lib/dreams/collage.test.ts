import { describe, expect, it } from 'vitest';
import {
	LAYOUTS,
	PHOTOS_MAX,
	gridStyle,
	templateFor,
	templatesFor,
	type Template
} from './collage';

/** How many tracks a `grid-template-*` value makes: `repeat(6, 1fr)` is six. */
function tracks(value: string): number {
	const repeated = /^repeat\((\d+),/.exec(value);
	return repeated ? Number(repeated[1]) : value.trim().split(/\s+/).length;
}

/** Every unit square a template's cells cover, as „row:column“, in cell order. */
function squares(template: Template): string[][] {
	return template.cells.map((area) => {
		const [r1, c1, r2, c2] = area.split('/').map(Number);
		const covered: string[] = [];
		for (let r = r1; r < r2; r++) for (let c = c1; c < c2; c++) covered.push(`${r}:${c}`);
		return covered;
	});
}

describe('the templates', () => {
	const counts = [2, 3, 4, 5];

	it('are three for every count from two to five', () => {
		for (const count of counts) expect(templatesFor(count)).toHaveLength(LAYOUTS);
		expect(PHOTOS_MAX).toBe(5);
	});

	it('have exactly one cell for every photograph', () => {
		for (const count of counts) {
			for (const template of templatesFor(count)) expect(template.cells).toHaveLength(count);
		}
	});

	it('tile their grid exactly: no hole for the mat to show through, no cell over another', () => {
		for (const count of counts) {
			for (const template of templatesFor(count)) {
				const covered = squares(template).flat();
				const whole = tracks(template.rows) * tracks(template.cols);

				expect(new Set(covered).size).toBe(covered.length);
				expect(covered).toHaveLength(whole);
			}
		}
	});

	it('give the first photograph the biggest cell, or one as big as any', () => {
		// „Jako první“ is also „make this the big one“, so the lead must never
		// be smaller than a cell behind it. Tracks differ in size, so compare
		// by squares only where every track is the same fraction.
		for (const count of counts) {
			for (const template of templatesFor(count)) {
				const even = !/[2-9]fr/.test(template.cols + template.rows);
				if (!even) continue;

				const sizes = squares(template).map((cell) => cell.length);
				expect(sizes[0]).toBe(Math.max(...sizes));
			}
		}
	});
});

describe('templateFor', () => {
	it('is nothing for a dream with one photograph or none', () => {
		expect(templateFor(0, 0)).toBeNull();
		expect(templateFor(1, 2)).toBeNull();
	});

	it('is the chosen variant among the ones for this many photographs', () => {
		expect(templateFor(3, 1)).toBe(templatesFor(3)[1]);
		expect(templateFor(5, 2)).toBe(templatesFor(5)[2]);
	});

	it('falls back to the first for a variant that is not one', () => {
		expect(templateFor(4, 7)).toBe(templatesFor(4)[0]);
		expect(templateFor(4, -1)).toBe(templatesFor(4)[0]);
		expect(templateFor(4, undefined)).toBe(templatesFor(4)[0]);
	});

	it('treats more than five as five, which is all a tile shows', () => {
		expect(templateFor(9, 0)).toBe(templatesFor(5)[0]);
	});
});

describe('gridStyle', () => {
	it('is the two track lists and nothing else', () => {
		expect(gridStyle(templatesFor(3)[0])).toBe(
			'grid-template-columns:1fr 1fr;grid-template-rows:3fr 2fr;'
		);
	});
});
