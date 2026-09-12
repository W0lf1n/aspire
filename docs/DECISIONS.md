# Decisions

Every answered question and every deviation from `PLAN.md`, with the reason.
Prosper keeps a file like this and it is the most useful file in that
repository; this one starts on the same day the code does.

**Revised:** 2026-09-11

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

### D21 — A tenant is a board, and a board is a pairing code

Petr asked for the app to be multitenant. Accounts would have answered it
and broken rule 7, so the tenant is the thing Prosper's auth already has a
shape for: a board. `boards` holds a name and a pairing code; every device
and every dream carries a `board_id`; every query is scoped by the device's
board; nothing crosses. Nobody logs in, and the pairing screen did not
change. PLAN.md §8.6 closes with it: Zuzana is her own board, or a device
on Petr's, decided by which code she types.

Three consequences:

- **The code lives in the database, hashed with PBKDF2**, not SHA-256. A
  twelve-digit code is 10^12 guesses, and a plain hash of it falls to a
  graphics card in an afternoon; a hundred thousand rounds turn a dump of
  the table into years. Prosper never needed this because its one code
  lives in configuration, out of the database's reach.
- **`Pairing:Code` seeds the first board** and is otherwise ignored. The
  laptop mode and the runbook keep working unchanged, and a board that has
  no code, which is what the Boards migration leaves for rows that predate
  boards, takes the configured one on the next start.
- **Boards are made by the operator**, with `board add | code | list` in
  the API's own image, because there is no signup form and there is not
  going to be one. The command speaks English: it is part of the runbook,
  not of the screen.

The migration creates the table, adds `board_id` to devices and dreams,
and points any existing rows at a board it makes for them, so a server
upgraded with paired devices does not need re-pairing. The media path of
D7 keeps no board segment; a dream id is already unique on its own.

### D22 — Dreams are words first, a like is a counter, and deleting asks nothing

The first M1 slice is the dream without its photograph: POST, GET, PUT,
DELETE and one more verb, `POST /dreams/{id}/likes`, all scoped by the
device's board. Three choices worth writing down:

- **A like is a counter on the dream, not a row per device.** Petr chose
  likes within a board over likes across boards, and within a board a like
  is fuel rather than a social signal: the heart says how often the dream
  was felt, and the count is what the board wants to see. It is counted in
  the database (`likes = likes + 1`), so two devices tapping at once both
  land.
- **A dream on another board is a 404, not a 403.** That board's existence
  is not this device's business.
- **Deleting asks nothing and says so in a toast**, as Prosper's goals do.
  There is no undo yet: it would need a soft delete, and a dream is a few
  words and one photograph, both quick to give back.

Validation lives twice on purpose. `DreamService.Problem` on the server and
`rules.ts` on the client say the same Czech sentences in the same order, so
the sentence appears on the keystroke and the server stays a backstop. A
server sentence travels as a problem's `detail`, which the client's
`ApiError` now carries.

### D23 — Photographs: three WebP sizes, an id in the path, nothing kept with its EXIF

M1's pipeline is PLAN.md §4's, with the details it left open decided:

- **ImageSharp 3.1**, under the Six Labors Split License, free for a
  personal project and for anyone under a million dollars a year. 3.1
  rather than 4, because the API this code was written against is 3.1's
  and a resize does not need what 4 added.
- **Three sizes, one format.** Thumb at 400 px on the longest edge, screen
  at 1280, full at 2048, all WebP at quality 82, never scaled up. The
  client sends at most 2048 px, a JPEG made on the device and oriented by
  the browser, so full is the archive and nothing larger ever crosses the
  wire. PLAN.md's `original_path` is gone: the upload waits in the system's
  temp directory only until the worker has read it.
- **The path carries the image id**, `/media/{dreamId}/{imageId}/{size}.webp`,
  not D7's `/media/{dreamId}/{size}.webp`. nginx serves the tree with a
  year of `immutable`, so a replaced photograph has to be a new URL or the
  phone keeps the old one for a year. D7's path stands corrected.
- **EXIF, XMP and IPTC are stripped**, after the orientation is applied.
  The files are served to anyone with the URL, and where a photograph was
  taken is not something a dream board should publish.
- **The resize is a background worker** on a channel (PLAN.md §4). The
  upload answers 202 as soon as the file has landed; `ready` on the wire
  says when the sizes exist, and the board asks again for a few seconds
  while it is false. The worker sweeps on start: a row whose staged upload
  did not survive a restart is dropped, not left waiting.
- **On a laptop the API serves `/media/`** with the same cache header, and
  Vite proxies it beside `/api`; in production nginx answers first and the
  API never sees a byte.

### D24 — Offline is read-only, and the service worker keeps three caches

Petr asked for the images and the texts to be on the device so the board
works without a signal, and for nothing to be written while it is. That is
PLAN.md's M2, folded into M1, and it is the opposite of Prosper: no outbox,
no sync engine, no local database. The server stays the first copy (D7)
and the device keeps a picture of it.

- **Three caches, three rules.** The shell is precached per build and
  replaced whole. The photographs have a cache that outlives builds and is
  answered cache-first, because a photograph's URL never changes its
  picture (D23); the app fills it with the screen size of every tile's
  picture after each successful load and prunes it to the board, so a
  deleted dream's picture leaves the device with it. The board's JSON has
  the third: network first, always, and only when the network fails is the
  last board handed over, with a header that says so. A 5xx counts as the
  network failing, because that is what a proxy answers for an API that is
  not there. Nothing else under `/api/` is cached, and no write ever is.
- **Offline is a flag every screen reads.** The browser's own flag is a
  hint; a request nobody answered, a 5xx from the proxy standing in for
  the server, or a board served from the cache turns the flag off, and any
  other answer turns it on. The app pings `/health` once on start, so a
  screen opened cold knows too. Off, the hearts rest, the
  disc on the bar rests, the forms say why their pill is disabled, and the
  dream's screen hides its three pills behind one sentence. Nothing is
  queued: a like tapped offline is a like not made, which is what Petr
  asked for.
- **Unpairing forgets the board**, both caches with the token: a device
  that is no longer this board's should not keep its pictures.
- **A dead token gets a sentence.** A 401 with a token still on the device
  used to be an empty board; now the board says the server does not know
  this device and links to Párování. The service worker drops the cached
  board on a 401 for the same reason.

iPhone evicts an unused PWA's storage after about a week (PLAN.md §7); the
caches refill on the next open with a signal, and nothing here depends on
them being there.

---

## After M1 · 2026-09-10 — the daily pick and the wall

### D25 — The pick is once a day and worked out on the device; the reel is what is not yet done

PLAN.md §3.2 asks for two things M1 did not build: the board should show
dreaming and in-progress by default, and the first tile should be a dream
chosen for the day rather than always the same one. Both are here.

- **The reel is what is still ahead.** A dream marked splněno leaves the
  swipe. The filter is the client's rather than a query parameter on
  `/dreams`: the board stays one request, one cached response and one prune
  of the photograph cache (D24), and two screens read it. A personal board is
  a few dozen dreams; a second list would be a second cache entry, and the
  wall would be blank without a signal.
- **The Síň slávy is now the wall, which is half of M3.** Filtering without
  it would have made splněno mean *gone*, and a filter that loses a dream is
  worse than no filter. It is the achieved dreams, most recent first, each
  still its own photograph. The before-and-after photograph, the affirmation
  and the anniversary stay M3's.
- **The pick is once a day, not once an open.** PLAN.md §5 gives the rule as
  `ORDER BY last_shown_at NULLS FIRST, random() LIMIT 1` and §3.2 says "on
  open". Taken literally that moves the first tile every time the board is
  looked at, including the poll that waits for a photograph to be resized.
  So: the dream already stamped today stays the pick, and only when there is
  none does the least recently shown win — at random among those never shown
  at all, which is the plan's rule. Exactly one dream is stamped a day, which
  makes it idempotent, keeps the tile still under a thumb, and lets two
  devices on one board agree on the day's dream.
- **The device works the pick out; only the stamp is a request.**
  `lastShownAt` is on the wire now, so a board that has arrived already has
  everything the rule needs and the first tile is right on the first paint,
  with no second round trip before the reel can render.
  `POST /dreams/{id}/shown` answers 204 and nobody waits for it. Offline it
  is skipped, as every write is (D24): the board still opens on a pick,
  worked out from the remembered board, though a board remembered from
  before today's stamp can pick a different dream than the one another
  device saw. It is a dream from your own board either way.
- **The pick wears no badge.** Nothing on the tile says it was chosen. The
  reordering is meant to be felt as *this is what the board opened on*, not
  read as a label, and a second pill on a photograph is one more thing
  between the person and the picture (rule 4).

---

## On the VPS · 2026-09-10 — the first photographs

### D26 — The media volume's owner is set, not inherited, and the API proves it can write before it serves

