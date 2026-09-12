---
name: Aspire
description: A private wall of photographs in the pocket. Prosper's grammar, warmed and given a sky.
colors:
  ground: "#f6f2ee"
  surface: "#ffffff"
  surface-pressed: "#f4efe9"
  surface-soft: "#efe9e2"
  hairline: "#e6dfd6"
  hairline-strong: "#d1c8bd"
  ink: "#211c19"
  ink-2: "#5d554e"
  ink-3: "#7d746b"
  pill: "#211c19"
  pill-ink: "#ffffff"
  ember: "#cc4a1c"
  ember-ink: "#ffffff"
  ember-wash: "rgb(204 74 28 / 12%)"
  dusk: "#5a4fd6"
  dusk-ink: "#ffffff"
  dusk-wash: "rgb(90 79 214 / 12%)"
  danger: "#d93a4a"
  danger-wash: "rgb(217 58 74 / 10%)"
  sky-1: "#e5602a"
  sky-2: "#d84b7a"
  sky-3: "#5a4fd6"
  photo-ink: "#ffffff"
  photo-ink-2: "rgb(255 255 255 / 78%)"
  knob: "#ffffff"
  glass: "rgb(255 255 255 / 72%)"
  glass-edge: "rgb(255 255 255 / 60%)"
  dark-ground: "#141210"
  dark-ground-2: "#1a1715"
  dark-surface: "#1f1b19"
  dark-surface-pressed: "#292421"
  dark-surface-soft: "rgb(255 255 255 / 8%)"
  dark-hairline: "rgb(255 255 255 / 12%)"
  dark-ink: "#f7f2ee"
  dark-ink-2: "rgb(247 242 238 / 74%)"
  dark-ink-3: "rgb(247 242 238 / 52%)"
  dark-pill: "#f7f2ee"
  dark-pill-ink: "#141210"
  dark-ember: "#ff8a55"
  dark-ember-ink: "#141210"
  dark-dusk: "#a49cff"
  dark-dusk-ink: "#141210"
  dark-glass: "rgb(31 27 25 / 72%)"
  dark-glass-edge: "rgb(255 255 255 / 10%)"
typography:
  display:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "2.75rem"
    fontWeight: 600
    lineHeight: 1
    letterSpacing: "-1.3px"
  headline:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1.75rem"
    fontWeight: 600
    lineHeight: 1.35
    letterSpacing: "-0.5px"
  title:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "2.125rem"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "-0.8px"
  title-sm:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1.375rem"
    fontWeight: 600
    lineHeight: 1.1
    letterSpacing: "-0.4px"
  subtitle:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 600
    lineHeight: 1.35
    letterSpacing: "0.1px"
  body:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.9375rem"
    fontWeight: 400
    lineHeight: 1.35
    letterSpacing: "0.1px"
  body-sm:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "0.1px"
  label:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.8125rem"
    fontWeight: 400
    lineHeight: 1.35
    letterSpacing: "0.24px"
  badge:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.35
    letterSpacing: "0.1px"
  tab:
    fontFamily: "Inter, system-ui, sans-serif"
    fontSize: "0.6875rem"
    fontWeight: 600
    lineHeight: 1
    letterSpacing: "0.1px"
rounded:
  sm: "12px"
  md: "16px"
  lg: "20px"
  xl: "28px"
  full: "9999px"
spacing:
  "1": "4px"
  "2": "8px"
  "3": "12px"
  "4": "16px"
  "5": "24px"
  "6": "32px"
  "7": "40px"
  "8": "56px"
