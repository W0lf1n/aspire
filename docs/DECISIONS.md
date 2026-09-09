# Decisions

Every answered question and every deviation from `PLAN.md`, with the reason.
Prosper keeps a file like this and it is the most useful file in that
repository; this one starts on the same day the code does.

**Revised:** 2026-09-09

---

## M0 · 2026-09-09 — the scaffold

The brief for M0 said: where Prosper does X, Aspire does X. These are the
places PLAN.md and Prosper disagreed, and what was chosen.

### D1 — The repository has Prosper's layout, not PLAN.md §4's

`apps/api`, `apps/web`, `packages/contracts`, `deploy/`, not `backend/` and
`frontend/` with compose at the root. PLAN.md was written before Prosper was
inspected; the two apps share a VPS and a way of working, and a second layout
would be a second set of habits.

### D2 — The UI is Czech

PLAN.md §8.5 left it open. Prosper is Czech and the two are siblings, so
Nástěnka · Přidat · Síň slávy · Nastavení, and Czech routes: `/pridat`,
`/sin-slavy`, `/nastaveni`. Code, comments, commits and docs are English, as
in Prosper.

### D3 — .NET 10, not the .NET 9 in PLAN.md

Prosper's csproj, Dockerfile and CI are all on 10. Both SDKs are installed on
the development machine.

### D4 — Three backend projects, where Prosper has one

`Aspire.Api`, `Aspire.Domain`, `Aspire.Infrastructure`, as PLAN.md asks.
Prosper's server deliberately has no domain: it stores rows as JSON and
reasons about four fields. Aspire's will have a daily-pick rule and an image
pipeline, which is exactly what the split is for. Domain and Infrastructure
are nearly empty in M0 and that is fine.

### D5 — Real migrations, with SQLite kept for the laptop

Prosper has no migrations and creates its schema with `EnsureCreated`,
documented as a decision. Aspire's schema will grow with every milestone,
so: Postgres runs `MigrateAsync()` on start; the SQLite laptop mode keeps
`EnsureCreated`, since Npgsql migrations cannot run on SQLite. A
`DesignTimeDbContextFactory` pins `dotnet ef` to Npgsql whatever environment
the command runs in, and CI fails on a model change without a migration.
`dotnet-ef` is a repository-local tool (`apps/api/.config/dotnet-tools.json`)
rather than a global install, because the global one on the development
machine was 9.x.

### D6 — Auth is Prosper's, so there is no users table

A pairing code typed once, a device-bound bearer token stored as a SHA-256
hash, no expiry, the two rate-limit fences, the same tests. That answers
PLAN.md §8.1. It also removes `users` and every `user_id` from PLAN.md §5:
one board, as many devices as you own. PLAN.md §8.6 (a second person) is
therefore "a second device on the same board" unless a second board is ever
wanted, at which point it is a real design question rather than a column.

The client keeps the token in `localStorage`. Prosper keeps its token in
IndexedDB because it already has Dexie; Aspire has no IndexedDB layer and
gains nothing by adding one for a single string.

### D7 — Photographs on the VPS disk, in a named volume

PLAN.md §8.2 left disk versus MinIO open. Disk: a `media` volume mounted at
`/data/media` in the API, read-only into the web container, where nginx
serves `/media/` with a year of cache and never involves the API for a
byte. The path is `/data/media/{dreamId}/{size}.webp` — no user segment, per
D6. The API image creates the directory owned by the app user before the
user switch, so the volume inherits that ownership on first mount and the
non-root API can write into it. MinIO or S3 is a later decision if the disk
fills.

### D8 — A Vite dev proxy instead of CORS in development

Prosper's client has a configurable server address, so its dev setup opens a
CORS door for the Vite origin. Aspire's client is always same-origin, so Vite
proxies `/api` to the API's dev port instead. The `Cors:Origins` allowlist
still exists in the API and is unused.

### D9 — Columns are snake_case

Prosper's columns are PascalCase because EF takes the property name
verbatim, and its runbook has a section explaining why `select entity from
changes` fails. A fresh schema does not need that section: `AppDbContext`
renames every column to snake_case in `OnModelCreating`, fifteen lines, no
package. PLAN.md §5 already reads that way.

### D10 — The `dreams` table exists from the first migration

The M0 brief asked for an "initial empty migration". The initial migration
carries `devices`, which auth needs, and `dreams` with the columns PLAN.md §5
names minus the ones that belong to later tables, so the placeholder
`GET /api/v1/dreams` reads a real table and M1 adds images rather than
starting over.

### D11 — `/api/v1/health`, not `/health`

The brief wrote `GET /health` and `GET /api/dreams`. In Prosper's deployment
only `/api/` reaches the API; a bare `/health` would be answered by the
static site. So the routes are Prosper's: `/api/v1/health`, `/api/v1/pair`,
`/api/v1/dreams`.

### D12 — Port 8081

Prosper holds `127.0.0.1:8080` on the same VPS. Aspire publishes `8081`, its
own compose project name, and its own host vhost.

### D13 — Small things

- `.editorconfig` added (not in Prosper) matching Prettier: tabs, LF, UTF-8;
  four spaces for C#, two for YAML.
