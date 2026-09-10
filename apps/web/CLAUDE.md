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

Two rune modules hold app-wide state: `lib/ui/toast.svelte.ts` and
`lib/offline/status.svelte.ts`. The second is the connection flag (D24):
`connection.online` is read by the bar, the board, the forms and a dream's
screen, and every write rests while it is false. The API client sets it
from what each request learned.

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
| `.chip`, `.round`, `.glass`                              | Glass on the ground: a filter chip, a 40 px round button, the surface                                     |
| `.btn`                                                   | Soft pill; `--primary`, `--accent`, `--photo`, `--card`, `--quiet`, `--danger`, `--sm`, `--lg`, `--block` |
| `.toggle`, `.field`, `.facts`, `.well`, `.hint`, `.link` | The rest of the primitives                                                                                |

## Routes

| Route                 | Screen                                                        |
| --------------------- | ------------------------------------------------------------- |
| `/`                   | Nástěnka. The reel: a tile a screen, the day's pick first     |
| `/pridat`             | Přidat sen. The dream's form (`DreamForm`) with an ember pill |
| `/sen/[id]`           | One dream: the tile, the facts, the heart, Upravit and Smazat |
| `/sen/[id]/upravit`   | The same form with the saved values; back is the dream        |
| `/sin-slavy`          | Síň slávy. The achieved dreams, the most recent first         |
| `/nastaveni`          | The hub: Vzhled, Párování, the version                        |
| `/nastaveni/vzhled`   | systém / světlý / tmavý                                       |
| `/nastaveni/parovani` | The code and a device name; paired, Odpojit                   |
| `/styleguide`         | Tokens and components, both themes. Unlinked                  |

The bar is four slots: Nástěnka · ⊕ · Síň slávy · Nastavení. `lib/ui/nav.ts`
decides which one a path lights and has the test.

## Testing

Vitest, node environment, `requireAssertions: true`. `nav.test.ts`,
`settings.test.ts`, `api/pairing.test.ts`, `dreams/rules.test.ts`,
`dreams/board.test.ts` (the reel, the daily pick, the tile's line),
`images/downscale.test.ts` and
`offline/cache.test.ts`; a new rule gets a test before it gets a screen, and
it lives in a `.ts` module the component imports, never in the component.
