# Decisions

Every answered question and every deviation from `PLAN.md`, with the reason.
Prosper keeps a file like this and it is the most useful file in that
repository; this one starts on the same day the code does.

**Revised:** 2026-09-10

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

- **The command touches nothing.** `docker compose run --rm api dotnet
  Aspire.Api.dll vapid` prints a pair and exits, and it runs *before* the
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
