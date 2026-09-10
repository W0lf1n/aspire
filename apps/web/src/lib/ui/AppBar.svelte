<script lang="ts">
	/**
	 * The back header of a detail screen: a 40 px round chevron and the
	 * screen's name at 17. It sits in the page's own scroll column, so it
	 * scrolls away with the content rather than holding a bar.
	 *
	 * Only detail screens wear it. A tab screen puts its title in the flow as
	 * `.title` and has no back — the tabs are peers.
	 */
	import type { Snippet } from 'svelte';
	import { resolve } from '$app/paths';
	import Icon from './Icon.svelte';

	interface Props {
		title: string;
		/**
		 * Where the chevron goes: the board, the Nastavení hub, or one dream
		 * (from its edit screen). Two branches below rather than one resolved
		 * string, because the lint rule wants to see `resolve()` at the href.
		 */
		back?: '/' | '/nastaveni' | { dream: string };
		/** A control for the trailing edge — a 40 px round button. */
		trail?: Snippet;
	}

	let { title, back = '/', trail }: Props = $props();
</script>

{#snippet chevron()}
	<Icon name="chevron-left" size={18} stroke={1.8} />
{/snippet}

<header class="bar">
	{#if typeof back === 'string'}
		<a class="round" href={resolve(back)} aria-label="Zpět">{@render chevron()}</a>
	{:else}
		<a class="round" href={resolve('/sen/[id]', { id: back.dream })} aria-label="Zpět">
			{@render chevron()}
		</a>
	{/if}
	<h1 class="bar__title">{title}</h1>
	{#if trail}
		<div class="bar__trail">{@render trail()}</div>
	{/if}
</header>

<style>
	.bar {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		min-height: 48px;
	}

	.bar__title {
		flex: 1;
		min-width: 0;
		font-size: var(--text-lg);
		font-weight: 600;
		letter-spacing: var(--track-body);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.bar__trail {
		display: flex;
		align-items: center;
		gap: var(--space-2);
	}
</style>