components:
  button:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.ink}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "40px"
  button-active:
    backgroundColor: "{colors.hairline}"
  button-primary:
    backgroundColor: "{colors.pill}"
    textColor: "{colors.pill-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.full}"
    padding: "0 24px"
    height: "48px"
  button-accent:
    backgroundColor: "{colors.ember}"
    textColor: "{colors.ember-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.full}"
    padding: "0 24px"
    height: "48px"
  button-photo:
    backgroundColor: "rgb(255 255 255 / 18%)"
    textColor: "{colors.photo-ink}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "40px"
  button-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "40px"
  button-quiet:
    backgroundColor: "transparent"
    textColor: "{colors.ink-2}"
    rounded: "{rounded.full}"
    height: "40px"
  button-danger:
    backgroundColor: "transparent"
    textColor: "{colors.danger}"
    rounded: "{rounded.full}"
    height: "40px"
  button-sm:
    rounded: "{rounded.full}"
    typography: "{typography.label}"
    padding: "0 14px"
    height: "32px"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.lg}"
    padding: "16px"
  card-pressed:
    backgroundColor: "{colors.surface-pressed}"
  card-list:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.lg}"
    padding: "4px 16px"
  well:
    backgroundColor: "{colors.ground}"
    rounded: "{rounded.sm}"
    padding: "14px 16px"
  dream:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.photo-ink}"
    rounded: "{rounded.lg}"
    padding: "24px 16px 16px"
  chip:
    backgroundColor: "{colors.glass}"
    textColor: "{colors.ink}"
    typography: "{typography.label}"
    rounded: "{rounded.full}"
    padding: "0 14px"
    height: "40px"
  chip-on:
    backgroundColor: "{colors.pill}"
    textColor: "{colors.pill-ink}"
  badge:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.ink-2}"
    typography: "{typography.badge}"
    rounded: "{rounded.full}"
    padding: "4px 10px"
  badge-dreaming:
    backgroundColor: "{colors.dusk-wash}"
    textColor: "{colors.dusk}"
  badge-progress:
    backgroundColor: "{colors.ember-wash}"
    textColor: "{colors.ember}"
  badge-achieved:
    textColor: "{colors.photo-ink}"
  badge-tag:
    backgroundColor: "rgb(0 0 0 / 28%)"
    textColor: "{colors.photo-ink}"
  segment:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.full}"
    padding: "3px"
  segment-item:
    textColor: "{colors.ink-2}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.full}"
    padding: "0 16px"
    height: "34px"
  segment-item-on:
    backgroundColor: "{colors.pill}"
    textColor: "{colors.pill-ink}"
  input:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.sm}"
    padding: "0 16px"
    height: "48px"
  round:
    backgroundColor: "{colors.glass}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    size: "40px"
  circle:
    backgroundColor: "{colors.surface-soft}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    size: "40px"
  tabbar:
    backgroundColor: "{colors.glass}"
    textColor: "{colors.ink-3}"
    typography: "{typography.tab}"
    rounded: "{rounded.full}"
    padding: "0 8px"
    height: "62px"
  tabbar-disc:
    backgroundColor: "{colors.ember}"
    textColor: "{colors.ember-ink}"
    rounded: "{rounded.full}"
    size: "48px"
  toast:
    backgroundColor: "{colors.glass}"
    textColor: "{colors.ink}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.full}"
    padding: "14px 20px"
    height: "48px"
---

# Design System: Aspire

Recorded from the built M0 (2026-09-09) at `src/lib/styles/tokens.css` and
`src/lib/styles/app.css`. Those two files are the system; this file describes
them. When they disagree, the files are right. Aspire is Prosper's sibling
and shares its grammar (the 4 px grid, the type ladder, the radii, every
button a pill, elevation by luminance, the floating glass bar, Inter
400–600, no uppercase); what follows names what is shared and what is
Aspire's own.

## Overview

**Creative North Star: "A Wall of Photographs"**

Aspire is a private dreamboard: a wall of the person's own pictures, each
with a title and one line saying why. The interface is the wall, not the
pictures. Its ground is warm linen with a hint of rose, its ink is warm, and
its cards are white raised by nothing but luminance. Every control is a pill
or a circle. There are exactly two accents and one gradient: ember acts,
dusk marks, and the sunrise gradient between them stands in for a photograph
wherever there is not one yet, so the empty board already looks like the
board.

Density is a phone's: one column at most 34 rem wide, 16 px gutters, 12 px
between cards, and a frosted bar floating over the bottom edge. Nothing in
the chrome competes with a picture. The dark theme is not an inversion but a
different room, a print on a dark wall: charcoal ground, graphite cards, the
two accents lifted to lit hues with dark ink on them, and two faint coloured
lights behind the glass.

**Key Characteristics:**
- Warm linen ground and warm ink in the light; warm charcoal in the dark
- Two accents only: ember (`--signal`) acts, dusk (`--dusk`) marks
- One gradient (`--dawn`) that is theme-independent, like a photograph
- White type over a bottom scrim on every photograph, in both themes
- Elevation by luminance; the dream tile is the one object that casts a shadow
- Every button a pill; every identity a circle; Inter 400/600, sentence case
- One colour source: a literal hex outside `tokens.css` is a bug

