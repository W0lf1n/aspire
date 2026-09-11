import { describe, expect, it } from 'vitest';
import { shellsToForget } from './shell';

const KEPT = ['aspire-media', 'aspire-board'];

describe('shellsToForget', () => {
	it('keeps the build before this one, so a page still running it can navigate', () => {
		const keys = ['aspire-1789000000000', 'aspire-1789116223768', ...KEPT];

		expect(shellsToForget(keys, 'aspire-1789116223768', KEPT)).toEqual([]);
	});

	it('throws away the generation before that', () => {
		const keys = ['aspire-1788000000000', 'aspire-1789000000000', 'aspire-1789116223768', ...KEPT];

		expect(shellsToForget(keys, 'aspire-1789116223768', KEPT)).toEqual(['aspire-1788000000000']);
	});

	it('never touches the photographs or the board, which are not shells', () => {
		const keys = ['aspire-1789116223768', ...KEPT];

		expect(shellsToForget(keys, 'aspire-1789116223768', KEPT)).toEqual([]);
	});

	it('reads the order off the name, not off the order the browser listed them in', () => {
		const keys = ['aspire-1789000000000', 'aspire-1788000000000', 'aspire-1787000000000'];

		// The newest of the leftovers survives wherever it appears in the list.
		expect(shellsToForget(keys, 'aspire-1789116223768', KEPT)).toEqual([
			'aspire-1788000000000',
			'aspire-1787000000000'
		]);
	});

	it('drops a cache with no build stamp, which cannot be placed in time', () => {
		const keys = ['aspire-legacy', 'aspire-1789000000000', ...KEPT];

		expect(shellsToForget(keys, 'aspire-1789116223768', KEPT)).toEqual(['aspire-legacy']);
	});

	it('has nothing to do on a first install', () => {
		expect(shellsToForget(['aspire-1789116223768'], 'aspire-1789116223768', KEPT)).toEqual([]);
		expect(shellsToForget([], 'aspire-1789116223768', KEPT)).toEqual([]);
	});
});
