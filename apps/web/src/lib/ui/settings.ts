/**
 * The rooms of Nastavení, as the hub lists them: a title, a one-line live
 * summary, an icon and the page it opens.
 */

import type { IconName } from './Icon.svelte';
import { MODE_LABEL } from '$lib/push/schedule';
import { POLICY_SUMMARY, type OfflinePolicy } from '$lib/offline/policy';
import type { NudgeMode } from '@aspire/contracts';
import { THEME_LABEL, type Theme } from './theme';

export type SettingsPage =
	'vzhled' | 'upozorneni' | 'tapeta' | 'stahovani' | 'parovani' | 'zacit-znovu';

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
	/** How much of the board this device keeps for offline (D39). */
	offline: OfflinePolicy;
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
			id: 'stahovani',
			href: '/nastaveni/stahovani',
			title: 'Stahování',
			sub: POLICY_SUMMARY[facts.offline],
			icon: 'cloud-download'
		},
		{
			id: 'parovani',
			href: '/nastaveni/parovani',
			title: 'Párování',
			sub: facts.paired ? 'spárováno' : 'zatím nespárováno',
			icon: 'link'
		},
		// Last, below the room that would undo it: the one thing in here that
		// cannot be taken back (D80).
		{
			id: 'zacit-znovu',
			href: '/nastaveni/zacit-znovu',
			title: 'Začít znovu',
			sub: facts.paired ? 'smaže všechny sny a fotky' : 'až bude spárováno',
			icon: 'trash'
		}
	];
}