## Colors

Warm neutrals do the work; colour is reserved for the two accents, the sky,
and the photographs themselves.

### Primary
- **Ember** (`--signal`, `{colors.ember}`): the accent that acts. The add disc
  in the bar, links, the caret, the focus ring, text selection wash, the
  toggle when on, the "in progress" badge, and `.btn--accent` for the one
  action a screen is about. In the dark it lifts to `{colors.dark-ember}`
  and takes dark ink, the way a lit sun does.

### Secondary
- **Dusk** (`--dusk`, `{colors.dusk}`): the accent that marks and never acts.
  The "dreaming" badge, the dusk circle, and the far end of the sky. In the
  dark it lifts to `{colors.dark-dusk}` with dark ink.

### Tertiary
- **The Sky** (`--dawn`): the one gradient, three stops at 160°, ember
  (`sky-1`) through rose (`sky-2`, the stop that keeps sRGB from going brown
  in the middle) to dusk (`sky-3`). Theme-independent, because a sunrise does
  not change with the lights. It appears at scale in exactly three places: the
  empty board's tile, the "achieved" badge, and the sky circle.
- **Danger** (`{colors.danger}`): the outline of a destroying pill and error
  text. Not a data colour; there is no data colour.

### Neutral
- **Linen** (`--ground`): the page ground, and the well inside a card
  (`--ground-2` is the same value in the light theme; a step lighter than the
  ground in the dark).
- **Surface** (`--surface`): a card. **Pressed** (`--surface-2`): the card or
  row under a thumb. **Soft** (`--surface-3`): chips inside a card, badges,
  inputs, the default pill, a circle with no colour of its own.
- **Hairline** (`--hairline`): the rule between rows and facts, and the desktop
  column's edges. **Strong** (`--hairline-2`): the toggle's off track.
- **Ink, ink-2, ink-3**: text, secondary text (labels, subs, hints, the
  unselected segment), and tertiary (placeholders, chevrons, resting tabs).
- **Pill / pill-ink**: the primary pill and the chosen segment or chip. Ink on
  white by day, warm white on charcoal by night; the one pair that inverts.
- **Photo-ink / photo-ink-2** over **scrim**: type on a photograph is white,
  and 78 % white for the why, over a bottom scrim (transparent to 58 % black
  from 38 % down). Both themes, always. Two longer ramps join it:
  **scrim-tall** for a photograph the height of a screen, where the words sit
  a sixth of the way up and `--scrim` has barely begun, and **scrim-top**,
  the only scrim that runs the other way — 32 % black fading out by a third —
  under chrome that floats on a photograph rather than above it.
- **Glass / glass-edge**: the frosted surface and its 1 px edge. Light: white
  at 72 %; dark: graphite at 72 %. The bar adds a rim gradient (`--glass-shine`,
  `--glass-rim`) and a lens (`--glass-lens`) under the current tab.

### Named Rules
**The Ember Acts, Dusk Marks Rule.** Ember goes only on things that do
something. Dusk goes only on things that say what state something is in.
Neither is decoration, and there is no third accent.

**The One Sky Rule.** `--dawn` is the only gradient. It stands in for a
photograph and is allowed at scale only where a photograph would be; it does
not tint chrome, cards or backgrounds.

**The Scrim Rule.** Type on an image is `--photo-ink` over `--scrim`, bottom
only, in both themes. The top of a photograph stays a photograph — with one
named exception, the reel, where the chrome has nowhere else to be and wears
`--scrim-top` for it (D40). A card, a pair or a tile in a list never does.

**The One Hex Rule.** Every colour lives in `tokens.css`. The single
exception is the ground, written once more in `app.html`'s `theme-color` and
the manifest, which cannot take a custom property.

## Typography

**Display Font:** Inter (with system-ui)
**Body Font:** Inter (same face)

**Character:** One family, self-hosted as a variable face (400–600, latin and
latin-ext for Czech), tabular figures everywhere. Large sizes tighten as they
grow, from −0.4 px at 22 to −1.3 px at 44. Everything is sentence case.

### Hierarchy
- **Display** (600, 44 px, line 1, −1.3 px): the wordmark "Aspire" at the top
  of the board, in the flow, scrolling away with the tile.
