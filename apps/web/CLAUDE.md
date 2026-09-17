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

Five rune modules hold app-wide state: `lib/ui/toast.svelte.ts`,
`lib/offline/status.svelte.ts`, `lib/ui/update.svelte.ts`,
`lib/offline/writes.svelte.ts` (the `use:writes` action — a control that
writes wears it instead of remembering the connection, D67) and
`lib/dreams/deleting.svelte.ts` (the few seconds a deleted dream is held
before the request goes, D65). The second is
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

| Class                                                                | What it is                                                                                                                                                                                  |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `.page`, `.title`                                                    | The one scroll region of a tab screen; its name at 28 px in the flow                                                                                                                        |
| `.card`, `.card--press`, `--list`                                    | White on the ground, 20 px corners; pressable; a card of rows                                                                                                                               |
| `.dream`, `.dream--sky`, `--wide`                                    | A photograph, 4:5, with `.dream__body` at its foot over the scrim; `photoStyle` says where it is looked at (D54); `.dream__under` is the thumb blurred under the reel's picture (D63)       |
| `.dream__say`                                                        | The affirmation in the why's place on a tile: 500, full white                                                                                                                               |
| `.row`, `.row__body`, `.row__end`                                    | The list row: circle · title over sub · end slot                                                                                                                                            |
| `.circle`                                                            | 40 px identity circle; `--accent`, `--dusk`, `--sky`; `--sm` `--xs` `--lg`                                                                                                                  |
| `.badge`                                                             | 12 px pill: `--dreaming`, `--progress`, `--achieved`, `--photo`, `--tiny`                                                                                                                   |
| `.seg`, `.seg__item`                                                 | The segmented pill, `aria-pressed` selects; `--soft` inside a card                                                                                                                          |
| `.chip`, `.chip--soft`, `.round`, `.glass`                           | Glass on the ground: a filter chip (`--soft` inside a card), a 40 px round button, the surface                                                                                              |
| `.btn`                                                               | Soft pill; `--primary`, `--accent`, `--photo`, `--card`, `--quiet`, `--danger`, `--sm`, `--lg`, `--block`                                                                                   |
| `.sheet`, `.sheet__panel`, `.sheet__handle`, `.sheet__body`          | A native `<dialog>` rising from the bottom edge, pulled down by its handle to put it away (D76); `Sheet.svelte` is its behaviour                                                            |
| `.stats`, `.stats__item`                                             | The board counted: four figures over their words, each a button that narrows the list (D77)                                                                                                 |
| `.crop`, `.crop__frame`, `.crop__top`, `.crop__words`, `.crop__acts` | The editor's page: a full-screen `<dialog>` on the reel's own shape, the words and pills above its scrim; `CropEditor` and `CollageEditor` stand on it (D54, D85)                           |
| `.swipe`, `.swipe__face`, `.swipe__tray`                             | A row that slides aside to a tray of three; `--lifted` is the row in the hand. Every one exactly as tall as the next (D78, D79)                                                             |
| `.row__link`, `.row__grip`, `.row__no--press`                        | A row whose dream is a link _beside_ its controls: the grip it is dragged by, the number that is a field (D79)                                                                              |
| `.dream__collage`, `.dream__cell`                                    | Two to five photographs as one picture cut into cells, edge to edge in its tile; the grid arrives inline from the template (D82); a cell fills or holds its photograph whole on a mat (D88) |
| `.toggle`, `.field`, `.facts`, `.well`, `.hint`, `.link`             | The rest of the primitives                                                                                                                                                                  |

## Routes

