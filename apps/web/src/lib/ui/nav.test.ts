import { describe, expect, it } from 'vitest';
import { activeTab } from './nav';

describe('activeTab', () => {
	it('lights the board on the root', () => {
		expect(activeTab('/')).toBe('board');
		expect(activeTab('')).toBe('board');
	});

	it('keeps Nastavení lit on its pages', () => {
		expect(activeTab('/nastaveni')).toBe('settings');
		expect(activeTab('/nastaveni/vzhled')).toBe('settings');
		expect(activeTab('/nastaveni/')).toBe('settings');
	});

	it('keeps the board lit on a dream', () => {
		expect(activeTab('/sen/abc')).toBe('board');
		expect(activeTab('/sen/abc/upravit')).toBe('board');
	});

	it('lights the hall of fame', () => {
		expect(activeTab('/sin-slavy')).toBe('hall');
	});

	it('lights the list', () => {
		expect(activeTab('/seznam')).toBe('list');
		expect(activeTab('/seznam/')).toBe('list');
	});

	it('lights nothing for a screen outside the bar', () => {
		expect(activeTab('/pridat')).toBeNull();
		expect(activeTab('/styleguide')).toBeNull();
	});

	it('does not confuse a prefix with a page', () => {
		expect(activeTab('/nastaveni-x')).toBeNull();
	});
});