- **Title** (600, 34 px, line 1.1, −0.8 px, balanced wrap): a dream's title on
  its tile on the board. **Title-sm** (22 px, −0.4 px) is the same title on a
  tile in a grid.
- **Headline** (600, 28 px, −0.5 px): a tab screen's name (`.title`), in the
  flow rather than in a bar.
- **Subtitle** (600, 17 px): the back header's title on a detail screen.
- **Body** (400, 15 px, line 1.35, +0.1 px): body copy, row titles (600),
  inputs, the primary pill's label.
- **Body-sm** (400, 14 px, line 1.5): the why on a tile (in `photo-ink-2`,
  max 32ch) and the affirmation in its place (500, full white), segment
  labels (600), the default pill's label (600), facts, the toast.
- **Label** (400, 13 px, +0.24 px): card labels, field labels, row subs,
  hints, chips (600), links (600). Never uppercase.
- **Badge** (600, 12 px): a status pill. **Tab** (600, 11 px): the bar's
  labels.

### Named Rules
**The Two-Weight Rule.** The face is loaded at 400–600, but the build sets
only 400 and 600. 500 is available and unused; do not introduce it without
a reason, and never 700.

**The Sentence Case Rule.** No `text-transform: uppercase` anywhere; the
label tracking (+0.24 px) is the only thing that distinguishes a label.

## Layout

The app is a phone-shaped column, at most 34 rem wide, centred, the height
of the dynamic viewport, and the window itself never scrolls. Every screen
owns exactly one scroll region (`.page`): a column of cards with a 16 px
gutter, 12 px between cards, 12 px plus the safe-area inset at the top, and
enough room at the foot to scroll the last row clear of the bar (bar lift +
62 px + 32 px on a screen with a bar; 24 px plus the inset on one without).
Above 35 rem the column shows its edges as a hairline on each side and is
otherwise unchanged: desktop is the phone layout with room around it. That
is deliberate and now decided — the wide masonry board PLAN.md §3.2 asked
for is dropped, because a grid of thumbnails is a gallery and this app is a
queue (D38). There is no breakpoint at which a screen changes its shape.

The spacing scale is a 4 px grid: 4, 8, 12, 16, 24, 32, 40, 56. Rows are
64 px tall (52 short), a hub row's pressable area bleeds to the card's edge.
Touch targets are 44 px minimum, 48 for a primary pill and the add disc.

A tab screen puts its title (28 px) in the flow with no bar; the board puts
the wordmark (44 px) there instead. A detail screen opens with a back header:
a 40 px round glass chevron, the name at 17 px, an optional trailing round
button. Both scroll away with the content.

The bar floats 16 px in from each side and 24 px off the bottom, or 12 px
past the home indicator, whichever is more. The toast sits 16 px above it.

## Elevation & Depth

Elevation is luminance. A card is white on the linen ground and raised by
nothing else: no hairline, no lit edge, no shadow. Press is luminance too: a
pill or a row darkens one step under the thumb, and nothing scales or glows.
Shadows belong only to the layers that float (glass) and to the one object
that is physically a print.

### Shadow Vocabulary
- **Photo** (`--elev-photo`, `0 12px 32px -8px rgb(33 28 25 / 28%)`; dark
  `0 16px 40px -8px rgb(0 0 0 / 60%)`): the dream tile, and only the dream
  tile. Soft, offset, warm: a print pinned to a wall.
- **Glass** (`--elev-glass`, `0 8px 32px rgb(33 28 25 / 14%)`; dark 45 %
  black): the tab bar and the toast.
- **Sheet** (`--elev-sheet`, `0 -12px 48px rgb(0 0 0 / 25%)`; dark 60 %):
  the bottom sheet, and only it — the one surface that arrives from an edge
  rather than sitting on the ground (D44).
- The toggle's 18 px knob carries a 1 px `rgb(0 0 0 / 20%)` shadow so it
  reads on the ember track. A control detail, not a surface shadow.

Glass is `--glass` over a 1 px `--glass-edge`, backdrop-blurred (20 px for a
chip or round button, 24 px for the bar and toast) and saturated 1.6. It is
for what sits over content or on the ground: the bar, the toast, the round
buttons, the chips. Never a card and never inside a card, where nothing
scrolls under the surface to frost; there a circle or a pill takes the soft
surface instead. Where the browser cannot blur, a floating layer goes opaque
(`--surface`).