The first photographs uploaded to `aspire.petrbohac.eu` saved and then
vanished: the form said the dream was on the board, the tile showed the sky,
and nothing said why. `/data/media` in the volume was `root:root` while the
API runs as uid 1654, so `Directory.CreateDirectory` threw `Permission denied`
in the worker, `ImageService.ProcessAsync` deleted the row it could not
finish, and the board — which only shows an image once `ready` is true — had
nothing left to show.

- **The assumption that was wrong.** `apps/api/Dockerfile` chowns `/data/media`
  to `$APP_UID` before switching to that user, on the understanding that a
  named volume inherits the ownership of the directory it is mounted over.
  It does, but only from the **first** container to mount it, and only when
  that container's image actually has a directory there. `web` mounts the same
  volume at `/usr/share/nginx/media`, which the nginx image has no directory
  for; whichever way that lands first decides the volume's owner for the rest
  of its life. `depends_on` orders starts, not volume creation, and it is not
  a thing to leave to an ordering.
- **So the owner is set.** A one-shot `media-init` in the compose file
  `chown`s the volume and exits, and the API waits on
  `service_completed_successfully`. It runs on every `up`, does nothing once
  the ownership is right, and does not care which container got there first.
- **And the API refuses to start without it.** The guard in `Program.cs` was
  `Directory.CreateDirectory(media.Root)`, which succeeds on a root that
  already exists without ever testing whether anything can be written into
  it — so it passed every start while every photograph failed.
  `MediaStore.EnsureWritable` writes a file and deletes it, and throws a
  sentence naming the root and the user when it cannot. A deployment that
  cannot keep a photograph now fails loudly at boot instead of quietly, one
  upload at a time.
- **`ProcessAsync` still drops the row on a failure, and that stays.** It is
  right for a file that is not a photograph, and the staged upload is deleted
  either way, so there is nothing a surviving row could be retried from. The
  fix for the case that is the deployment's fault belongs at the start, where
  it now is.


---

## M3 · 2026-09-10 — the affirmation

### D27 — The affirmation takes the why's place on a tile, and stands beside it on the dream's own screen

PLAN.md §3.4 asks for "an optional affirmation line shown on card". A dream
already has a why, and a tile already carries a title over it; a third block
of type on a photograph is one more thing between the person and the picture
(rule 4). So the two share one slot rather than stacking.

- **The affirmation wins that slot when there is one.** The why explains the
  dream to you — it is written to be read once and remembered. The
  affirmation says the dream as though it were already true, in the person's
  own words, and Yager's whole point is that it is said daily. On a board
  you swipe every morning, the line that is meant to be said out loud beats
  the line that explains. With no affirmation the why keeps the slot, so a
  dream written before this change looks exactly as it did.
- **It is the same line in the same place, a shade louder.** `.dream__say`
  is 500 in full `--photo-ink` where the why is 400 in `--photo-ink-2`; the
  size, the position and the 32ch measure do not move. Nothing labels it. A
  caption saying "afirmace" would be the interface introducing the sentence
  instead of letting it be said.
- **The dream's own screen shows both**, because that is the screen you go to
  in order to read: the why stays on the photograph where it always was, and
  the affirmation opens the card above the facts at 17 px 500 in ink — the
  only line on that screen written in the person's own voice.
- **The choice is `tileLine` in `dreams/board.ts`, with its test.** It is a
  rule about what the board shows, which is that module's job, and a rule
  gets a test before it gets a screen (apps/web/CLAUDE.md). The component
  asks it and styles the answer; it does not decide.
- **120 characters, like the title.** A line, not a paragraph — the why has
  500 characters for the explaining. The column, the server's sentence and
  `rules.ts` all read `AffirmationMaxLength`, so they cannot drift.

### D28 — A photograph knows which of the two it is, and the wall stands them side by side

PLAN.md §3.3 wants the achieved photograph beside the dreamt one. That is
two pictures of one dream, and every screen has to know which is which: the
reel must not open on the proof, and the wall is only proof if both are there.

- **The kind is a column, not a path.** `DreamImage.Kind` is `dreamt` or
  `achieved`, kebab-case on the wire, in the database and in
  `packages/contracts`, the way `DreamStatus` already is. The files stay at
  `{media}/{dreamId}/{imageId}/{size}.webp` — a new photograph is still a new
  id, so nothing cached for a year can go stale, and the media store did not
  have to learn a second word.
- **The migration backfills `dreamt`, not `""`.** EF generates a new
  non-nullable string column with `defaultValue: ""`, and
  `DreamImageKindNames.Parse` throws on that — so the board would 500 on the
  first read after a deploy rather than show the photographs already on it.
  Every photograph taken before this change is a dreamt one; the migration
  says so.
- **The upload names its kind and the API does not argue.** `POST
  /dreams/{id}/images?kind=achieved`; anything but the two words is a
  sentence, and a missing one is `dreamt`. The dream's *status* is not
  checked: a status can change after, and the answer to a dream that went
  back to plním must never be deleting somebody's photograph. So the picker
  is offered only while the dream is achieved, and what is already there
  stays there.
- **Replacing one kind leaves the other.** `photosToReplace` takes only the
  kind being replaced — and the rows still being resized with it, or the old
  one returns as a second picture a moment later. `photos.ts` holds this,
  `photoOf` and `photosOf`, with the test; the three screens that used to
  each find their own first-ready image now ask it.
- **The wall shows the pair as one object.** Two halves, 4:5 each, a 2 px
  seam, one set of corners and one shadow around both: the argument is that
  the right-hand photograph looks like the left-hand one, and a gutter or an
  arrow between them would be the interface explaining the joke. Nothing
  labels which is which — the words and the date sit on the achieved half,
  which is the one that happened. A dream with only one photograph is the
  wide tile it always was.
- **Offline keeps both.** `screenUrls` fed the media cache one picture per
  dream; a wall of half-pairs without a signal is not a wall, so it now
  keeps the dreamt and the achieved.

### D29 — The anniversary is one line on the board, and its verb agrees with the dream

PLAN.md §3.3 asks for "1 year ago you achieved X". Push is M5's, so for now
the anniversary has to be somewhere the person already looks — and the place
they look every morning is the board.

- **One line above the reel, not a card and not a tile.** The photograph is
  the hero (rule 4); an anniversary that pushed the day's dream down the
  screen would be the wall borrowing the board rather than reaching it. The
  line is a dusk wash with a small dusk circle, because dusk marks and never
  acts (tokens.css), and the whole of it is the way back to the dream.
- **Only when there is one, which is most days never.** `anniversaryToday`
  is in `dreams/board.ts` with the other three rules about what the board
  shows, and has its test. The day is the device's own, as the daily pick's
  is: an anniversary should land on the day the person is living, not on
  UTC's. A dream achieved on 29 February keeps its anniversary on 29
  February — the alternative is inventing a date it did not happen on.
- **The most recent one wins when two share a day.** That is the wall's own
  order, and one line is the rule; a list of anniversaries is a screen, and
  the screen already exists.
- **„Před rokem se ti splnil sen …“** — the verb agrees with *sen*, which is
  masculine, and not with the person. A board is a pairing code and the
  server has never been told anybody's gender (D21), so a sentence that
  needed to know would be wrong for half the boards. Czech wants `rokem` for
  one year and `lety` for every number above it; `formatAnniversary` in
  `dreams/format.ts` holds that, with its test.

---

## After M3 · 2026-09-10 — the reel at a hundred dreams

### D30 — The reel is shuffled on every open, and only a window of it is in the document

PLAN.md §3.1 wanted drag to reorder, board order being the person's own
priority. At the size the board is actually meant to reach — around a
hundred dreams — a fixed order is the wrong idea twice over, so it is
replaced rather than built.

- **A fixed order becomes a route you know by heart.** A dream you always
  reach on the ninetieth swipe is a dream you never see, which is exactly
  the habituation the daily pick exists to break (D25), one tile further
  down. Shuffling on every open means no dream has a permanent place, and
  the ninetieth swipe is a different dream every morning. Dragging a
  hundred tiles into an order would also be an afternoon's work to build
  and an afternoon's work to use.
- **The head is still the day's pick.** The pick holds for the day and two
  devices agree on it; only the tail is shuffled. So the board still opens
  on the same dream all day, and what follows it is new every time.
- **The order is a sequence of ids, worked out once per open.** A `$derived`
  that reshuffled whenever the board changed would move the tiles under a
  thumb every time a heart was tapped. `reelSequence` makes the order,
  `reelOrder` reads the board through it: what has left the reel since falls
  out, and what has arrived goes on the end rather than reshuffling the rest.
- **`sortOrder` stays.** It is still the board's own stable order — what the
  server lists in, what the wall breaks ties on, and what the reel falls
  back to before a sequence exists. It simply stopped being what the reel
  shows.
