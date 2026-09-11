/**
 * The rooms of Nastavení, as the hub lists them: a title, a one-line live
 * summary, an icon and the page it opens.
 */

import type { IconName } from './Icon.svelte';
import { MODE_LABEL } from '$lib/push/schedule';
import type { NudgeMode } from '@aspire/contracts';
import { THEME_LABEL, type Theme } from './theme';

export type SettingsPage = 'vzhled' | 'upozorneni' | 'tapeta' | 'parovani';

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
	/** What the morning nudge is set to on this device (PLAN.md §3.6). */
	nudge: NudgeMode;
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
			id: 'upozorneni',
			href: '/nastaveni/upozorneni',
			title: 'Upozornění',
			sub: MODE_LABEL[facts.nudge].toLocaleLowerCase('cs-CZ'),
			icon: 'sparkles'
		},
		{
			id: 'tapeta',
			href: '/nastaveni/tapeta',
			title: 'Tapeta',
			sub: facts.paired ? 'sny na zámek telefonu' : 'až bude spárováno',
			icon: 'image'
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