In the dark, `--glow` puts two radial lights behind the glass, ember top-left
and dusk low-right, at the strength of a lamp in the next room. It is `none`
in the light theme.

### Named Rules
**The Luminance Rule.** Resting surfaces have no shadow, no border and no
edge. A card is raised because it is lighter than the ground; that is the
whole elevation.

**The One Print Rule.** The dream tile is the only object on the ground that
casts a shadow, because it is the only object that is a photograph.

**The Glass Is Never a Card Rule.** Glass frosts what scrolls under it. A
surface with nothing under it is the soft surface.

## Shapes

Radii come in five sizes and each has a job: 12 px for inputs, wells and the
focus ring; 16 px is defined for a small tile but no primitive uses it in M0;
20 px for cards and dream tiles; 28 px for the sheet's top; and the pill
(9999 px) for every button, chip, badge, segment, toggle, the bar and the
toast. Identity is a circle: 40 px in a row (34 in a settings row, 28 in a
chip, 52 on a preview), its colour behind a white glyph.

The dream tile is portrait, 4:5, with a 16:10 wide variant; its image covers,
the scrim sits between the image and the words, and the words gather at its
foot. Icons are drawn on a 24 grid with round caps and joins, stroke 1.7 for
chrome and 2 inside a circle; the set is hand-drawn chrome glyphs plus a
small Lucide subset inlined as paths.

## Components

The primitives are single classes in `app.css`, composed in markup. There is
no component library and none is planned.

### Buttons
Every button is a pill; rank is the fill, never the shape, and press is one
luminance step.
- **Shape:** full pill (9999 px), 40 px tall, 16 px side padding, 14 px 600 label.
- **Default (soft):** soft surface on ink; presses to hairline.
- **Primary:** 48 px, 24 px padding, 15 px label; `--pill` on `--pill-ink`,
  inverting with the theme. Presses to 80 % pill mixed with surface.
- **Accent:** 48 px, ember on ember-ink. For the one action a screen is about.
  Presses to 85 % ember mixed with ink.
- **Photo:** 18 % white glass, blur 16, white label. The pill on a tile, over
  the scrim. Presses to 30 % white.
- **Card:** card-coloured, for a pill on the ground. **Quiet:** transparent,
  ink-2, presses to soft. **Danger:** transparent with a 1.5 px danger
  outline, presses to the danger wash.
- **Sizes:** `--lg` 48 px with the 15 px label; `--sm` 32 px, 14 px padding,
  13 px label; `--block` full width.
- **Disabled:** keeps its shape at 40 % opacity; the label says what is
  missing.
- **Focus:** a 2 px ember outline offset 2 px, on everything focusable.

### Chips
- **Style:** a 40 px glass pill on the ground with a 13 px 600 name and
  14 px side padding; the category filter over the board.
- **State:** the chosen chip is the primary pill (`--pill` on `--pill-ink`).

### Segmented pill
- A pill track with 3 px of padding, card-coloured on the ground or soft
  (`.seg--soft`) inside a card, `.seg--glass` over a photograph; segments
  34 px (36 soft) at 14 px 600 in ink-2 — full `--ink` on glass —
  `aria-pressed` selects and the chosen segment is the primary pill. `--soft`
  spends its segments' padding on a track that stretches, so it is full width
  or it collapses.
  Labels are sentence case: "Systém", "Světlý", "Tmavý".

### Cards / Containers
- **Corner Style:** 20 px.
- **Background:** surface; presses to surface-pressed when it is a link.
- **Shadow Strategy:** none (The Luminance Rule).
- **Border:** none.
- **Internal Padding:** 16 px, 12 px gap between children. A card of rows
  (`.card--list`) drops to 4 px vertical padding and lets rows carry their
  own height. A **well** inside a card is a 12 px recessed slab in the ground
  colour with 14/16 px padding, for the why, quoted.
- **Rows:** a circle, a title (15 px 600) over a sub (13 px ink-2), an end
  slot; 64 px, hairline between; a pressable row bleeds to the card's edge.
- **Facts:** a `dt`/`dd` list at 14 px, ink-2 label and 600 value, hairline
  between, 10 px vertical padding.

