<script lang="ts">
	/**
	 * The toast: one glass pill in the ink, centred above the tab bar. A toast
	 * with an action carries it as a small primary pill on its right; tapping
	 * the toast itself puts it away.
	 */
	import { toast } from './toast.svelte';

	async function act() {
		const action = toast.current?.action;
		toast.dismiss();
		await action?.run();
	}
</script>

{#if toast.current}
	{@const t = toast.current}
	{#key t.id}
		<div class="wrap" role="status" aria-live="polite">
			<div class="toast" role="presentation" onclick={toast.dismiss}>
				<span class="toast__text">{t.message}</span>
				{#if t.action}
					<button
						type="button"
						class="toast__action"
						onclick={(event) => {
							event.stopPropagation();
							void act();
						}}
					>
						{t.action.label}
					</button>
				{/if}
			</div>
		</div>
	{/key}
{/if}

<style>
	/* Absolute inside the app column: on a desktop the app is a phone-shaped
	   column in the middle of a wide window, not the window. */
	.wrap {
		position: absolute;
		left: var(--space-4);
		right: var(--space-4);
		bottom: calc(var(--toast-lift, var(--space-5)) + env(safe-area-inset-bottom, 0px));
		z-index: var(--z-toast);
		display: flex;
		justify-content: center;
		pointer-events: none;
	}

	.toast {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-3);
		width: 100%;
		max-width: 26rem;
		min-height: 48px;
		padding: 14px 20px;
		border-radius: var(--radius-full);
		background: var(--glass);
		border: 1px solid var(--glass-edge);
		-webkit-backdrop-filter: blur(24px) saturate(1.6);
		backdrop-filter: blur(24px) saturate(1.6);
		color: var(--ink);
		font-size: var(--text-md);
		font-weight: 600;
		text-align: center;
		box-shadow: var(--elev-glass);
		pointer-events: auto;
		animation: toast-in var(--dur-base) var(--ease-out);
	}

	@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
		.toast {
			background: var(--surface);
		}
	}

	.toast__text {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.toast__action {
		flex: none;
		margin: -6px -8px -6px 0;
		padding: 6px 12px;
		border-radius: var(--radius-full);
		background: var(--pill);
		color: var(--pill-ink);
		font-size: var(--text-sm);
		font-weight: 600;
	}

	@keyframes toast-in {
		from {
			opacity: 0;
			transform: translateY(12px);
		}
		to {
			opacity: 1;
			transform: none;
		}
	}
</style>
