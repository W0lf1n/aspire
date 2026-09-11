/**
 * The bar's four destinations, and which of them a path lights up. Kept
 * out of the component so the rule has a test: `/nastaveni/vzhled` keeps
 * Nastavení lit, `/pridat` lights nothing.
 */

import type { IconName } from './Icon.svelte';

export type TabId = 'board' | 'list' | 'hall' | 'settings';

export interface Tab {
	id: TabId;
	path: '/' | '/seznam' | '/sin-slavy' | '/nastaveni';
	label: string;
	icon: IconName;
}

export const TABS: readonly Tab[] = [
	{ id: 'board', path: '/', label: 'Nástěnka', icon: 'board' },
	{ id: 'list', path: '/seznam', label: 'Seznam', icon: 'list' },
	{ id: 'hall', path: '/sin-slavy', label: 'Síň slávy', icon: 'trophy' },
	{ id: 'settings', path: '/nastaveni', label: 'Nastavení', icon: 'settings' }
];

/** The tab a pathname belongs to, or null for a screen outside the bar. */
export function activeTab(pathname: string): TabId | null {
	const here = pathname.replace(/\/+$/, '') || '/';
	// A dream's own screens are the board, one level down — from the Seznam
	// too: a dream belongs to the board, whichever door was used.
	if (here.startsWith('/sen/')) return 'board';
	for (const tab of TABS) {
		if (here === tab.path) return tab.id;
		if (tab.path !== '/' && here.startsWith(`${tab.path}/`)) return tab.id;
	}
	return null;
}