- **Five tiles at a time, growing as the end is neared.** A tile is a whole
  screen with a photograph on it; a hundred of them is a first paint you can
  feel, on the one screen whose whole promise is that it is instant (§2).
  An `IntersectionObserver` on a one-pixel mark at the end of what is
  rendered adds `REEL_WINDOW` more, a screenful of slack ahead of the thumb.
- **Nothing is ever removed from the top.** Real virtualisation would take
  tiles out above the viewport, and removing a tile from a snapping scroll
  region moves the one under the thumb. A reel that is long costs memory; a
  reel that jumps costs the person their place.

### D31 — The voice memo is dropped

PLAN.md §8.4 asked whether the voice memo was worth it in v1.1. It is not.

The app's promise is ten seconds of swiping somewhere you happen to be — a
train, a queue, the end of a bad meeting — and audio is the one thing there
that does not work: it needs quiet or headphones, it cannot be swiped, and
it takes as long to hear as it took to say. Yager's argument for hearing
your own voice is real, but it belongs to a room with a door, not to a
phone in a pocket. §3.4's other half, the affirmation, carries the same
idea in a form the board can actually show every morning (D27).

Server-side it would have been a kind of media the store has never held,
with its own mime checks, its own cap, its own cache and its own delete
path — the whole of the photograph pipeline again, for something played
once. §3.4 and §4's `MediaRecorder` line are struck through.

### D32 — Categories are Yager's nine, fixed, and the board asks for one at a time

**Superseded by D43 (2026-09-11): the set is three — Chtít · Být · Dělat.**
Everything below still holds except the nine themselves: fixed rather than a
table, optional on a dream, chips in the form and a rail over the board.

PLAN.md §8.3 asked whether categories were the fixed Yager set or free-form.
They are the fixed set: `home`, `car`, `travel`, `family`, `freedom`,
`giving`, `business`, `health`, `fun`, shown as Bydlení · Auto · Cestování ·
Rodina · Svoboda · Dávání · Byznys · Zdraví · Zábava.

- **Fixed, because the set is the method.** Yager's nine are not an
  arbitrary taxonomy; seeing all nine is what reminds you which parts of a
  life you have stopped dreaming about. A board that could rename them would
  need a screen to rename them in, a decision about what happens to the
  dreams in one that is deleted, and a table — for a list that has not
  changed since the book. It is an enum beside `DreamStatus` and
  `DreamImageKind`, one lower-case word on the wire and in the column,
  and a `HasConversion` with no migration when a Czech label is reworded.
- **Optional on a dream.** Adding a dream is meant to be three taps and a
  photograph (§2). A question a dream has to answer before it can be written
  is a dream that does not get written, so `category` is nullable, like
  `targetYear`. A dream with none belongs to no area and turns up only under
  „Vše" — which is where it already was.
- **The form wraps, the board scrolls.** Nine is too many for a segmented
  pill. In the form they are chips that wrap into three rows, a set to read
  through once; above the reel they are a rail that runs to both edges of
  the screen, because there they cost a line above a photograph. Pressing
  the chosen chip in the form takes the area off again — a tenth „žádná"
  chip would be one more thing to read for a state the nine already say.
- **The board offers only the areas it has something in**
  (`categoriesOnBoard`), so the rail is short on a small board and never
  offers an area that would empty the reel. If the board runs out of the
  area being asked for — its last dream marked splněno on another device —
  the filter falls back to „Vše" rather than leaving an empty reel under a
  chip that is no longer there.
- **The filter runs after the shuffle** (`byCategory` on the ordered reel),
  so choosing an area lifts dreams out of an order that is already fixed:
  the tiles that stay do not move. Choosing one resets the window and the
  scroll, because a new area is a new reel and it should start at its first
  tile.
- **The tile says the area in the tag it already has**: `sním · Cestování`,
  the pattern the styleguide had for `na cestě · 2028`. A second badge on a
  photograph is a second thing between the person and the picture (rule 4).
- **The top of the board is now a snap point.** The reel's scroll snapping
  ran straight past everything above the first tile, so a swipe up landed
  back on the tile it came from — which would have made the new rail
  unreachable. `.wordmark` carries `scroll-snap-align: start`: the top of
  the board is a place to stop.

---

## M4 · 2026-09-10 — the wallpaper

### D33 — The collage is made on request and never stored, on a canvas the phone asks for

PLAN.md §3.5 wants a lock-screen collage from a few chosen dreams, so the
dream is seen a hundred times a day without opening anything.

- **Made on request, kept nowhere.** The photographs are already on disk at
  full size; the collage is drawn from them into a `MemoryStream` and
  streamed back. A stored collage would be a second tree under the media
  root to own, to prune when a dream changes, and to get the ownership of
  wrong on a volume — which is exactly the failure D26 was. There is
  nothing to invalidate, because there is nothing kept.
- **The canvas is the phone's own screen, not a preset.** §4 named 1170×2532
  and 1080×2400, but the device knows its own `screen` and
  `devicePixelRatio`, so it asks for exactly that and the phone never scales
  the result up. The presets survive as the server's default for a request
  that names no size. Clamped to 200–4096 px an edge: below that it is a
  thumbnail, above it a way to spend the VPS's memory.
- **Six at most** (`CollageLayout.MaxPhotographs`), mirrored on the client.
  Above six each dream is too small on a phone to be the one you recognise.
- **The arrangement is pure geometry, and it is tested without an image.**
  `CollageLayout` returns rectangles: up to three the photographs stack in
  full-width bands, because a phone canvas is twice as tall as it is wide
  and a band is the shape a photograph survives; above three they pair up.
  Rounding is carried rather than repeated, so the cells sum exactly to the
  canvas — a one-pixel seam of nothing down a wallpaper is a thing people
  notice, and the test asserts the areas add up.
- **No gutter, and so no colour.** A seam between the cells would need a
  colour, and the one place a colour exists here is `tokens.css`, which a C#
  renderer cannot read (rule 1). Edge to edge is also the better wallpaper.
  Each photograph covers its cell and is centre-cropped to it, never
  letterboxed and never squashed.
- **JPEG, not WebP.** The file leaves the app: it is saved to a photo
  library and then chosen as a wallpaper by the phone's own settings, and
  JPEG is the format every one of those steps has always taken.
- **The share sheet, then a download link.** On a phone „Uložit obrázek" is
  in the share sheet, and a `download` link is not; `navigator.canShare({
  files })` decides, and a browser without it gets the link. A share the
  person backed out of (`AbortError`) is not a failure and says nothing.
  The screen keeps the finished image on it either way, because the surest
  way to save a picture on a phone is still to hold a finger on it.
