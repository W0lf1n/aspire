/**
 * The rooms of Nastavení, as the hub lists them: a title, a one-line live
 * summary, an icon and the page it opens.
 */

import type { IconName } from './Icon.svelte';
import { THEME_LABEL, type Theme } from './theme';

export type SettingsPage = 'vzhled' | 'parovani';

export interface SettingsRow {
	id: SettingsPage;
	href: `/nastaveni/${SettingsPage}`;
	title: string;
	/** What is in the room right now, in one line. */
	sub: string;
	icon: IconName;
}

export interface SettingsFacts {
	theme: Theme;
	/** Whether this device holds a token. */
	paired: boolean;
}

export function settingsRows(facts: SettingsFacts): SettingsRow[] {
	return [
		{
			id: 'vzhled',
			href: '/nastaveni/vzhled',
			title: 'Vzhled',
			sub: THEME_LABEL[facts.theme],
			icon: 'sun-moon'
		},
		{
			id: 'parovani',
			href: '/nastaveni/parovani',
			title: 'Párování',
			sub: facts.paired ? 'spárováno' : 'zatím nespárováno',
			icon: 'link'
		}
	];
}