| Route                    | Screen                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/`                      | Nástěnka (the tile is `ui/ReelTile.svelte`). Two reels behind a segment — Vše, a pager of everything ahead with the day's pick then a shuffle, and Teď, the ten in his own order (D53). Five tiles at a time; a tile with no photograph carries the pill that gives it one (D52); the heart is a glass circle with no number, filled on a dream that has been fuelled (D58); a dream with several photographs is a row swiped across, and past its last photograph the swipe is the other reel (D87) |
| `/seznam`                | Seznam. Every dream as a line, in his own order — dragged by its grip, or by typing over its number (D79) — counted above (D77), narrowed to Teď by a Vše · Teď segment (D83), searched below (D49); a line swiped left shows its hearts, Sdílet and Smazat (D78, `ui/DreamRow.svelte`); ＋ opens a sheet of five fields (D44)                                                                                                                                                                       |
| `/ted`                   | Teď. The ten on the second reel, numbered, ordered with arrows; ＋ opens a sheet of the rest (D53)                                                                                                                                                                                                                                                                                                                                                                                                   |
| `/pridat`                | Přidat sen. The dream's form (`DreamForm`) with an ember pill; `?url=` fills the link sheet, which is where a shared pin lands (D56); up to five photographs in one pick (D84)                                                                                                                                                                                                                                                                                                                       |
| `/sen/[id]`              | One dream: the tile — up to five photographs in its first pick (D84), and from two up a carousel of the collage and then each photograph, or the photographs alone (D86, D87), placed in `CollageEditor` with a cell whole on its mat if wanted (D85, D88) — the shelf of its photographs, the facts with „Naposledy upraveno“ (D77), the heart, Upravit and Smazat                                                                                                                                  |
| `/sen/[id]/upravit`      | The same form with the saved values; back is the dream                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| `/sin-slavy`             | Síň slávy. The achieved dreams, the most recent first; the pair                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `/nastaveni`             | The hub: Vzhled, Upozornění, Tapeta, Stahování, Párování, Začít znovu, the version and _Obnovit aplikaci_                                                                                                                                                                                                                                                                                                                                                                                            |
| `/nastaveni/upozorneni`  | Upozornění. Off / daily / weekdays, and one to five hours, a row each (D72)                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `/nastaveni/tapeta`      | Tapeta. Up to six dreams onto a lock-screen collage                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| `/nastaveni/vzhled`      | systém / světlý / tmavý                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| `/nastaveni/stahovani`   | How much of the board is kept offline: co prolistuješ / na wifi / vždy celá, and what it takes up                                                                                                                                                                                                                                                                                                                                                                                                    |
| `/nastaveni/parovani`    | The code and a device name; paired, Odpojit                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `/nastaveni/zacit-znovu` | Začít znovu. What goes and what stays, how many dreams it is about to take, and a sheet that asks for „začínám znovu“ typed out (D80)                                                                                                                                                                                                                                                                                                                                                                |
| `/styleguide`            | Tokens and components, both themes. Unlinked                                                                                                                                                                                                                                                                                                                                                                                                                                                         |

The bar is five slots: Nástěnka · Seznam · ⊕ · Síň slávy · Nastavení, and
`--slots` on `.tabbar` is the one number they are worked out from.
`lib/ui/nav.ts` decides which one a path lights and has the test — a dream's
own screens and `/ted` light Nástěnka, whichever list was used to reach them.

`lib/ui/pager.ts` is the reel's paging (D40): CSS does the snapping and the
momentum, and the pager fences a gesture to one page either side of where it
began, answers a wheel gesture once, and moves the arrow and page keys one
dream. The board reads the dream on the screen back from it — that is what
grows the window and what keeps the neighbouring photographs eager.

## Testing

Vitest, `requireAssertions: true`, in two projects: **server** in node for
the rules, and **client** in happy-dom for `*.svelte.test.ts`, which mounts a
component with Svelte's own `mount` and `flushSync` (D68). The client project
needs `resolve.conditions: ['browser']` or Svelte hands back its server
build, whose `mount` exists only to say it is not the browser.

`ui/ReelTile.svelte.test.ts` is the first of the component tests: the sky
with its pill, the sky that says the photograph is being made, the picture
with its thumb under it, the preview before the server has it, the heart lit
only once fuelled and carrying no number, the heart and the file input dying
without a signal, and the badge and the link. A component test checks what
only a component can get wrong; the rule behind it keeps its own test.

The node project's files: `nav.test.ts`,
`settings.test.ts`, `api/pairing.test.ts`, `dreams/rules.test.ts` (what a
dream may be, and its line in the Seznam), `dreams/board.test.ts` (the reel
and its shuffle, the daily pick and the fuel that decides it, the area filter,
the Seznam's order, the tile's line, the anniversary), `ui/pager.test.ts` (the paging
arithmetic: the fence, the wheel's reach in three delta modes, the keys, the
glide's curve) and `ui/pager.wiring.test.ts` (the pager itself, on a fake
scroll region and a frame queue the test turns by hand), `dreams/format.test.ts`,
`dreams/photos.test.ts` (which of a dream's two photographs a screen shows,
and the style that says where it is looked at),
`images/focal.test.ts` (a drag and a pinch into a point and a zoom, and the
axis there is nothing more of to see),
`dreams/upload.test.ts` (the order a photograph is replaced in: out before
in, only its own kind, and what happens when the sizes never come),
`dreams/focus.test.ts` (who is on Teď and in what order, and that the ten
are full at ten),
`dreams/wallpaper.test.ts` (who can be on a collage, which six it starts
with, the link the morning automation is given, and how big it is),
`dreams/share.test.ts` (a shared dream's whole link, and what the message
beside it may say),
`push/schedule.test.ts` (the nudge's time both ways, the list of up to five
and what adding, moving and removing one does to it, and why the nudge
cannot be offered here — browser, server, permission, in that order),
`ui/sideways.test.ts` (what counts as a swipe across the reel, and the step
that stops at either end),
`images/downscale.test.ts`, `offline/cache.test.ts` (what is kept, in what
order — the thumb before the picture — at which rung, a few at a time, the
window ahead of the thumb and the ceiling), `dreams/photos.test.ts` (which
photograph a screen shows, and which rung the reel reads on this screen, D62),
`offline/shell.test.ts` (which shell caches a new build may throw away, and
which one it must keep) and `offline/policy.test.ts` (how much of the board a device keeps, and what an
unreadable connection counts as), `dreams/deleting.test.ts` (the undo
window: hidden at once, sent when it closes, still hidden afterwards, and
never sent twice) and `offline/writes.test.ts` (what locks a control that
writes, and the sentence a form shows), `ui/pull.test.ts` (how far a sheet
is pulled before it goes, D76), `ui/swipe.test.ts` (when a finger's first
pixels are a swipe and when they are the list being scrolled, and whether the
tray stays open, D78), `ui/reorder.test.ts` (which line a dragged row lands
on, which rows step aside, and how fast the list scrolls near an edge, D79),
`dreams/order.test.ts` (a dream moved to a line, the board counted off from
nought, and the line to ask the server for while a deletion is still held),
`dreams/stats.test.ts` (the board counted), `dreams/collage.test.ts` (three
templates for every count, each tiling its grid with no hole and no overlap,
D82), `dreams/reset.test.ts` (the phrase, and the three ways Czech counts
dreams), `images/focal.test.ts` also for a photograph shown whole — the room
it floats in, and which way the point runs under a finger (D81) — and whether two crops show a photograph the same way, `sameCrop` (D88),
`dreams/slides.test.ts` (the collage and each photograph, or the photographs alone, and nothing for one, D87),
`ui/placing.ts`'s test (a drag against the finger, a pinch, a lifted finger that does not jump, D85),
`ui/carousel.test.ts` (which slide is showing, where a dot or an arrow scrolls to, and how far a row can still go before a swipe is the reel's, D86, D87), and the D83–D84
cases in `dreams/focus.test.ts` (the Seznam narrowed to Teď), `dreams/upload.test.ts` (several
photographs sent in order and waited for once, a refusal halfway, and the ones that fit) and
`images/downscale.test.ts` (a file that is not a photograph costs only itself); a new rule gets a test before it gets
a screen, and it lives in a `.ts` module the component imports, never in the
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
