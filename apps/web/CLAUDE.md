# CLAUDE.md — apps/web

The SvelteKit PWA. Read the root `CLAUDE.md` first; this file covers only
what is specific to the app.

**Stack:** SvelteKit 2 · Svelte 5, runes forced · Vite 8 · TypeScript strict ·
Vitest 4 · `adapter-static`. No runtime dependency.

## Configuration lives in `vite.config.ts`

There is no `svelte.config.js`. The SvelteKit plugin is configured inline:
runes forced outside `node_modules`, `adapter-static` with `fallback:
'200.html'` and `precompress: true`, a dev proxy from `/api` to
`127.0.0.1:5300`, and Vitest's config in the same file.

`routes/+layout.ts` sets `ssr = false` and `prerender = true`: the board is
fetched with the device's token, which lives on the device, so a server has
nothing to render. `routes/sen/+layout.ts` turns prerendering off again for
a dream's screens: an id in the path has nothing to prerender, and the
shell's `200.html` carries them.

## Svelte 5, as used here

Runes only: `$state`, `$derived`, `$props`, `$effect`. No `export let`, no
stores. Anything that must happen "when the app opens" belongs in
`routes/+layout.svelte`'s effects: the status bar colour, the update watcher,
the connection watcher, the splash.

Three rune modules hold app-wide state: `lib/ui/toast.svelte.ts`,
`lib/offline/status.svelte.ts` and `lib/ui/update.svelte.ts`. The second is
the connection flag (D24): `connection.online` is read by the bar, the board,
the forms and a dream's screen, and every write rests while it is false. The
API client sets it from what each request learned. The third is whether a new
build has taken over — `update.ready`, read by the toast that stays until it
is tapped and by Nastavení's _Obnovit aplikaci_ (D42).

## The design system

`lib/styles/tokens.css` is the only place a colour exists; `lib/styles/app.css`
owns the primitives. `DESIGN.md` beside this file is the system as built.
Two accents, `--signal` (ember, acts) and `--dusk` (marks), one gradient
`--dawn`, and `--photo-ink` over `--scrim` for type on a photograph.

| Class                                                    | What it is                                                                                                |
| -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `.page`, `.title`                                        | The one scroll region of a tab screen; its name at 28 px in the flow                                      |
| `.card`, `.card--press`, `--list`                        | White on the ground, 20 px corners; pressable; a card of rows                                             |
| `.dream`, `.dream--sky`, `--wide`                        | A photograph, 4:5, with `.dream__body` at its foot over the scrim                                         |
| `.dream__say`                                            | The affirmation in the why's place on a tile: 500, full white                                             |
| `.row`, `.row__body`, `.row__end`                        | The list row: circle · title over sub · end slot                                                          |
| `.circle`                                                | 40 px identity circle; `--accent`, `--dusk`, `--sky`; `--sm` `--xs` `--lg`                                |
| `.badge`                                                 | 12 px pill: `--dreaming`, `--progress`, `--achieved`, `--photo`, `--tiny`                                 |
| `.seg`, `.seg__item`                                     | The segmented pill, `aria-pressed` selects; `--soft` inside a card                                        |
| `.chip`, `.chip--soft`, `.round`, `.glass`               | Glass on the ground: a filter chip (`--soft` inside a card), a 40 px round button, the surface            |
| `.btn`                                                   | Soft pill; `--primary`, `--accent`, `--photo`, `--card`, `--quiet`, `--danger`, `--sm`, `--lg`, `--block` |
| `.sheet`, `.sheet__panel`                                | A native `<dialog>` rising from the bottom edge; `Sheet.svelte` is its behaviour                          |
| `.toggle`, `.field`, `.facts`, `.well`, `.hint`, `.link` | The rest of the primitives                                                                                |

## Routes