- `launchSettings.json` added (not in Prosper) so `dotnet run` starts in
  Development on `127.0.0.1:5300` with SQLite and the dev pairing code.
- `planner-PLAN.md` removed from this repository; it belongs to Planner.
- pnpm 11 in CI and the web image, because pnpm 11 wrote the lockfile.
- `DESIGN.md` lives at `apps/web/DESIGN.md` — where the brief asked for it
  (`frontend/DESIGN.md`) and where the design tooling looks — rather than in
  `docs/` as in Prosper.

### D14 — The dreams query breaks ties on the id

Found by the first smoke test: SQLite cannot `ORDER BY` a `DateTimeOffset`,
so `ThenBy(CreatedAt)` threw a 500 on the laptop and would have worked on
Postgres. The tie-break is the id, deterministic on both providers. The
laptop mode exists to catch exactly this.

---

## Design · 2026-09-09 — the first edition

### D15 — Prosper's system, warmed, with a sky

The brief pinned the world: start from Prosper's visual language so the two
feel like siblings, warmer and more colourful, at most two accent hues,
photos are the hero. So no concept roll was run and no comps were made — the
direction was the brief's, and there was no image generation to comp with.
The direction contract is the first child of `<body>` in `app.html`.

What changed from Prosper, in `apps/web/src/lib/styles/tokens.css`:

- The ground is warm linen `#f6f2ee` with a hint of rose (not cream, so the
  ember reads warm rather than the ground reading yellow); the ink is warm
  `#211c19`; the dark ground is warm charcoal `#141210` and the dark ink is
  warm white `#f7f2ee`, never pure black or white.
- Two accents. **Ember** `--signal` `#cc4a1c` is the one that acts: the add
  disc, links, the active toggle, the "in progress" badge. It is the deepest
  orange that still passes AA as text on white (4.6:1); in the dark it lifts
  to `#ff8a55` and takes dark ink on it, the way a sun does. **Dusk**
  `--dusk` `#5a4fd6` never acts: it marks a dream still being dreamed and it
  is the far end of the sky.
- One gradient, `--dawn`, ember through rose to dusk, theme-independent like
  a photograph. It is the only place colour appears at scale, and it stands
  in for a photograph until there is one.
- `--photo-ink` and `--scrim`: type on a photograph is white over a bottom
  scrim, in both themes.
- Everything else is Prosper's: the 4 px grid, the type ladder, the radii,
  Inter 400/500/600, no uppercase, pills, circles, luminance elevation, the
  floating glass bar.

### D16 — The empty board is the first dream's tile

The empty state is a 4:5 `.dream` tile with the sky in it, the title where a
title will be, the why where the why will be, and the add pill on it: the
board already looks like the board before anything is on it. The sky drifts
at the pace of a slow breath, the one authored motion on the screen, off
under `prefers-reduced-motion`.

### D17 — Four slots, the disc in the second

Prosper's bar is five slots with the disc in the middle. Aspire's brief named
four entries in this order: Board · Add · Hall of Fame · Settings. The bar
keeps the order and draws Add as the ember disc, the one accent-coloured
thing on it. Off-centre, and on purpose: it is the action, not a tab.
Reviewable on `/styleguide`; the order is the brief's to change.

### D18 — Two detector findings, both brief-pinned

Impeccable's detector flags Inter as overused and `--ease-spring` as bounce
easing. Both are Prosper's — the one face, and the lens spring on exactly one
element — and both are recorded as ignores in `.impeccable/config.json` with
that reason, the way Prosper's config does.

### D19 — The finish review, and what it changed

Impeccable's finish reviewer, run fresh on the captured build in both
themes, returned "fix" with three material findings, all applied in one
batch and scored resolved on the verdict pass:

- **No label above a title.** The `první sen` badge over "Zatím žádný sen"
  read as a kicker, which the craft floor bans outright. A dream's status
  lives in the tile's top corner as `.dream__tag`, dark glass on any
  photograph, never above the heading.
- **The tile clears the bar on a desktop.** In the 34rem column a 4:5 tile
  ran under the floating bar and hid the add pill on first paint. Above
  35rem the first tile caps its height to what the viewport leaves under
  the wordmark and above the bar; it gives up its ratio before its foot.
- **Segments agree on case.** `Systém · Světlý · Tmavý`, like every other
  segmented pill; the hub's `podle systému` stays lowercase as a summary
  line.

Noted and kept: the Síň slávy empty state is an icon-circle card rather
than a sky tile like the board's. That is for M3, when the wall has a shape.

### D20 — Pairing has no address field and no probe

Prosper's pairing screen takes a server address, and before it sends the
code it probes `/api/v1/health` at that address, so that a mistyped domain
reads as "that is not the server" rather than "the code is wrong". Aspire's
client is always the same origin as its API (D8), so there is no address to
mistype and nothing for the probe to distinguish: the screen is the code, a
name for the device list, and one pill. Every failure arrives as a status
and each status has its Czech sentence in `apps/web/src/lib/api/pairing.ts`,
which is tested; a browser's own message never reaches the screen.

The card is a form, so the keyboard's return key pairs. *Odpojit* forgets
the token on the device only; the row on the server stays until it is
deleted there, which is what revocation is (D6). Same as Prosper.
