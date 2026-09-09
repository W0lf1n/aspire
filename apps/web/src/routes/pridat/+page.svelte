<script lang="ts">
	/**
	 * Přidat — the add screen. A stub in M0: it names the three things a
	 * dream needs and where they will come from, and that is all. The form,
	 * the camera and the upload arrive with M1.
	 */
	import AppBar from '$lib/ui/AppBar.svelte';
	import Icon from '$lib/ui/Icon.svelte';
</script>

<svelte:head>
	<title>Aspire — přidat sen</title>
</svelte:head>

<main class="page">
	<AppBar title="Přidat sen" />

	<section class="card card--list">
		<div class="row">
			<span class="circle circle--accent"><Icon name="camera" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Fotka</span>
				<span class="row__sub">Z fotoaparátu, z galerie, nebo odkaz</span>
			</span>
		</div>
		<div class="row">
			<span class="circle circle--dusk"><Icon name="sparkles" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Název</span>
				<span class="row__sub">Pár slov, jak tomu říkáš</span>
			</span>
		</div>
		<div class="row">
			<span class="circle circle--sky"><Icon name="image" size={20} stroke={1.8} /></span>
			<span class="row__body">
				<span class="row__title">Proč</span>
				<span class="row__sub">Jedna věta, kterou uvidíš každý den</span>
			</span>
		</div>
	</section>

	<p class="hint">Přidávání snů přijde s dalším milníkem. Zatím tu je jen tvar obrazovky.</p>
</main>