### Badges
- A 12 px 600 pill, 4/10 px padding, soft surface on ink-2 by default.
- **Dreaming** is dusk on the dusk wash; **in progress** is ember on the
  ember wash; **achieved** is white on the whole sky.
- **On a photograph** a badge is 18 % white glass; as the tile's **status tag**
  it is 28 % black glass in the top-left corner, 12 px in, never above the
  title. `--tiny` drops to 11 px with 2/8 px padding.

### Inputs / Fields
- **Style:** a 12 px-radius, 48 px-tall field in the soft surface with no
  border, 16 px side padding, 15 px text; label above at 13 px ink-2; hint
  below at 13 px. The textarea variant is 5.5 rem tall and resizes vertically.
- **Focus:** the outline is replaced by a 1.5 px ember ring (`box-shadow`).
- **Disabled:** 50 % opacity. The caret is ember.
- **Toggle:** 40 × 24, strong-hairline track, ember when on, an 18 px white
  knob travelling 16 px in 150 ms.
- **Search field:** `.search` — a magnifier, the input and a 28 px round
  clear button in one 48 px slab, 12 px radius, on `--surface` so it reads as
  a card sitting on the ground rather than as one of a form's questions. The
  ember ring is on the slab (`:focus-within`), not the input, and the
  browser's own search-cancel button is turned off so there is only one way
  to empty it.

### The note
`.note` — a wash slab for something the app noticed and the person decides
about; today, a dream being written that is already written down (D50).
`--signal-wash` with 13 px ink-2 text, 12 px radius, titles in 600 ink. Ember
and never `--danger`: nothing failed, nothing is blocked, and a note that
looks like an error is a note that gets clicked past. It lives directly under
the field it is about, and it is a polite live region — it arrives while you
type, so it is announced after the letter rather than interrupting it.

### Navigation
- **Tab bar:** a frosted pill 62 px tall, glass with the glass shadow, blur 24,
  a 1 px rim gradient lit from the top-left (`--glass-shine` to
  `--glass-rim`), a highlight entering along the top edge, and a lens
  (`--glass-lens`, its own rim) in the current tab's slot that springs to the
  chosen one in 360 ms on `--ease-spring`. Five equal slots: Nástěnka,
  Seznam, the disc, Síň slávy, Nastavení — `--slots` on `.tabbar` is the one
  number the columns, the lens's width and its travel are worked out from. Tabs are a 24 px icon over an 11 px 600 label
  in ink-3, ink when current, ink-2 on hover. It does not bend what scrolls
  under it (Prosper's displacement map is not carried over; a photograph
  does not need it).
- **The add disc:** a 48 px ember circle with a white plus in the middle
  slot, the one accent-coloured thing on the bar and the only thing on it
  that is not a tab. It opens Přidat, which carries no bar of its own.
- **Back header:** on a detail screen only; a 40 px round glass chevron and
  the title at 17 px, in the scroll column.

### Dream tile (signature)
A photograph, portrait 4:5, 20 px corners, the one shadow on the ground.
Its image covers; a bottom scrim sits between image and words; the body
gathers at the foot with 24/16/16 px padding and 8 px gaps: title at 34 px
600 balanced, one line under it at 14 px with `max-width: 32ch`, then a
photo pill. That line is the affirmation when the dream has one —
`.dream__say`, 500 in full white, because it is meant to be said — and the
why otherwise, 400 in 78 % white (D27). Status, if any, is a glass tag in the top-left corner. With no
photograph, `.dream--sky` paints the sky in its place, and on the board the
sky drifts: two radial lights over the gradient, 16 s alternate on
`--ease-in-out`, felt not seen, off under reduced motion. Above 35 rem the
first tile on the board caps its height to what is left under the wordmark
and above the bar, giving up its ratio before its foot.

