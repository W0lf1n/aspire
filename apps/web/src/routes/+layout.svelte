<script lang="ts">
	import '$lib/styles/app.css';
	import { afterNavigate } from '$app/navigation';
	import Toaster from '$lib/ui/Toaster.svelte';
	import { syncThemeColor } from '$lib/ui/theme';
	import { applyUpdate, watchUpdates } from '$lib/ui/update.svelte';
	import { health } from '$lib/api/client';
	import { reportOffset } from '$lib/push/nudge';
	import { watchConnection } from '$lib/offline/status.svelte';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	/* The status bar reads the resolved ground, which moves with the system
	   theme even when nothing else does. */
	$effect(() => {
		syncThemeColor();
		const media = matchMedia('(prefers-color-scheme: dark)');
		media.addEventListener('change', syncThemeColor);
		return () => media.removeEventListener('change', syncThemeColor);
	});

	/**
	 * A new build is asked for on every resume and reconnect, announced when
	 * it has taken over, and reloaded into on the next navigation.
	 */
	$effect(() => watchUpdates());
	afterNavigate(applyUpdate);

	/**
	 * Offline is a state every screen reads; the flag is kept here. One ping
	 * on start, because the browser's flag says nothing about the server and
	 * a screen opened cold has not asked it anything yet.
	 */
	$effect(() => {
		const stop = watchConnection();
		void health().catch(() => undefined);
		return stop;
	});

	/**
	 * Where this device is, told to the server on every open and every resume
	 * (D51). A phone is the same subscription in a new time zone after a
	 * flight and after every clock change, and the offset is the only thing
	 * the morning nudge has to go on (D34).
	 *
	 * A resume counts as an open: on a phone the app is rarely loaded cold,
	 * and the morning after the clocks move is exactly a resume.
	 */
	$effect(() => {
		void reportOffset();
		const onResume = () => {
			if (document.visibilityState === 'visible') void reportOffset();
		};
		document.addEventListener('visibilitychange', onResume);
		return () => document.removeEventListener('visibilitychange', onResume);
	});

	/**
	 * The launch splash lives in `app.html`, on screen from the first paint.
	 * It goes the moment the app has rendered, after its one rise.
	 */
	$effect(() => {
		const splash = document.getElementById('splash');
		if (!splash) return;
		const animated = splash.getAnimations({ subtree: true });
		let gone: ReturnType<typeof setTimeout> | undefined;
		void Promise.allSettled(animated.map((a) => a.finished)).then(() => {
			splash.classList.add('splash-out');
			gone = setTimeout(() => splash.remove(), 350);
		});
		return () => clearTimeout(gone);
	});
</script>

<div class="app">
	{@render children()}
	<Toaster />
</div>

<style>
	/**
	 * The frame. A column the height of the *dynamic* viewport: the tab bar is
	 * `flex: none`, and every screen owns exactly one scroll region inside it.
	 * On a desktop the app is still a phone-shaped column, and it says so.
	 *
	 * `position: relative` is the containing block the toast and the bar are
	 * positioned against, which keeps them inside the column on a desktop.
	 */
	.app {
		position: relative;
		z-index: var(--z-raised);
		display: flex;
		flex-direction: column;
		height: 100vh;
		height: 100dvh;
		overflow: hidden;
		max-width: 34rem;
		margin-inline: auto;
		padding-left: env(safe-area-inset-left, 0px);
		padding-right: env(safe-area-inset-right, 0px);
	}

	@media (min-width: 35rem) {
		.app {
			border-inline: 1px solid var(--hairline);
		}
	}
</style>
