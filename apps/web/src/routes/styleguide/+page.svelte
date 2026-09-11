<script lang="ts">
	/**
	 * /styleguide — the tokens and the base components, on one screen, in
	 * either theme, so the look can be judged before M1 builds on it. Unlinked
	 * from the app; a developer route.
	 */
	import Icon from '$lib/ui/Icon.svelte';
	import Sheet from '$lib/ui/Sheet.svelte';
	import TabBar from '$lib/ui/TabBar.svelte';
	import { applyTheme, readTheme, type Theme } from '$lib/ui/theme';
	import { toast } from '$lib/ui/toast.svelte';

	let theme = $state<Theme>(readTheme());
	let on = $state(true);
	let chip = $state<'vse' | 'sny' | 'cesta'>('vse');
	let sheet = $state(false);

	const THEMES: { value: Theme; label: string }[] = [
		{ value: 'system', label: 'Systém' },
		{ value: 'light', label: 'Světlý' },
		{ value: 'dark', label: 'Tmavý' }
	];

	/** The colour roles, read off the root so the swatch says what it is. */
	const ROLES = [
		'--ground',
		'--surface',
		'--surface-2',
		'--surface-3',
		'--hairline',
		'--ink',
		'--ink-2',
		'--ink-3',
		'--pill',
		'--signal',
		'--dusk',
		'--danger'
	] as const;

	let values = $state<Record<string, string>>({});

	function readValues() {
		const style = getComputedStyle(document.documentElement);
		const next: Record<string, string> = {};
		for (const role of ROLES) next[role] = style.getPropertyValue(role).trim();
		values = next;
	}

	$effect(() => {
		readValues();
		const media = matchMedia('(prefers-color-scheme: dark)');
		media.addEventListener('change', readValues);
		return () => media.removeEventListener('change', readValues);
	});

	function chooseTheme(next: Theme) {
		theme = next;
		applyTheme(next);
		readValues();
	}

	const TYPE = [
		['--text-hero', '44', 'Aspire'],
		['--text-3xl', '34', 'Dům u lesa'],
		['--text-2xl', '28', 'Síň slávy'],
		['--text-xl', '22', 'Rok v Japonsku'],
		['--text-lg', '17', 'Vzhled'],
		['--text-base', '15', 'Jedna věta, kterou uvidíš každý den.'],
		['--text-md', '14', 'Fotka z telefonu, název a proč.'],
		['--text-sm', '13', 'podle systému'],
		['--text-xs', '12', 'první sen'],
		['--text-2xs', '11', 'Nástěnka']
	] as const;

	const SPACE = [1, 2, 3, 4, 5, 6, 7, 8] as const;
</script>

<svelte:head>
	<title>Aspire — styleguide</title>
</svelte:head>