### Areas (chips)
Three fixed areas — Chtít · Být · Dělat (D43; Yager's nine until then, D32). In a form they wrap into rows of `.chip .chip--soft`
— glass has no ground to be glass over inside a card, so `--soft` is the
card's own version, as `.seg--soft` is; the chosen one is `.chip--on` in the
pill's colours. Above the reel they are a rail instead, one line, scrolled
sideways with the bar hidden and bled to both screen edges with a negative
inline margin so a chip is never cut mid-word by the page's padding.

### The sheet
A form that arrives from the bottom edge instead of a screen you navigate to
(D44): `.sheet` is a native `<dialog>` opened with `showModal`, so the top
layer carries it over the floating bar without a z-index, and the escape key,
the focus trap and the inert screen behind it come free. The panel is the
app's column (34 rem) at most, 28 px on its top corners, `--ground` so what
sits in it is a card on the ground exactly as on a page, capped at the screen
less 56 px and scrolling inside itself; a 36×4 `--hairline-2` grab bar says
which edge it came from and is not a control. It rises 100 % of its own
height in 220 ms on `--ease-out` while the screen dims to `--overlay`, both
with `allow-discrete` so it leaves the way it came. The dim is a plain
element, not `::backdrop`: a backdrop does not inherit the tokens everywhere,
and a colour outside `tokens.css` is not a colour this app owns.

### The list (Seznam)
The board as one column (D44): `.card--list` of `.row`s, newest first, each a
number, a 40 px photograph cropped into the circle's place — or the sky where
there is none — the title, and a line of state · area · year. The number is
13 px 500 in ink-3, tabular, in a 2ch right-aligned column, and it is the
dream's place in the whole list, so a searched list reads 4, 17, 38 (D48).
The search field sits between the title row and the card from six lines up,
with `n z m` under it in ink-2 while it is being used, and the empty answer is
a card saying so with a quiet pill that empties the field.

### The reel (Nástěnka)
A pager, not a list (D40). The scroll region is the reel alone —
`scroll-snap-type: y mandatory`, `touch-action: pan-y pinch-zoom` — and a
page is the screen: full-bleed, no gap, no radius, no shadow, `height: 100%`
of the scrollport with `scroll-snap-align: start` and `scroll-snap-stop:
always`. The offsets are then exact multiples of the scrollport, which is
what `lib/ui/pager.ts` stands on: it leaves the browser its momentum and
fences a gesture to one page either side of where it began, answers a wheel
gesture once and ignores its momentum tail for 150 ms, and moves the arrow
and page keys one dream (Home and End the ends). Those last two glide on
`--ease-out`'s own curve over `--dur-slow`, written out in JS because a
scroll offset is not a property CSS can animate, with snapping off for the
length of it; under reduced motion there is only the landing. A tile's
photograph wears `--scrim-tall` rather than `--scrim` — a screen is a longer
ramp than a 4:5 print — and its foot clears the bar by `--page-end`.

There are two reels behind one segmented pill at the head of the chrome —
**Vše · Teď** — in `.seg--glass`, the glass the chips beside it wear, because
over a photograph there is no ground for the card-coloured track to sit on;
its unselected segment takes the chips' full `--ink` rather than `--ink-2`.
Teď is at most ten dreams in the person's own order, not shuffled and with no
areas rail over it: ten are reached in ten swipes, so there is nothing to
narrow (D53). Empty, Teď is a `.page` with the soft segment full width above a
card that says what it is for — its own screen rather than a silent fall back
to Vše. Ordering the ten is `/ted`: a numbered `.card--list` whose rows carry
three `.round--sm` buttons, the up chevron being the down one turned over.

The chrome floats over the photograph on `--scrim-top`: the areas rail, the
anniversary as `.glass` with its dusk kept on the circle, the connection's
sentence in the same glass. Only the pills take a tap; everything between
them falls through to the dream. There is no wordmark — the bar says which
screen this is — and the band measures itself into `--band` so a tile's
status badge clears it. The order is the day's pick and then a shuffle, new
on every open (D30); five tiles are in the document at a time and five more
arrive when the reel is two dreams from the end of them, counted from the
pager's own index. Nothing is removed from the top: taking a tile out of a
snapping region moves the one under the thumb.

A tile's controls are one wrapping row at the foot of `.dream__body`, the
only part of it that takes a tap: the heart, and — on a tile whose dream has
no photograph — a `.btn--photo` label with the camera that opens the phone's
own picker (D52). The picked file shows on the tile from the tap, before the
server has made its sizes, so the sky becomes the picture immediately; the
pill reads „Ukládám…“ while it does and is gone once there is a photograph.
Replacing one is still the dream's own screen, where there is room to look at
it first. The row is inside the body, which is in the flow of a fixed-height
page, so it adds nothing to the scroll region and the paging arithmetic
stands.

Empty, the board is a `.page` again, with the wordmark, the 4:5 sky tile and
the anniversary back in the flow on `--dusk-wash`. There is nothing to swipe.

### The crop (D54)
Every photograph carries a point and a zoom, and every surface that shows one
honours them: `photoStyle` in `dreams/photos.ts` emits `object-position` and,
above zoom 1, a `transform: scale()` whose origin is the same point. A
photograph nobody has moved gets no style at all — the default is what
`object-fit: cover` already does. The editor is a full-bleed `<dialog>` whose
frame is the reel's own page: the app column at 34 rem, `100dvh`, the
`--scrim-tall` a tile wears, and the title and line where they will really
sit. Its picture is the only element in the app with `touch-action: none`.
Three `.btn--photo` pills sit at the foot — Zrušit · Na střed · Hotovo, the
last one ember — and the hint rides at the top in `.glass`.

### The photo picker
The dream's tile with the picture in it, and a wrapping row of `.btn--photo`
pills under the words: *Vybrat fotku* (a `<label>` over a hidden file input),
*Posunout* once there is a picture (D54), and *Z odkazu* always (D56). The
link opens a `Sheet` with one `.field`, a `.hint` saying what the server does
with it, and Zrušit · Vzít — the button reading „Stahuju…“ while it waits. A
link that gives nothing replaces the hint with a `.note` in the sheet itself,
because a sentence handed to the screen underneath is a sentence the sheet is
covering.

### The pair (Síň slávy)
A dream with both photographs stands them side by side: two 4:5 halves in a
`1fr 1fr` grid with a 2 px seam, the pair owning one 20 px radius and the
one photo shadow while the halves give up theirs, so it reads as a single
object rather than two tiles. The title and the date sit in the right-hand
half's `.dream__body` — the half that happened. Nothing labels which is
which and nothing sits between them (D28). With one photograph it is the
16:10 wide tile.

### Toast
One glass pill in the ink, centred 16 px above the bar, at most 26 rem
wide, 48 px tall, 14 px 600, rising 12 px in 220 ms on `--ease-out`. An
action rides on the right as a small primary pill. 2.6 s plain, 6 s with an
action, and `ms: 0` for one that stays until it is tapped — that last is for
the message whose action cannot be got back to once it has gone, which is a
new build being ready, and for nothing else (D42).

### Motion
Three durations: 150 ms for state (colour, the knob), 220 ms for entrances
(the toast), 360 ms for the lens; and the 16 s drift. Eases: `--ease-out`
(0.16, 1, 0.3, 1) for state, `--ease-in-out` for the drift, `--ease-settle`
for the splash's rise, and `--ease-spring` (0.3, 1.45, 0.5, 1) for the lens
only. `prefers-reduced-motion` stops everything.

## Do's and Don'ts

### Do:
- **Do** put every colour in `tokens.css` and reach it by role; the dark
  theme is a `--dark-*` bank pointed at twice, once under
  `prefers-color-scheme: dark` and once under `[data-theme='dark']`, and a new
  role is added in all three places.
- **Do** raise a card by luminance alone: surface on ground, 20 px corners,
  no shadow, no border.
- **Do** make every button a pill and rank it by fill: primary inverts,
  accent is ember, soft sits in a card, card-coloured sits on the ground,
  quiet is text, danger is an outline.
- **Do** set type on a photograph in white over the bottom scrim, in both
  themes, and put a tile's status in the top corner as a glass tag.
- **Do** use ember only for what acts and dusk only for what marks; the sky
  only where a photograph would be.
- **Do** keep every label sentence case, on the 4 px grid, at 44 px targets
  or more, and stop all motion under `prefers-reduced-motion`.
- **Do** use glass only for what floats over content or sits on the ground;
  inside a card, use the soft surface.

### Don't:
- **Don't** add a third accent hue, a data colour, or a second gradient.
- **Don't** use uppercase, weight 500 or 700, or a second typeface.
- **Don't** put a shadow, hairline or lit edge on a card, a row or any
  resting surface; the dream tile is the only object on the ground that
  casts one.
- **Don't** put a label above a tile's title; the title carries its own
  weight and status lives in the corner.
- **Don't** frost a card or a page ground, and don't leave a floating layer
  translucent where the browser cannot blur.
- **Don't** write a literal hex outside `tokens.css`, other than the ground
  in `theme-color` and the manifest.
- **Don't** scale, bounce or glow on press; press is one luminance step.
