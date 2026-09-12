import { describe, expect, it } from 'vitest';
import { CANNOT, cannot, isLocked } from './writes.svelte';

describe('isLocked', () => {
	it('is locked without a signal, whatever the control itself thinks', () => {
		expect(isLocked(false)).toBe(true);
		expect(isLocked(false, false)).toBe(true);
		expect(isLocked(false, true)).toBe(true);
	});

	it('is open with a signal and nothing else in the way', () => {
		expect(isLocked(true)).toBe(false);
		expect(isLocked(true, false)).toBe(false);
	});

	it('is locked with a signal when the control has its own reason', () => {
		expect(isLocked(true, true)).toBe(true);
	});
});

describe('cannot', () => {
	it('says nothing at all when the write can go', () => {
		expect(cannot(true, 'add')).toBe('');
		expect(cannot(true, 'save')).toBe('');
	});

	it('names the verb the form cannot do', () => {
		expect(cannot(false, 'add')).toBe(CANNOT.add);
		expect(cannot(false, 'save')).toBe(CANNOT.save);
	});

	it('is a Czech sentence, because it is on the screen', () => {
		for (const sentence of Object.values(CANNOT)) {
			expect(sentence).toMatch(/^Bez připojení .+\.$/);
		}
	});
});