| Route                   | Screen                                                                                                                                                                         |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `/`                     | Nástěnka. The reel, a pager: one full-bleed dream per swipe, the day's pick then a shuffle, five at a time. A tile with no photograph carries the pill that gives it one (D52) |
| `/seznam`               | Seznam. Every dream as a line, newest first; ＋ opens a sheet of five fields (D44)                                                                                             |
| `/pridat`               | Přidat sen. The dream's form (`DreamForm`) with an ember pill                                                                                                                  |
| `/sen/[id]`             | One dream: the tile, the facts, the heart, Upravit and Smazat                                                                                                                  |
| `/sen/[id]/upravit`     | The same form with the saved values; back is the dream                                                                                                                         |
| `/sin-slavy`            | Síň slávy. The achieved dreams, the most recent first; the pair                                                                                                                |
| `/nastaveni`            | The hub: Vzhled, Upozornění, Tapeta, Stahování, Párování, the version and _Obnovit aplikaci_                                                                                   |
| `/nastaveni/upozorneni` | Upozornění. Off / daily / weekdays, and the hour                                                                                                                               |
| `/nastaveni/tapeta`     | Tapeta. Up to six dreams onto a lock-screen collage                                                                                                                            |
| `/nastaveni/vzhled`     | systém / světlý / tmavý                                                                                                                                                        |
| `/nastaveni/stahovani`  | How much of the board is kept offline: co prolistuješ / na wifi / vždy celá, and what it takes up                                                                              |
| `/nastaveni/parovani`   | The code and a device name; paired, Odpojit                                                                                                                                    |
| `/styleguide`           | Tokens and components, both themes. Unlinked                                                                                                                                   |

The bar is five slots: Nástěnka · Seznam · ⊕ · Síň slávy · Nastavení, and
`--slots` on `.tabbar` is the one number they are worked out from.
`lib/ui/nav.ts` decides which one a path lights and has the test — a dream's
own screens light Nástěnka whichever list was used to reach them.

`lib/ui/pager.ts` is the reel's paging (D40): CSS does the snapping and the
momentum, and the pager fences a gesture to one page either side of where it
began, answers a wheel gesture once, and moves the arrow and page keys one
dream. The board reads the dream on the screen back from it — that is what
grows the window and what keeps the neighbouring photographs eager.

## Testing

Vitest, node environment, `requireAssertions: true`. `nav.test.ts`,
`settings.test.ts`, `api/pairing.test.ts`, `dreams/rules.test.ts` (what a
dream may be, and its line in the Seznam), `dreams/board.test.ts` (the reel
and its shuffle, the daily pick, the area filter, the Seznam's order, the
tile's line, the anniversary), `ui/pager.test.ts` (the paging
arithmetic: the fence, the wheel's reach in three delta modes, the keys, the
glide's curve) and `ui/pager.wiring.test.ts` (the pager itself, on a fake
scroll region and a frame queue the test turns by hand), `dreams/format.test.ts`,
`dreams/photos.test.ts` (which of a dream's two photographs a screen shows),
`dreams/upload.test.ts` (the order a photograph is replaced in: out before
in, only its own kind, and what happens when the sizes never come),
`dreams/wallpaper.test.ts` (who can be on a collage, and how big it is),
`push/schedule.test.ts` (the nudge's time both ways, and why the nudge
cannot be offered here — browser, server, permission, in that order),
`images/downscale.test.ts`, `offline/cache.test.ts` (what is kept, in what
order, a few at a time, the window ahead of the thumb and the ceiling),
`offline/shell.test.ts` (which shell caches a new build may throw away, and
which one it must keep) and `offline/policy.test.ts` (how much of the board a device keeps, and what an
unreadable connection counts as); a new rule gets a test before it gets a
screen, and it lives in a `.ts` module the component imports, never in the
component.

**The hidden Browser pane produces no frames.** Verifying the reel's
windowing there failed and looked like a bug: with the pane hidden
`requestAnimationFrame` never fires, and so no `IntersectionObserver` fires
either — a fresh observer on an element plainly in view reported nothing.
Emulating a phone viewport gives the page a real height but not a rendering
loop. Anything that depends on an observer or an animation frame needs the
pane visible, or a unit test on the arithmetic instead (`aheadOf`).

**Scroll events are part of that**, which is the same trap one turn further
on. The pager's fence is checked on `scroll`, and with no frames a `scrollTop`
set from the console dispatches nothing at all, so a fence that works looks
exactly like a fence that does not. `document.visibilityState` says `visible`
the whole time and is no help. `pager.wiring.test.ts` is the answer: a fake
element that moves the way a browser moves one, with the frames as an array.
Layout _is_ readable there — `getBoundingClientRect`, `offsetTop`,
`getComputedStyle` and a screenshot all work — so geometry can be checked in
the pane and behaviour cannot.