- **It lives in Nastavení, not in the bar.** The bar is four slots and full
  (D3's shape); a wallpaper is something you make now and then, not a place
  you go. A dream that is already achieved can be on one — that is exactly
  the kind you want on a lock screen — so the choice reads the whole board
  rather than the reel.

---

## M5 · 2026-09-10 — the morning nudge

### D34 — A subscription carries an offset, not a timezone, and the key pair is configuration

PLAN.md §3.6 wants a notification at a time the person sets, default 07:00,
off / daily / weekdays. The parts of that decided before anything was sent:

- **A subscription is per device, not per board.** A phone and a tablet on
  one board are two rows with two schedules, because it is the phone in a
  pocket at seven that this is for. The push endpoint *is* the device, so it
  is the unique key: subscribing again from the same phone moves the row it
  already has, and a phone re-paired into another board moves rather than
  colliding.
- **Off is the absence of a row**, not a row that is off. A browser that
  revokes a subscription leaves nothing behind either, so the two states the
  server can be in are the two states that exist.
- **The device sends its UTC offset in minutes, not an IANA zone name.** The
  API runs with `InvariantGlobalization`, so it has no zone database to look
  a name up in, and giving it one to serve one feature is the wrong trade.
  An offset goes stale twice a year; the device sends it again every time
  the app opens, so it is right again the first time anybody looks — and
  being an hour out for one morning is a notification at six or at eight,
  not a bug anybody files.
- **A nudge that missed its morning is not sent at bedtime.** `NudgeSchedule`
  fires within two hours of the time and then skips the day. A worker
  stopped over breakfast still sends at ten past; one that starts at eleven
  at night does not, because a dream at bedtime is not the morning habit
  this is and would be the app's first unwelcome notification. The whole
  rule is pure and tested — a weekend, a day already sent, a server that was
  down all morning — rather than something you wait a day to find out.
- **`LastSentOn` is a local date, not a timestamp**, because the question is
  "has today had its nudge".
- **`DueAsync` reads untracked.** `MarkSentAsync` writes with
  `ExecuteUpdate`, which goes round the change tracker; a tracked read after
  it handed back the row as it was before the stamp, and the test caught the
  same phone being nudged twice.
- **The VAPID pair is configuration, generated once by hand**, beside the
  pairing code. Not the database and not generated at start: the public key
  is baked into every browser subscription the server has ever handed out,
  so a pair that regenerated itself would silently orphan all of them. A
  server with no pair does not do notifications, says so with a 503 rather
  than keeping a subscription it cannot honour, and runs perfectly well
  otherwise — which is what a laptop wants.

### D35 — The push crypto is written here, against the specification's own numbers

PLAN.md §4 named the `WebPush` NuGet. The sending is written against the
RFCs instead, on `System.Security.Cryptography`.

- **Nothing is invented.** VAPID is an ECDSA P-256 signature over a JWT
  (RFC 8292) and the body is ECDH + HKDF + AES-128-GCM in the order RFC 8291
  sets out; every one of those primitives is in the BCL, and the whole of
  `WebPushCrypto` is about 180 lines of composing them. Hand-rolling a
  cipher would be reckless; composing standard ones to a published spec is
  what every library in this space also does.
- **The test is the specification's own worked example.** RFC 8291 §5
  publishes a push message with the keys, the salt and the resulting body,
  so the test asserts that this code produces those exact bytes — not that
  it agrees with itself. A round trip through an independent decrypt, and a
  VAPID signature verified against the public half, cover the rest.
- **What that caught.** The HKDF labels held a raw `0x01` byte rather than
  the two characters `\x01`, so every derived key was wrong. `grep` showed
  the line as correct — a 0x01 prints as nothing. Without the vector this
  would have shipped and failed silently on a phone at seven in the morning,
  which is the worst possible place to find out. The trap is in CLAUDE.md.
- **The dependency this does not add.** `WebPush` is at 1.0.13; the API's
  only other package for this would have been one more thing to audit and
  upgrade for a protocol that has not changed since 2017. The client already
  ships no runtime dependency (rule 3); the server keeps the same habit
  where the cost of doing so is a file with a test.

### D36 — `vapid` runs before the database, and the worker wakes every minute

- **The command touches nothing.** `docker compose run --rm -T api vapid`
  prints a pair and exits, and it runs *before* the
  migration in `Program.cs`: the moment you need it is while setting a box
  up, which is before its database exists. Run a second time it warns first,
  because replacing the pair silently orphans every subscription anybody has
  (D34) and the person running it twice has not read this file.
- **A minute is the tick.** The person chose the time to the minute, so
  nothing should be more than a minute late; the round is one query over a
  table with as many rows as there are phones, and the schedule is
  arithmetic. A board's dreams are read once however many of its devices are
  due, because a household is several phones and one board.
- **A sent nudge stamps `LastShownAt`.** A notification puts a dream in
  front of somebody, which is what "shown" means (D25) — so the board opens
  on the dream they were already told about when they tap it, and tomorrow's
  nudge is a different one. `DailyPick` is the same rule as `board.ts`'s,
  in C#, with its own test; the client has to keep its copy because it opens
  the board without a signal (D24).
- **A failed send is left unstamped**, so the next minute tries again and
  the grace window ends the day. A push service answering 404 or 410 is
  telling the truth and the row goes.
- **The service worker shows the notification**, because a push arrives when
  no page of the app is running. Tapping it opens that dream in the tab that
  is already there rather than a second copy of the app.

---

## After M5 · 2026-09-11 — the photographs a board keeps

### D37 — The offline prefetch is four at a time, in the order the board will be swiped

`rememberBoard` fetched every uncached photograph in one unbounded
`Promise.all`. That was right when a board had a handful of dreams. It is
not right now: PLAN.md aims at around a hundred (D30) and an achieved dream
has two photographs (D28), so a first load could start two hundred
screen-size requests in the same millisecond, on whatever connection the
phone happens to be on.

- **Four at a time** (`pooled`, `PREFETCH_AT_ONCE`). Nothing about what ends
  up cached changes — only how many are in flight. Four is about a browser's
  own per-host limit, enough that the first screenful is there in a moment
  and few enough that the connection is not handed the whole board to sort
  out while somebody is looking at the first tile.
- **A failure does not take the pool with it.** One photograph the server
  will not give up is not a reason to stop fetching the rest.
- **In the order they will be met** (`prefetchOrder`): the reel as it will
  actually be swiped — the day's pick first (D30) — and then the wall. This
  also changes nothing about what is cached; with four in flight it is the
  difference between the tile under the thumb being ready at once and being
  the hundredth request in the queue. It is composed from `reelOrder` and
  `achievedDreams`, so there is no second definition of the reel's order.
- **`have` became a `Set`.** It was an array being `includes`-d once per
  wanted URL: fine at ten photographs, two hundred × two hundred at the size
  this is now for.

**Still open, and not decided here: whether every dream's photograph should
be prefetched at all.** Four at a time fixes the thundering herd; it does
not change that a hundred-dream board still pulls tens of megabytes down on
a first load, which may be mobile data. The alternative is caching a window
ahead of the thumb rather than the whole board — but D24 promises the board
is readable without a signal, and narrowing that promise is a product
decision, not a performance one. It is Petr's to make.

---

## After M5 · 2026-09-11 — the desktop

### D38 — The desktop grid is dropped; desktop stays the phone with room around it

PLAN.md §3.2 has asked for a masonry grid on desktop and tablet since the
first draft, click through to the detail, and it was the one thing M1 left
behind. It is dropped rather than built.

The app is one column, at most 34 rem, and above 35 rem it shows its edges
as a hairline and is otherwise unchanged — that is DESIGN.md's layout and
it is the whole of the app's shape. A masonry board would be the only
screen that is not that, and the exception would have to be held against
every primitive for as long as the app exists: the page's one scroll
region, the floating bar positioned inside the column, the tile that gives
up its ratio so the bar is never covered.

But the real argument is not the cost. **A grid is a different feeling from
a reel.** §2 asks for a full-screen image per dream, and a masonry cell is
a thumbnail; twelve of them at once is a gallery of things you own rather
than one thing you want. The habituation that D30 fixed by shuffling comes
straight back on a wall where everything is visible at once — nothing is
ever *next*, so nothing is ever arrived at. And the day's pick (D25), which
is what makes the board different each morning, has nowhere to be when
every tile is equally in view.

Desktop is not where this app is used. Where it is used — adding a dream
from a laptop, reading the Síň slávy on a big screen — the phone column
already works, and works well: a 4:5 tile at 34 rem is a photograph at a
comfortable size, not a phone screenshot stretched.

So: nothing is built, §3.2's second bullet and §6's M1 note are struck
through, and M1 has nothing outstanding. If a desktop wall is ever wanted
it is a **new route** — a wall beside the reel, with its own answer to what
the pick means there — and not a breakpoint bolted onto the board.

---

## After M5 · 2026-09-11 — what the board downloads

### D39 — The board fetches a window of the reel, and the whole of itself only when the connection is free

D37 left a question open: whether a hundred-dream board should prefetch every
photograph at all. It should not. The answer, and the arithmetic behind it.

**The measurement first.** A `screen` photograph is 1280 px on its longest
edge at WebP quality 82, and twelve real ones came to 2.36 MB — **197 kB
each**. So a hundred-dream board is about **20 MB**, and an achieved dream
has two photographs (D28), so a board with a full Síň slávy is more.

**Twenty megabytes is not a storage problem.** A phone holds gigabytes of
photographs; the app's own code budget is 150 kB and this is a hundred times
that, but in absolute terms it is one podcast episode. If room were the only
question the answer would be to cache everything and stop thinking about it.
Two things make it a question anyway:

- **The first open on mobile data.** Twenty megabytes arriving while somebody
  is looking at the first tile, on a metered plan, competing with that tile.
- **Eviction, which size makes likelier.** §7 already notes that iOS drops a
  PWA's caches after about a week unused. Safari's eviction is per-origin and
  opaque, and a fatter origin is a better target. A smaller cache survives.

**So the window.** Prefetching follows the reel's own window (`aheadOf`): the
tiles in the document plus one screenful ahead — ten photographs on open,
fifteen when the reel has grown to ten, never the hundred. It is the same
`REEL_WINDOW` the reel renders by (D30), not a second idea of a window.
Measured on a twelve-dream board: five tiles, ten photographs, 2 MB instead
of 2.4.

**What the window does not do is download less over a month**, and this is
worth being honest about. The reel is shuffled on every open (D30), so the
window is a different handful every morning; a cache that only grew would
arrive at the whole board anyway, just slower and in the order least likely
to help. That was observed while building this — a second open pulled two
photographs the first had not wanted. The window bounds the **peak**, which
is what hurts; it does not bound the total.

**Which is why there is a ceiling.** `WINDOW_KEEP` is 40 photographs, about
8 MB: several mornings of swiping, and far enough under the size at which a
browser starts choosing for itself. `overCap` drops the oldest *fetched* —
the Cache API's key order is the only clock it has, and what just arrived is
at the far end of it, so a tile about to be swiped to is never what goes.

**And a window quietly narrows D24**, which promised the board is readable
without a signal. Under a window, offline is "the dreams the shuffle handed
you recently". That is acceptable for a reel — a queue is ten swipes, not a
hundred — but it is a product change, not a performance one, so it is a
choice and it is on a screen:

- **Co prolistuješ** — the window only. The smallest data bill.
- **Na wifi** — the whole board where the browser says the connection is
  free, the window on mobile data. The default where it can be kept.
- **Vždy celá** — the whole board regardless.

**The connection is read, not guessed.** `navigator.connection.type` is the
only member that says what a connection *is*; `effectiveType` is a speed
estimate that cannot tell wifi from good cellular, so it is read in one
direction only — definitely slow means treat it as metered, fast means
nothing. A connection the browser will not describe counts as metered: the
wrong guess one way costs a few megabytes of cache, and the other way costs
somebody's data.

**Safari has no Network Information API at all**, so on every iPhone — and
Petr's friends are on iPhones — „na wifi" is a promise that cannot be kept.
It is therefore **not offered there**: the screen shows two choices and a
sentence saying why, the same way the nudge screen handles a server with no
VAPID key rather than offering a switch that cannot work (§15). The default
where nothing can be detected is the window.

**The screen also answers the question that prompted all this** — how much
room the app takes — with `navigator.storage.estimate()` and a button that
forgets it. The photographs are a copy; the dreams are on the server.

### D40 — A swipe is worth one dream, and the board's chrome floats on the photograph

The reel was a list of prints that happened to snap. It is a pager now. Two
changes, written down as one, because neither of them works without the
other.

**What was wrong.** `scroll-snap-stop: always` is the standard's own promise
that a scroll will not pass over a snap position, and it was already on every
tile — but there was nothing underneath it for the promise to hold on to. The
snapping was `proximity`, the tiles had a 12 px gap between them and a 20 px
radius, each one was the screen *less* the bar rather than the screen, and
they shared a scroll region with the wordmark, the areas rail and the
anniversary. So the snap offsets were not multiples of anything, the top of
the board was a page of a different height, and a long drag carried exactly as
far as the finger did. On a trackpad it was worse: a flick is dozens of wheel
events, every one of them its own scrolling operation, so the promise was kept
four times and the reel had gone past four dreams.

**Every page is the screen.** Full-bleed — no gap, no radius, no shadow, the
photograph to all four edges — the scroll region is the reel and nothing else,
and snapping is `mandatory`. The offsets are then exact multiples of the
scrollport, which is what makes the arithmetic below true rather than nearly
true. A reel has no ground to show a dream against, and a seam of it passing
by mid-swipe is the tell that this is a list.

**And a fence, not a rewrite.** The obvious way to guarantee one dream per
gesture is to take the gesture: follow the finger with a transform and animate
to the next page on release. That throws away the platform's momentum, its
rubber band at the ends, its handoff between touch and scrollbar and keyboard,
and it fights `touch-action` for the rest of its life. `lib/ui/pager.ts` keeps
all of that and adds the one missing guarantee:

- **A touch may travel one page from where it began, and no further.** The
  fence is checked on every scroll event, and setting `scrollTop` is what ends
  a fling, so a swipe the length of the screen lands on the next dream and
  stops there. The length of the swipe decides nothing — which is the whole
  ask.
- **A wheel gesture is answered once**, and then nothing is heard until the
  wheel has been quiet for 150 ms, so a trackpad's momentum tail is not a
  second swipe. Firefox reports wheels in lines rather than pixels, so the
  travel is converted before it is counted.
- **The arrow and page keys move one dream**, Home and End the ends.

The glide those last two land with is written out in JavaScript, because a
scroll offset is not a property CSS can animate for us. It is `--ease-out`'s
own curve — cubic-bezier(0.16, 1, 0.3, 1) is easeOutExpo — over `--dur-slow`,
read from the stylesheet rather than copied, and snapping is off for the
length of it because every frame of a tween is a place the reel is not allowed
to rest. Under `prefers-reduced-motion` there is no glide, only the landing.

**The chrome floats because the paging is arithmetic.** With the wordmark, the
rail and the anniversary in the scroll region, the first page is a different
height from every other one and none of the above is true any more. They move
over the photograph instead: the rail as the glass chips it already was, the
anniversary as glass with its dusk kept on the circle, the connection's
sentence in the same glass beside them, all of it on `--scrim-top` — a token
that has been sitting in `tokens.css` unused since the first edition, and this
is what it was for. Only the pills take a tap; everything between them falls
through to the dream, which is also what keeps the top of the screen somewhere
the reel can be dragged from. The wordmark is dropped rather than moved: the
bar already says which screen this is, and „Aspire" set over somebody's
photograph is the interface refusing to step back (§4's rule). It survives on
the empty board, which is a page and not a reel.

A screen-tall photograph also needs a longer ramp than a 4:5 print does — the
words sit clear of the floating bar, a sixth of the way up, where `--scrim`
has barely begun — so `--scrim-tall` joins it in the bank.

**D30's one-pixel mark is gone.** The window used to grow when an
`IntersectionObserver` saw a mark at the end of what was rendered. The pager
already knows which dream is on the screen, so the window grows from that
instead, two dreams of slack ahead of the thumb. That is one moving part
fewer, and it also removes an element with no snap position from the middle of
a mandatory snapping region, which was a thing that had to be explained. The
rest of D30 stands: five at a time, nothing ever removed from the top.

**The cost, stated plainly.** A drag longer than a screen now stops dead at
the fence while the finger keeps going. That is what a pager is, and it is
what was asked for. D38 is untouched — the reel is the same shape at every
width, a full-bleed tile inside the same 34 rem column.

**And it cannot be watched in the Browser pane.** A scroll event is dispatched
by the frame loop, and a hidden pane has no frames (`apps/web/CLAUDE.md`), so
a fence checked on scroll never fires there — the first attempt to verify it
looked exactly like a bug. `pager.wiring.test.ts` drives a fake scroll region
and a frame queue the test turns by hand instead, which is the check that
lasts anyway.

### D41 — The nudge screen asks whether the server can send before it offers a switch, and the laptop's key pair lives in user secrets

M5 built the morning nudge and left one thing half-true. `DEPLOYMENT.md` said
that without a VAPID pair "the Upozornění screen tells the person the server
cannot send"; it did not. `unavailable()` knew two reasons — a browser that
cannot do notifications, and an origin that has already refused — and the
server's half only ever surfaced as a toast, **after** the browser had been
asked for permission, for a server that had nothing to send.

That is the worst order those two things can happen in. A browser gives an
origin one notification prompt, near enough: refuse it and the switch can
never work again without a trip into browser settings. Spending it on a
server with no keys is spending it on nothing.

So the screen asks first, the way Stahování asks before it offers „na wifi“
(D39). `outOfReach` in `push/schedule.ts` takes the three facts — browser,
server, permission — and answers with one sentence or none; `nudge.ts`
gathers them. In that order, because each makes the next beside the point: a
browser that cannot do notifications makes the server's keys irrelevant, and
a server with no keys makes a permission the person could fix irrelevant too.

**A server that will not answer is not a reason.** Offline is a state this
screen already says in its own words, and „cannot send“ would hide a switch
that works perfectly well the moment there is signal. So `sends` is
`true | false | null`, and only `false` blocks. Until all three are known the
controls are drawn and disabled rather than withheld — the shape of the
screen is the same either way, and a switch that cannot be tapped for half a
second is quieter than a sentence that appears and goes.

**And the laptop now has a key pair, in the user secrets store.**
`appsettings.Development.json` is in the repository and should stay readable —
the pairing code in it is `000000` on purpose — but a VAPID private key is
not that kind of value. `UserSecretsId` in `Aspire.Api.csproj`, the file
itself outside the checkout, the same three settings production reads from
the environment. A laptop's pair is its own and must never be the VPS's:
whichever server a browser subscribed through is the only one whose key can
reach it again, which is D34 from the other direction.

**Verified against a standing-in push service**, because a browser was not
available to be one. A local listener with a real P-256 point and a
subscription due that minute: the worker woke, chose a dream, and posted
336 bytes of `aes128gcm` under a VAPID `Authorization`, TTL 14400. Everything
but the browser decrypting it and calling `showNotification`, which needs a
phone.

### D42 — A new build keeps the one before it, and the way back into the app is a control rather than a toast

The first deploy after M5 left the app with no way forward and no way to
refresh. Three things had to be true at once, and all three were.

**The service worker was deleting the shell that was still in use.** The
shell is precached under the build's own name, so every build brings a cache
and `activate` cleans up after it — and it cleaned up everything that was not
the new one. A page still running the *old* bundle is served from the cache
that just went. It then asked for a route chunk whose hashed name exists
neither in the cache nor in the new image, got a 404, and could not finish the
navigation. Which is the same navigation `applyUpdate` was waiting for to
reload the page into the new build.

So a new build now keeps **one generation back**: its own shell, and the
newest of the others (`offline/shell.ts`, `shellsToForget`). A page from the
build before last is beyond saving anyway, and the photographs and the board
are not shells and were never in this. A cache whose name carries no build
stamp cannot be placed in time, so it is not the generation worth keeping and
it goes.

**The toast said so for eight seconds and then stopped saying it.** The one
thing it offered — *Obnovit* — could not be found again once it had gone. It
now stays until it is tapped (`ms: 0`, `toast.svelte.ts`). That is a power
kept for exactly this: a toast that sits there is a toast in the way, so it
is for the message whose action cannot be got back to, and for nothing else.

**And an installed app has no address bar.** On a phone, a PWA on the home
screen has no reload button anywhere — the browser's chrome is the thing
being installed away. So Nastavení's version card carries *Obnovit
aplikaci*, always, not only when a build is waiting. A control that appears
only once there is news cannot be reached: `applyUpdate` reloads on the next
navigation, so navigating to the screen to press the button is already the
press. Asking first is what makes it more than a reload — a worker that has
been sitting unasked since yesterday installs now, and the page comes back on
the new build.

`update.ts` became `update.svelte.ts` for this: `pending` is `$state` now,
because two screens read it rather than one toast writing it.

**Three fixes for one failure, on purpose.** Any one of them alone leaves a
version of the morning where somebody is stuck: the shell fix stops the
bricking but not a missed toast, the toast fix is useless on a build whose
chunks are gone, and the button is the one that works when the other two have
already failed. This is the screen the app is recovered *from*, so it does not
get to depend on anything.

---

## After M5 · 2026-09-11 — the list, and three areas instead of nine

### D43 — The areas are Chtít · Být · Dělat, and D32's nine are gone

Yager's nine — Bydlení · Auto · Cestování · Rodina · Svoboda · Dávání ·
Byznys · Zdraví · Zábava — are replaced by three: `want`, `be`, `do`, shown
as **Chtít · Být · Dělat**. Same enum, same column, same one lower-case word
on the wire; `packages/contracts`, `DreamCategory` and `CATEGORY_LABEL` move
together as they always have.

- **Asked, and answered.** D32 argued the nine are the method and fixed them
  for that reason. The method the board is actually kept by turned out to be
  a different one: a dream is something you want, something you want to be,
  or something you want to do — and that is a question you can answer in the
  second it takes to write the dream down. Nine is a taxonomy you have to
  stop and place a dream in.
- **What the set costs is the form, not the column.** Nine chips wrapped
  into three rows of the form and ran off both edges of the board's rail.
  Three chips are one row in the form and four pills — „Vše“ and the three —
  in the rail. Nothing in either place had to change to hold them: the form
  still wraps, the rail still scrolls, and both were built to (D32).
- **Optional, exactly as before.** A dream with no area belongs to none, and
  the rail offers only the areas the board has something in. Pressing the
  chip already chosen takes the area off again — a fourth „žádná“ chip would
  be one more thing to read for a state the three already say.
- **The old words are cleared, not mapped.** The `Areas` migration nulls
  every category that is not one of the three. Nine do not map onto three
  without inventing an answer the person never gave, and a dream showing an
  area it was never put in is worse than a dream showing none — the field is
  optional and „no area“ is a state every screen already draws. The column
  is the same shape, so the migration carries no schema change at all; it
  exists for the sentence in it. `DreamCategoryNames.Parse` still throws on a
  word it does not know, which is the truth about a row this build never
  wrote.
- **On the laptop the migration does not run.** SQLite mode creates the
  schema and leaves it alone, so a dev `aspire.db` written before this keeps
  its nine words and answers the board endpoint with a 500. The same
  `UPDATE` run against the file fixes it without losing the dreams.

### D44 — Seznam is the fourth tab, and a dream is written in a sheet

`/seznam` is a new screen: every dream on the board as one line, the most
recently written first, with ＋ in the corner opening a sheet of five fields
— Název · Proč · Afirmace · Oblast · Kdy.

- **The reel is for looking; this is for writing.** The board is a
  photograph you swipe at arm's length, and it is deliberately bad at being
  a list: one dream fills the screen, the order is shuffled every open
  (D30), and what is achieved has left it altogether. None of that helps
  when the question is „what have I got“ or „let me get this down before I
  lose it“. So the list is the one screen on which the board is all of
  itself — the achieved dream and the dream still ahead in the same column,
  in the one order that never moves, newest at the top where you are looking
  after you write one.
- **A sheet, not a screen.** Přidat is a place you go: a photograph first,
  its own route, the board underneath it gone. Writing a dream into a list
  is an errand — you are already looking at the thing you want it to join,
  and losing sight of it to type two sentences is the wrong trade. The sheet
  rises over the list, the list stays visible behind the dim, and the saved
  dream appears at the top of it as the sheet sinks.
- **Five fields and no photograph.** The sheet asks what a sentence can
  answer. A photograph is the one thing that cannot be typed — it is picked,
  cropped, waited on — so it stays where the waiting has room: Přidat, or
  the dream's own screen afterwards. `⊕` on the bar is still Přidat and
  still the way in when the dream arrives as a picture.
- **No Stav.** A dream being written for the first time is „sním“; that is
  what the word means. The segment is on Přidat and on Upravit, where the
  answer can have changed. `DreamForm` grew two props for this — `withStatus`
  and `oncancel` — rather than a second form: the fields, their checks and
  their sentences are the same fields, and a copy of them is a copy that
  drifts.
- **The bar goes to five slots.** Nástěnka · Seznam · ⊕ · Síň slávy ·
  Nastavení. The disc moves to the middle one — five slots have a middle,
  four did not, and the one accent-coloured thing on the bar belongs where
  the bar is symmetrical about it, two tabs either side. The lens now slides
  across five, and `--slots` on `.tabbar` is the one number the columns, the
  lens's width and its travel are all worked out from. Four labels at 10 px fit a
  360 px screen with room; a sixth slot would not, and that is the bar's
  limit rather than a rule about tabs.
- **The sheet is a primitive.** `.sheet` in `app.css`, spending
  `--elev-sheet`, `--overlay` and the 28 px radius the token bank has held
  since M0 for exactly this. It is a native `<dialog>` opened with
  `showModal`, which is the whole reason to use one: the top layer puts it
  over the floating bar with no z-index to argue with, and the escape key,
  the focus trap and the inert screen behind it come with it. Being open is
  the screen's state, never the element's — escape and the dim *ask* to
  close — so a sheet in the middle of saving stays up.
- **The dim is an element, not `::backdrop`.** A backdrop does not inherit
  the tokens in every browser that has one, and a colour that is not in
  `tokens.css` is not a colour this app owns.

---

## After M5 · 2026-09-11 — the button and the invitation

### D45 — The deployment is a script on the box, and the button only presses it

`deploy/deploy.sh` is the deployment. `.github/workflows/deploy.yml` opens an
SSH session, runs that script, and does nothing else.

- **Because the runbook was already the deployment.** `DEPLOYMENT.md`'s
  Updating section was four commands chained with `&&`, typed into a terminal
  at the end of a day. The two things a person then did by eye — look at
  whether the app came back, look at whether the box had anything
  uncommitted on it — are the two things a script does the same way every
  time, and the two things a tired person skips. So the script is the runbook
  with its eyes: it refuses to fast-forward over uncommitted changes, and it
  does not call a deployment finished until `/api/v1/health` answers `ok`.
- **The key it uses can only ask for a deployment**, which is D47 — the
  first version of this pointed the workflow at `root` and a path, and that
  made the secret in GitHub a root shell.
- **The logic is in the repository, not in the workflow.** A deployment that
  lives in YAML can only be run by the thing that reads the YAML. This one
  runs by hand over SSH, from the button, from cron if it ever wants to be,
  and it is versioned beside the compose file it acts on. The workflow is
  eleven lines of `ssh` and a summary.
- **Manual, `workflow_dispatch` only.** One person, one VPS, and a board
  somebody reads on a train. Every green commit arriving unannounced buys
  nothing here that pressing a button does not. It does not check that CI
  passed either: it deploys what it is pointed at, says which sha that was,
  and CI has already run on the push.
- **No action from the marketplace.** `appleboy/ssh-action` and its relatives
  are a third party in the path to the box, holding the key, for four lines of
  `ssh`. The same argument as the client's zero runtime dependencies, applied
  where the stakes are the box rather than the bundle.
- **The host key is a secret, not a keyscan.** Keyscanning inside the workflow
  trusts whatever answers on the night somebody is in the middle, which is the
  attack `known_hosts` exists for. It is pasted once, compared against the
  laptop's own line.
- **The ref is checked before it reaches a remote shell.** It is typed by a
  person into a box on a web page and then interpolated into an `ssh`
  argument; the workflow refuses anything that is not `[A-Za-z0-9._/-]`, and
  passes every value through the environment rather than through `${{ }}`
  inside the script.
- **A failed build is not a failed deployment.** `build` writes new images
  while the old containers go on serving from the old ones, so nothing is
  half-swapped; going back is the same script with the sha it printed last
  time, which rebuilds it. There is no image to retag because the box builds
  from source and there is no registry.

### D46 — A pairing code is generated by the API and printed in one terminal

`board invite <name>` makes a board and its code in the same breath, twelve
digits from `RandomNumberGenerator`, and prints it once. `scripts/invite.sh`
is one SSH session that runs it on the box.

- **Because the multitenancy was already there.** A board is the tenant (D21)
  and a device belongs to whichever board's code it typed. Nothing in the app
  needed a line for a second person: a new board *is* the invitation. What
  was missing was the code — `board add` made the operator invent one, and
  `DEPLOYMENT.md` handed them a `shuf` line to do it with.
- **The machine's randomness, not a person's.** A code typed by somebody in a
  hurry has a pattern in it and 10^12 is only 10^12 if every digit is a coin
  toss. `RandomNumberGenerator.GetInt32` rather than a byte and a remainder,
  which would make the low digits likelier — exactly the bias a guesser
  starts from. The leading digit is never zero, as the runbook's own `shuf`
  line has always produced: a code is read aloud, written down and typed
  back, and a leading zero is the digit that gets lost on the way.
- **Printed once, on purpose.** What is kept is the PBKDF2 hash, so there is
  no command that reads a code back and no dump that contains one. Lost means
  `board code` with a new one.
- **Not a GitHub Action.** A workflow that prints a code leaves it in a run
  log for ninety days, readable by anybody who can read the repository, and
  reachable by anybody who ever gets a token for it. The code is the whole of
  what stands between a stranger and somebody's dreams. So the job is a
  script on the laptop: the code is on one screen, for as long as that
  terminal is open.
- **Still no signup form** (D6). A board is something the operator makes, and
  this is the operator making one in a single command rather than in four.

### D47 — The deploy key gets a user of its own, and one command to run

The box has an `aspire-deploy` user. Its key carries a forced command,
`/usr/local/bin/aspire-deploy` (`deploy/aspire-deploy` in this repository),
which accepts exactly `aspire-deploy <ref>` and runs the deployment through
one `sudo` rule.

- **Because a key in a third party's vault is worth what it can be used
  for.** The first version of D45 pointed the workflow at `root` and a path,
  which means the secret in GitHub was a root shell on the VPS. Nothing about
  GitHub makes that unreasonable; the trouble is that it is unbounded — a
  leaked secret, a compromised runner, or any of the ways a token gets out
  become the whole box rather than one deployment.
- **What could not be fenced, and is said out loud in the runbook.** The
  deployment writes `/opt/aspire` and talks to the Docker socket, and socket
  access is root: a member of the `docker` group starts a container with `/`
  mounted in it and is done. So the deployment still runs as root. Pretending
  otherwise by putting the deploy user in the `docker` group would be the
  same privilege with a longer story.
- **So the fence is on the key, where it can actually hold.** `command=` in
  `authorized_keys` means sshd runs the wrapper whatever the client asks for,
  with the ask in `SSH_ORIGINAL_COMMAND`; `no-pty`, `no-port-forwarding`,
  `no-agent-forwarding`, `no-X11-forwarding` and `no-user-rc` take the rest.
  The wrapper refuses everything that is not `aspire-deploy <ref>` and every
  ref that is not `[A-Za-z0-9._/-]`, a leading dash included — that last one
  is an option to `git checkout`, not a branch. A shell, a `cat`, an `scp`
  and a `master; rm -rf /` all end at the same sentence.
- **The sudo rule names one script and nothing else**, and the script is
  root-owned in a root-owned tree, because a script a user may run as root
  and may also edit is a root shell with extra steps.
- **The wrapper is in the repository and copied to `/usr/local/bin`**, not
  symlinked into `/opt/aspire`. It is the fence; a fence that moves when the
  thing it fences is updated is not one. It changes about never.
- **The workflow's vocabulary shrank to match.** It sends `aspire-deploy
  '<ref>'` rather than a path to a script, so the path lives on the box and
  `VPS_PATH` is gone. The single quotes are for the case where somebody sets
  this up without the forced command and a real shell sees the string; with
  the forced command there is no shell, so the wrapper unquotes it itself.
- **What this still does not stop:** anybody who can push to `master` can run
  code as root on the box, because the box builds what it fetches. That is
  what continuous deployment is. The runbook says so next to the commands
  rather than leaving it to be discovered.

---

## After M5 · 2026-09-11 — the list gets numbers, a field, and a second opinion

### D48 — Every line in the Seznam is numbered, and the number is the dream's place in the whole list

The list has a number down its left edge, 1 at the top. It is the dream's
place in the list as a whole, so a searched list shows 4, 17 and 38 rather
than 1, 2, 3.

- **Because a column of forty titles has no landmarks.** The reel is
  deliberately placeless — shuffled every open, one dream a screen — and the
  Seznam is the opposite of that: it is the board as a thing with a shape.
  A number is the cheapest way to give it one. „Ten sedmnáctý“ is a sentence
  you can say about a dream; „ten někde uprostřed“ is not.
- **It is also the count.** The last number is how many dreams there are,
  which is a fact the app otherwise never says out loud, and the one fact
  that makes the list feel like an inventory rather than a feed.
- **The place holds while the list is searched**, which is the whole reason
  the number is positional rather than per-result. Three results numbered 1,
  2, 3 say nothing; three results numbered 4, 17 and 38 say where they are,
  so emptying the field is a way back to them rather than a new search.
- **Newest first, so the number moves.** A number that changes when a dream
  is added is the price of a list that puts what you just wrote at the top
  (D44), and it is worth paying: the alternative is numbering from the first
  dream ever written, which puts 1 at the bottom of the screen and makes the
  top of the list a number nobody can predict. This is a place in a column,
  not an identity — the id is the identity, and it is in the URL.
- **Tabular figures and a 2ch column**, so a title starts on the same
  vertical whether its line is 7 or 38, and the column does not shiver while
  it is being narrowed.

### D49 — The Seznam searches everything a dream says, from six lines up

The field appears above the list once there are `SEARCH_FROM` (6) lines, and
narrows on the title, the why, the affirmation, the state, the area and the
year — `dreams/search.ts`.

- **Everything, because the word you remember is not always the title.** A
  dream is a title, a reason and a sentence you say to yourself; the word
  that comes back three weeks later is as likely to be in the why. The state
  and the area are searched in the Czech the screen shows them in, so
  „splněno“ finds what is done and „být“ finds that area — what is searched
  is what is read.
- **Diacritics are folded off both sides.** Czech typed at speed on a phone
  is Czech with half the marks missing, so „stesti“ finds *Štěstí* and
  „drevena zahrad“ finds *Dřevěná dílna na zahradě*. A search that insists
  on a caron is a search that answers „nic“ to a word that is on the screen.
- **Words are an AND, not a phrase.** A person types the two words they
  remember, not the words in the order they were written.
- **From six lines**, because below that the whole list is already on one
  screen and a field for narrowing five lines is one more thing to read on
  the screen whose job is to be the list.
- **On the client, over the board it already has.** The board is in memory
  and in the cache (D24, D39); a search endpoint would mean a round trip, a
  spinner, and nothing at all offline, for a list this app hopes stays in the
  low hundreds.

### D50 — A dream that is already written down is said out loud, never stopped

While a title is being typed, the form compares it against the board and
names what it looks like: „Podobný sen už v seznamu máš: **Island na kole** ·
plním.“ The pill stays live, says the same word it always said, and saving
takes one press.

- **Because it is his list.** The same dream written twice is sometimes
  exactly what somebody means — a second run at it, a different year, the
  same wish said better. The app has an observation, not a veto, and a
  confirm step would turn the observation into an accusation.
- **So the note is ember, not red** (`.note` on `--signal-wash`). It is not
  an error: nothing failed, nothing is blocked, and a warning that looks
  like a failure is a warning that gets clicked past.
- **It is live rather than on submit**, under the title as it is typed, in a
  polite live region. A warning that arrives after the press is a warning
  about something you have already decided.
- **Generous about phrasing, mean about everything else.** The comparison is
  on a folded, punctuation-less key: equal keys, one title standing whole
  inside another as words, or a Sørensen–Dice bigram score over 0.72.
  „Barcelona“ and „Barcelona 2027“ are the same dream; „Naučit se
  španělsky“ and „Naučit se anglicky“ are not, and a check that cannot tell
  those apart is a check that gets ignored. Containment needs three letters,
  not four, because dům, byt, pes and auto are whole dreams in Czech and the
  match is word for word rather than letter for letter.
- **The affirmation counts, and only word for word.** It is a sentence a
  person says to themselves; the same sentence under two titles is one dream
  with two names.
- **Dice rather than an edit distance**, because this runs on every keystroke
  against the whole board and bigrams are linear.
- **Upravit checks too, minus itself.** A title is a title whether it is
  being written or rewritten, so the edit screen passes the dream's own id
  and is left out of its own answer.
- **The screens that have no board do not check.** Přidat and Upravit fetch
  it alongside their own work and a fetch that fails is a form that says
  nothing, never a form that cannot be used.

---

## M6 · 2026-09-12 — the nudge that came late

### D51 — A nudge is urgent, says so in the log, and the device reports where it is on every open

A nudge set for 07:00 arrived at 08:17. The server was not at fault and the
clock was not either: the dream it named was stamped `2026-09-12 05:00:02Z`,
which on this board's +120 offset is two minutes past seven. The hour and a
quarter was spent in the push service's queue, because of one word.

- **`Urgency: high`, not `normal`.** RFC 8030 §5.3 lets a push service hold
  anything below `high` until the device is convenient to reach, and Apple
  carries web push through the same queue as app notifications, where the
  lower priority is the one a sleeping phone may defer. The header had always
  said `normal`, under a comment arguing the opposite — „the phone is asleep;
  waking it is the whole point“. The specification's own example of `high` is
  a time-sensitive alert, which is exactly a notification at a minute the
  person chose; this server sends nothing else, so there is nothing for it to
  crowd out and no budget it can spend.
- **A sent nudge writes a line.** Only failures were logged, so a notification
  that arrived late could not be told apart from one that was sent late — the
  diagnosis had to go through `last_shown_at` in `psql`, which only works
  because the stamp happens to exist (D36). The line carries the board, the
  dream and the device's own local time. Not the endpoint: that is the
  capability to push to somebody's phone, and it does not belong in a log.
- **The envelope has a test now.** `WebPushSenderTests` drives the sender
  through a fake handler and asserts the four headers and the status mapping.
  The crypto was checked against RFC 8291's own numbers from the first day
  (D35) and the envelope around it was checked by nothing, which is where the
  only delivery bug so far lived.
- **`POST /api/v1/nudge/offset`, on every open and every resume.** D34
  promised that „the device sends it again every time the app opens, so it is
  right the morning after a clock change“, and the code never did: the offset
  went only when the switch in Upozornění was moved. A subscription made in
  summer would have nudged an hour out all winter. It is its own verb rather
  than a `PUT` of the subscription, because a `PUT` carrying no mode and no
  time writes „daily at seven“ over whatever the row said — opening the app
  must never move the hour. A resume counts as an open: a phone is rarely
  loaded cold, and the morning after the clocks move is a resume.
- **It answers 204 whether or not there was a row.** A device that has never
  asked to be nudged gets the same answer as one that has, so the endpoint
  cannot be used to find out whether this phone wants a morning dream.
- **`FindAsync` reads untracked now**, which the offset's own test caught:
  `MarkSentAsync` and `UpdateOffsetAsync` both write with `ExecuteUpdate`,
  which goes round the change tracker, so a tracked read after either one
  hands back the row as it was before. D34 had already learned this about
  `DueAsync` and the lesson had not reached the neighbouring method.

What this does not change: the two-hour grace window stays (D34). A deploy
over breakfast should still send the morning's dream, and the window is why
the 11th's nudge went at 09:11 rather than not at all.

### D52 — A tile without a photograph is where that photograph is asked for

A dream written as a sentence in the Seznam is saved with no picture (D44),
and the reel is what that costs: a full-screen tile of words on a gradient,
with one control on it, the heart. The way to give it a photograph was two
screens away — open the dream, then the picker. It is on the tile now.

- **Where the gap is noticed is where it is closed.** The reel is the screen
  whose whole promise is the photograph (§2), so a tile that is missing one
  is the best possible moment to ask: the person is looking at the dream,
  thinking about it, and the phone's own library is one tap away.
- **The picture appears on the tap, not eight seconds later.** The downscaled
  file becomes an object URL and the tile shows it while the upload, the
  resize and the three sizes happen behind it. The URL is let go of when the
  next pick makes one or when the screen goes — never the moment the upload
  finishes, because the real photograph and the preview swap on a later frame
  and a URL revoked between the two blanks the tile.
- **The pill is gone once there is a photograph.** Replacing one stays on the
  dream's own screen, where there is room to look at what is there first. A
  reel tile offers the thing that is missing and nothing else.
- **The sequence moved to `dreams/upload.ts`.** Out before in, and only its
  own kind: a failed upload leaves the sky rather than the wrong picture, and
  changing the dreamt photograph never takes the proof with it (D28). Two
  screens do this now, and the order is the part that is easy to get wrong,
  so it is one function with its own test — the API calls arrive as an
  argument so the ordering can be checked without a browser.
- **It adds no height to the scroll region.** The controls are a row inside
  `.dream__body`, which is in the flow of a page that is exactly the
  scrollport, so the snap offsets stay multiples of it and rule 14 holds.
- **It rests without a signal**, like every write (D24).

Checked end to end in the pane: a photograph picked on a sky tile uploads,
polls twice, swaps the preview for `/media/…`, and takes the pill with it.

---

## M7 · 2026-09-12 — the second reel

### D53 — There are two reels, and Teď is ten dreams in his own order

The board is one reel of everything still ahead, shuffled on every open so no
dream has a permanent place (D30). That is the right shape for a hundred
dreams and the wrong one for the handful somebody is actually working on this
month. So the board has two, chosen by a segment over the photograph: **Vše ·
Teď**.

- **Teď is not shuffled, and that is the whole point.** D30's argument against
  a fixed order was the ninetieth swipe: a dream always reached last is a
  dream never reached. Ten dreams are reached in ten swipes, so there is no
  ninetieth, and an order he chose is worth more than an order that surprises
  him. Rank 1 is the top and the reel opens there.
- **Ten, and the eleventh is refused.** The cap is the feature rather than a
  limit on it: somebody focusing on eleven things is not focusing. The client
  knows the number so the pill says so on the tap, from the board it already
  holds; the server says the same sentence and means it.
- **The day's pick stays on Vše, and so does its stamp.** The pick is what
  makes the board a different one each morning (D25) and what the nudge names
  (D36). A board opened on Teď has not put the day's dream in front of
  anybody, so it does not claim it did: the stamp waits until Vše is on the
  screen. It fires on the switch, which is the moment it becomes true.
- **The rank is an ordering key, not a position.** The screens number the ten
  by their place in the list, so a gap left by one taken off costs nothing,
  and there is no compaction to get wrong on either side. Reordering rewrites
  the lot as 1…N, which is also what puts any drift back.
- **The index on `(board_id, focus_rank)` is not unique**, which is a
  deliberate departure from how this was planned. A unique index is checked
  per statement, so two dreams swapping places would collide on the way past
  each other, and the fix would be a two-phase write. Ten rows a board, every
  rank assigned by the server, and a shared rank is a tie broken by the id —
  not corruption worth that. `Two_dreams_swapping_places_do_not_collide` is
  the test that would have failed.
- **A dream marked splněno leaves Teď** the way it leaves the reel, and the
  slot it held is free. It cannot be put back on while it is achieved: Teď is
  what is in front of you and the wall is what is behind.
- **Arrows rather than drag, on a screen of its own.** A dream gets on Teď
  from its own screen with one pill — that is the decision, and it is made
  while looking at the dream. `/ted` is where the ten are seen together and
  ordered, because that is the part that needs a list. Dragging was dropped
  for the board in D30 and nothing in the app has had it since; ten rows is
  the length arrows are for, and a drag handle on a phone competes with the
  scroll it sits in.
- **Every change saves the whole order in one request.** The ten are never
  half-ordered on the server, there is nothing unsaved to lose by leaving the
  screen, and a save that fails puts the list back and says so. The list moves
  before the request: an arrow that waits for a round trip is an arrow that
  feels broken.
- **Empty Teď is its own screen, not a fall back to Vše.** The person tapped
  Teď, so the screen owes them an answer about Teď. It says what Teď is for
  and offers the way to fill it.
- **Which reel was open is remembered per device**, like the offline policy
  (D39), because it is about this phone. Vše is the default: it is where the
  pick lives, and a board with nothing on Teď has nothing else to show.
- **„teď“ leads the Seznam's line and is searchable.** It is the only part of
  that line saying what is being done about the dream now rather than what the
  dream is, and what is read is what is searched (D49) — so „ted“ without the
  diacritic narrows the list to the ten.
- **The segment wears glass over the photograph** (`.seg--glass`), like the
  chips beside it, with the areas rail under it on Vše only; ten dreams need
  no narrowing. It floats, adding no height to the scroll region, so the
  paging arithmetic stands (D40).

Checked in the pane end to end: the segment switches and is remembered, Teď
shows the ranks in order, an arrow swaps two and the server agrees, the sheet
adds one and it leaves the candidates, the pill toggles both ways, opening on
Teď stamps nothing, and „ted“ finds the four.