<main class="page guide">
	<header class="head">
		<h1 class="title">Styleguide</h1>
		<div class="seg seg--soft" role="group" aria-label="Motiv">
			{#each THEMES as option (option.value)}
				<button
					type="button"
					class="seg__item"
					aria-pressed={theme === option.value}
					onclick={() => chooseTheme(option.value)}
				>
					{option.label}
				</button>
			{/each}
		</div>
	</header>

	<!-- ── colour ─────────────────────────────────────────────────────── -->
	<h2 class="section">Barvy</h2>
	<div class="swatches">
		{#each ROLES as role (role)}
			<div class="swatch">
				<span class="swatch__chip" style:background="var({role})"></span>
				<span class="swatch__name">{role.slice(2)}</span>
				<span class="swatch__value">{values[role] ?? ''}</span>
			</div>
		{/each}
	</div>

	<div class="dream dream--sky dream--wide">
		<div class="dream__body">
			<h3 class="dream__title dream__title--sm">Jeden přechod, dva akcenty</h3>
			<p class="dream__why">Ember přes růžovou do dusku. Stojí na místě fotky, dokud žádná není.</p>
		</div>
	</div>

	<!-- ── type ───────────────────────────────────────────────────────── -->
	<h2 class="section">Typografie</h2>
	<section class="card">
		<p class="label">Inter 400 / 500 / 600, tabulkové číslice, bez verzálek</p>
		{#each TYPE as [token, px, sample] (token)}
			<div class="type-row">
				<span class="type-row__meta">{px}</span>
				<span class="type-row__sample" style:font-size="var({token})">{sample}</span>
			</div>
		{/each}
	</section>

	<!-- ── buttons ────────────────────────────────────────────────────── -->
	<h2 class="section">Tlačítka</h2>
	<section class="card">
		<p class="label">Každé je pilulka. Pořadí je výplň, ne tvar.</p>
		<div class="actions">
			<button type="button" class="btn btn--primary">Uložit</button>
			<button type="button" class="btn btn--accent"
				><Icon name="plus" size={18} stroke={2} />Přidat sen</button
			>
			<button type="button" class="btn">Upravit</button>
			<button type="button" class="btn btn--quiet">Zrušit</button>
			<button type="button" class="btn btn--danger">Smazat</button>
			<button type="button" class="btn btn--sm">Malé</button>
			<button type="button" class="btn btn--primary" disabled>Nedostupné</button>
		</div>
	</section>
	<div class="ground-row">
		<button type="button" class="btn btn--card">Na ploše</button>
		<button type="button" class="round" aria-label="Zpět"
			><Icon name="chevron-left" size={18} stroke={1.8} /></button
		>
		<button
			type="button"
			class="chip"
			class:chip--on={chip === 'vse'}
			onclick={() => (chip = 'vse')}>Vše</button
		>
		<button
			type="button"
			class="chip"
			class:chip--on={chip === 'sny'}
			onclick={() => (chip = 'sny')}>Sním</button
		>
		<button
			type="button"
			class="chip"
			class:chip--on={chip === 'cesta'}
			onclick={() => (chip = 'cesta')}>Na cestě</button
		>
	</div>

	<!-- ── card and rows ──────────────────────────────────────────────── -->
	<h2 class="section">Karta a řádky</h2>
	<section class="card card--list">
		<a class="row row--press" href="#karta">
			<span class="circle circle--dusk"><Icon name="sparkles" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Dům u lesa</span>
				<span class="row__sub">Ráno s kávou na verandě, ticho.</span>
			</span>
			<span class="row__end"><span class="badge badge--dreaming">sním</span></span>
		</a>
		<a class="row row--press" href="#karta">
			<span class="circle circle--accent"><Icon name="camera" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Rok v Japonsku</span>
				<span class="row__sub">Cíl 2028</span>
			</span>
			<span class="row__end"><span class="badge badge--progress">na cestě</span></span>
		</a>
		<a class="row row--press" href="#karta">
			<span class="circle circle--sky"><Icon name="trophy" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Vlastní firma</span>
				<span class="row__sub">Splněno v březnu</span>
			</span>
			<span class="row__end"><span class="badge badge--achieved">splněno</span></span>
		</a>
	</section>
	<p class="hint">Řádky a jména jsou ukázka, ne skutečné sny.</p>

	<!-- ── controls ───────────────────────────────────────────────────── -->
	<h2 class="section">Ovládání</h2>
	<section class="card">
		<div class="seg seg--soft" role="group" aria-label="Ukázka">
			<button type="button" class="seg__item" aria-pressed="true">Nástěnka</button>
			<button type="button" class="seg__item" aria-pressed="false">Mřížka</button>
		</div>
		<div class="row row--short">
			<span class="row__body">
				<span class="row__title">Ranní připomínka</span>
				<span class="row__sub">Každý den v 7:00</span>
			</span>
			<button
				type="button"
				class="toggle"
				role="switch"
				aria-checked={on}
				aria-label="Ranní připomínka"
				onclick={() => (on = !on)}
			></button>
		</div>
		<label class="field">
			<span class="field__label">Název <span>3 až 120 znaků</span></span>
			<input class="field__input" type="text" placeholder="Dům u lesa" />
		</label>
		<label class="field">
			<span class="field__label">Proč</span>
			<textarea
				class="field__input field__input--area"
				placeholder="Jedna věta, kterou uvidíš každý den."></textarea>
			<span class="field__hint">Krátce. Je to připomínka, ne esej.</span>
		</label>
		<div class="actions">
			<button type="button" class="btn" onclick={() => toast.show('Uloženo')}>Toast</button>
			<button
				type="button"
				class="btn"
				onclick={() =>
					toast.show('Nová verze je připravená', { action: { label: 'Obnovit', run: () => {} } })}
				>Toast s akcí</button
			>
		</div>
	</section>

	<!-- ── the dream tile ─────────────────────────────────────────────── -->
	<h2 class="section">Sen</h2>
	<div class="dream dream--sky">
		<span class="badge dream__tag">sním</span>
		<div class="dream__body">
			<h3 class="dream__title">Dům u lesa</h3>
			<p class="dream__why">Ráno s kávou na verandě, ticho a les za oknem.</p>
		</div>
	</div>
	<div class="dream sample" aria-label="Ukázka snu se stmavlou fotkou">
		<span class="badge dream__tag">na cestě · 2028</span>
		<div class="dream__body">
			<h3 class="dream__title">Rok v Japonsku</h3>
			<p class="dream__why dream__say">Rok žiju v Japonsku.</p>
		</div>
	</div>
	<p class="hint">
		Dlaždice 4:5, rohy 20, jediný objekt na ploše se stínem. Pod názvem stojí jedna řádka: afirmace,
		když ji sen má — o stupeň silnější a v plné bílé, jako druhá dlaždice — jinak proč.
	</p>

	<!-- ── the sheet ──────────────────────────────────────────────────── -->
	<h2 class="section">Plátno</h2>
	<section class="card">
		<div class="actions">
			<button type="button" class="btn btn--primary" onclick={() => (sheet = true)}>
				Otevřít plátno
			</button>
		</div>
		<p class="hint">
			Vyjede zdola přes ztmavenou obrazovku, rohy 28, uvnitř stojí karta jako na stránce. Pro
			formulář, který je pochůzka, ne místo — zavírá se únikem, ťuknutím vedle i vlastním tlačítkem.
		</p>
	</section>

	<!-- ── space and radius ───────────────────────────────────────────── -->
	<h2 class="section">Rozměry</h2>
	<section class="card">
		<p class="label">Mřížka 4 px</p>
		<div class="spaces">
			{#each SPACE as n (n)}
				<span class="space" style:width="var(--space-{n})" title="--space-{n}"></span>
			{/each}
		</div>
		<p class="label">Rohy 12 · 16 · 20 · 28 · pilulka</p>
		<div class="radii">
			<span class="radius" style:border-radius="var(--radius-sm)"></span>
			<span class="radius" style:border-radius="var(--radius-md)"></span>
			<span class="radius" style:border-radius="var(--radius-lg)"></span>
			<span class="radius" style:border-radius="var(--radius-xl)"></span>
			<span class="radius" style:border-radius="var(--radius-full)"></span>
		</div>
	</section>
</main>

<TabBar />

<Sheet open={sheet} title="Plátno" onclose={() => (sheet = false)}>
	<section class="card">
		<p class="hint">Karta na podkladu stránky, ve stejném odsazení jako kdekoli jinde.</p>
		<div class="actions actions--fill">
			<button type="button" class="btn btn--quiet" onclick={() => (sheet = false)}>Zrušit</button>
			<button type="button" class="btn btn--accent" onclick={() => (sheet = false)}>Hotovo</button>
		</div>
	</section>
</Sheet>

<style>
	.guide {
		gap: var(--space-3);
	}

	.head {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.section {
		margin: var(--space-4) 0 0;
		font-size: var(--text-lg);
		font-weight: 600;
		letter-spacing: var(--track-body);
	}

	.swatches {
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		gap: var(--space-2);
	}

	.swatch {
		display: flex;
		flex-direction: column;
		gap: 4px;
		padding: var(--space-2);
		border-radius: var(--radius-sm);
		background: var(--surface);
	}

	.swatch__chip {
		display: block;
		height: 40px;
		border-radius: 8px;
		border: 1px solid var(--hairline);
	}

	.swatch__name {
		font-size: var(--text-xs);
		font-weight: 600;
	}

	.swatch__value {
		font-size: var(--text-2xs);
		color: var(--ink-3);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.type-row {
		display: flex;
		align-items: baseline;
		gap: var(--space-3);
		min-width: 0;
	}

	.type-row__meta {
		flex: none;
		width: 2ch;
		font-size: var(--text-xs);
		color: var(--ink-3);
	}

	.type-row__sample {
		min-width: 0;
		font-weight: 600;
		line-height: var(--leading-tight);
		letter-spacing: -0.02em;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.ground-row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-2);
	}

	/* A stand-in for a photograph that has not been taken: a dark, quiet
	   field, so the scrim and the white type can be judged on something
	   that is not the sky. Ukázka, never a dream. */
	.sample {
		background:
			radial-gradient(70% 50% at 70% 20%, rgb(255 255 255 / 14%), transparent 70%),
			linear-gradient(200deg, var(--surface-2), var(--ink) 110%);
	}

	.spaces {
		display: flex;
		align-items: flex-end;
		gap: var(--space-2);
		height: 56px;
	}

	.space {
		display: block;
		height: 100%;
		border-radius: 4px;
		background: var(--signal-wash);
		border: 1px solid var(--signal);
	}

	.radii {
		display: flex;
		gap: var(--space-2);
	}

	.radius {
		display: block;
		width: 56px;
		height: 56px;
		background: var(--surface-3);
		border: 1px solid var(--hairline-2);
	}
</style>
