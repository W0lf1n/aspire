import { describe, expect, it } from 'vitest';
import { shareText, shareUrl } from './share';

describe('shareUrl', () => {
	it('is the origin the browser knows and the path the server gave', () => {
		expect(shareUrl('https://sny.example', '/s/abc')).toBe('https://sny.example/s/abc');
	});

	it('is a URL the browser can parse back', () => {
		const url = new URL(shareUrl('https://sny.example', '/s/abc'));

		expect(url.pathname).toBe('/s/abc');
		expect(url.search).toBe('');
	});
});

describe('shareText', () => {
	it('says the dream and nothing more private than the dream', () => {
		expect(shareText('Dům u lesa')).toBe('Můj sen: Dům u lesa');
	});
});
