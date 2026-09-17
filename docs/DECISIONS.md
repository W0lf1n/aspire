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

The card is a form, so the keyboard's return key pairs. _Odpojit_ forgets
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
- **Amended 2026-09-12 (PLAN.md §24.4).** Quality 75 for screen and full and
  70 for the thumb rather than 82 throughout; libwebp's slowest method; a
  Lanczos resample and a light sharpen after a downscale. Files written
  before that day are what they were — the URLs are immutable and
  re-encoding what is already served buys nothing. And `full` is on the
  wire as `largeUrl`: the rung a phone reads on the reel (D62), not an
  archive nobody asks for.

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
  it would have made splněno mean _gone_, and a filter that loses a dream is
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
  reordering is meant to be felt as _this is what the board opened on_, not
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
  sentence, and a missing one is `dreamt`. The dream's _status_ is not
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
- **„Před rokem se ti splnil sen …“** — the verb agrees with _sen_, which is
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
  pocket at seven that this is for. The push endpoint _is_ the device, so it
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
  prints a pair and exits, and it runs _before_ the
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
ever _next_, so nothing is ever arrived at. And the day's pick (D25), which
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
browser starts choosing for itself. `overCap` drops the oldest _fetched_ —
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
only member that says what a connection _is_; `effectiveType` is a speed
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
radius, each one was the screen _less_ the bar rather than the screen, and
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
the new one. A page still running the _old_ bundle is served from the cache
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
thing it offered — _Obnovit_ — could not be found again once it had gone. It
now stays until it is tapped (`ms: 0`, `toast.svelte.ts`). That is a power
kept for exactly this: a toast that sits there is a toast in the way, so it
is for the message whose action cannot be got back to, and for nothing else.

**And an installed app has no address bar.** On a phone, a PWA on the home
screen has no reload button anywhere — the browser's chrome is the thing
being installed away. So Nastavení's version card carries _Obnovit
aplikaci_, always, not only when a build is waiting. A control that appears
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
already failed. This is the screen the app is recovered _from_, so it does not
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
  the screen's state, never the element's — escape and the dim _ask_ to
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
  needed a line for a second person: a new board _is_ the invitation. What
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
  is Czech with half the marks missing, so „stesti“ finds _Štěstí_ and
  „drevena zahrad“ finds _Dřevěná dílna na zahradě_. A search that insists
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

**Amended 2026-09-12 (D65's session).** A photograph between the upload and
the resize painted this same sky, under this same pill — so a tile that was
holding a photograph invited the same one to be sent again, and a slow
connection read as an upload that failed. `photoComing` in `dreams/photos.ts`
tells the two apart, and the tile says „Zpracovává se…“ in the row the pill
was in. In that row rather than above it: anything that adds height to the
scrollport breaks rule 14's arithmetic.

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

### D54 — A photograph carries a point and a zoom, not a cropped file

Every surface in this app crops: the reel to the shape of a phone screen, the
Síň slávy to 4:5 and to half of a pair, the Seznam to a 40 px circle, the
wallpaper to a collage cell. All of them cropped from the centre, so a
portrait with a face near the top lost the face on the reel and there was
nothing to be done about it.

- **Three numbers on the photograph, not pixels on the disk.** `focus_x`,
  `focus_y` and `zoom`, defaulting to the middle and all of it — which is
  exactly the crop every photograph already had, so the migration changes
  nothing anybody can see. A crop baked into the file would be made for one
  of those four shapes and wrong for the other three; metadata is right for
  all of them at once, changes instantly, and costs no resize.
- **They are `object-position` percentages.** 0 is the left or top edge, 1 the
  right or bottom. That is the browser's own meaning, so `photoStyle` sets two
  custom properties and does no arithmetic at render time, and
  `FocalCrop.For` works the same window out in C# for a collage cell. One
  definition, two languages, and a wallpaper that crops where the reel crops.
- **Zoom is a transform with its origin at the point**, so what is being
  looked at stays where it is while the picture grows around it. It stops at
  three: past that the `screen` size is being stretched on a phone, and the
  point of this is a better crop rather than a worse picture.
- **A photograph nobody has moved gets no style at all.** The default is what
  `object-fit: cover` already does, and an identity transform on five
  full-screen tiles is five compositing layers bought for nothing.
- **The editor is the reel's own page**, not a preview of it: the app column,
  the height of the screen, the scrim, and the title and line where they will
  really sit. A photograph positioned in a 4:5 preview and then shown on a
  9:19 screen is positioned for the wrong shape. One finger moves it, two
  pinch it, a wheel zooms on a laptop and the arrows nudge it; the whole drag
  is measured from where the finger went down rather than added up frame by
  frame, so a gesture that hits an edge and comes back ends where it should.
- **The crop travels with the upload.** On the add screen there is no dream to
  hang a second request on, so `focusX`, `focusY` and `zoom` ride in the
  query beside `kind`. A photograph that already exists takes
  `PUT /dreams/{id}/images/{imageId}` instead, which touches no file.
- **`FocalCrop` is pure geometry with its own test**, beside `CollageLayout`
  for the same reason: every edge of it — both axes, the flush one, the zoom,
  a window that must never leave the photograph, numbers that cannot mean
  anything — is arithmetic rather than a picture somebody has to look at.

Two things the browser caught that no unit test would have. A photograph
already in the cache — which is every photograph the reel has just shown —
fires `load` before the editor exists, so its size was never read and every
gesture was a silent no-op; the element is asked directly now as well. And
`setPointerCapture` throws on a pointer the browser has already released, so
it is guarded: a drag without capture beats a drag that stops.

### D55 — The laptop's database says when it is behind, at start

SQLite mode creates its schema with `EnsureCreated` and migrations are
written for Npgsql (D5), so `EnsureCreated` makes the file once and never
touches it again. The morning after a model gains a column, the file is a
column short, the API starts perfectly well, and the first request for a
dream comes back „no such column: d.focus_rank“ as a 500 — which reaches the
screen as „Server odpověděl 500“, and that is a bad way to find out.

`SqliteSchema.BehindTheModelAsync` asks every table for one row before the
server serves anything, which makes SQLite prepare a statement naming every
column the model expects. A file that is behind says so at start, once, with
the file's full path and the fix in the same sentence, and the API refuses to
start rather than answering 500 all morning. The same bargain as
`MediaStore.EnsureWritable` (D26): prove it can do the thing before promising
to.

`pnpm start` runs the API and the web together, which is the other half of
the same problem: `pnpm dev` alone proxies to a port with nothing behind it,
and `ECONNREFUSED 127.0.0.1:5300` in the Vite log is not obviously „you did
not start the other one“. No new dependency — pnpm runs both scripts from one
regex.

---

## M7 · 2026-09-12 — a photograph from a link

### D56 — The server fetches a linked picture, and may only reach the internet

PLAN.md §4 has wanted „paste URL“ since M0 and nothing ever built it, because
the phone cannot: an image on another origin is not readable by script, and a
Pinterest pin's page is not readable at all. So the server fetches it — which
means this app now makes requests somebody else chose the target of, and that
is the most dangerous thing in it.

- **The address is refused, not the name.** A name resolves to whatever it
  likes, and can resolve to something different the second time it is asked;
  checking the URL and then letting the stack resolve it again is a check that
  can be walked past. The socket is opened in a `ConnectCallback` that
  resolves the host itself, keeps only the addresses `PrivateAddress.IsPublic`
  allows, and connects to those — so there is no window between the check and
  the connection.
- **Everything not obviously public is refused**, rather than a list of
  known-bad ranges: loopback, the three private blocks, carrier-grade NAT,
  link-local — which is where a cloud provider keeps its credentials —
  multicast, the reserved and documentation ranges, and the same set again
  for IPv6 including an IPv4 address wearing an IPv6 coat. The list of things
  reachable from inside a network is not one anybody finishes writing.
- **Redirects are walked by hand**, at most three, with every hop checked. A
  host answering 302 to `http://169.254.169.254/` is the whole trick, and
  `AllowAutoRedirect` would follow it before anybody looked.
- **http and https only, ports 80 and 443 only**, ten seconds, ten megabytes
  — the upload's own cap — and a content type that is an image or a page.
- **One sentence for every failure.** „Z tohohle odkazu fotku nedostanu.“ A
  message that said _which_ address was refused, or what a host answered,
  would turn this endpoint into a way to ask the internet questions from
  inside the VPS and read the answers back.
- **A content type is a claim; ImageSharp reading the header is the fact**, so
  what comes back goes through the same gate an upload does before anything
  is done with it. It is re-encoded to JPEG at 2048 px with the metadata
  stripped, exactly as an upload would be: whatever was in somebody else's
  file does not get served from ours.
- **A page is read for `og:image`, then `twitter:image`**, with a regular
  expression over its head — rule 3 reaches the API, and ImageSharp is its one
  media dependency. The page is never kept, never shown, and never rendered.
  On `i.pinimg.com` the sized path is swapped for `originals` and tried first,
  because `og:image` often names a 736 px copy and that is visibly soft
  full-bleed on a phone; strictly best-effort, with the named one tried after.
- **The picture comes back to the phone** rather than being put on a dream.
  On the add screen there is no dream yet, so there would be nothing to put it
  on; and coming back here means it takes the same road a picked file does —
  the preview, the crop editor (D54), the same upload. One endpoint, no second
  write path, and nothing on the server to undo if somebody changes their mind.
- **Paired devices only, and twenty a minute per address.** A token is the
  first fence; the clock is the second, because this one makes the server work
  on request.
- **A shared link fills the field and waits.** Android's share sheet reaches
  `/pridat` through `share_target` in the manifest, and anything on the phone
  can hand the app a URL — so it is pasted into the sheet for a person to
  send, never fetched on arrival.

Two things the real internet taught in the first five minutes. A news page is
megabytes of script and the tag is in the first few kilobytes, so the read cap
now truncates a page instead of refusing it — refusing big pages meant
refusing most of the web. And the failure sentence was landing on the screen
_underneath_ the open sheet, where nobody could read it; it belongs where the
thing that failed was asked for.

Checked against the live internet: a Wikipedia article's `og:image` came back
as a 354 kB JPEG, the same picture by its direct URL did too, and the metadata
address, a private address, localhost by name and a cloud metadata hostname
were all refused in milliseconds without a connection being made.

---

### D57 — On the one morning a year, the anniversary is the nudge

PLAN.md §3.3 has wanted the anniversary reminder since M0 and §12 left it as
one line over the reel „until push is M5's“. Push has been built since M5 and
the worker still picked by `last_shown_at` and never read `achieved_at`, so the
line was seen only by somebody who opened the app on the right day — which is
the one thing a person who has stopped opening the app will not do.

- **It replaces, never joins.** One nudge a morning is the whole bargain of
  §3.6 — this app interrupts once and then leaves somebody alone — so a
  morning cannot hold both today's dream and an anniversary. The anniversary
  wins: tomorrow there is another daily dream, and there is not another first
  anniversary of this one.
- **„Před rokem“ over „Splnil se ti sen „Loď“.“** The heading is how long ago
  and the line is what happened, the two reading as one sentence, which is
  `formatAnniversary`'s sentence on the board split where a notification splits
  it. `rokem` for one year and `lety` above it; the verb agrees with _sen_, so
  the sentence is right for whoever is holding the phone (D21).
- **The photograph is the achieved one**, the dreamt one where there is none.
  The proof is the picture (D28) and this morning is about the proof.
- **It stamps nothing but the subscription.** The dream is achieved, so it is
  not on the reel and the pick cannot reach it; and „shown“ is a word about the
  board's own turn (D25, D36). The board still opens on its own pick, and the
  dream the nudge would have named is named tomorrow instead of being quietly
  spent on a morning it was not shown.
- **Two languages, one rule, mirrored test for mirrored test.**
  `Aspire.Domain/Anniversary.cs` is `anniversaryToday` in `board.ts`: this day
  and month in an earlier year, whole years and at least one, the most recently
  achieved when two fall together, and 29 February on 29 February — because the
  alternative is inventing a date it did not happen on. The day is the device's
  own through its offset, as the pick's is, so an anniversary belongs to the
  day the person is living in rather than to UTC's.

Two things that were already wrong and are fixed by going near them. The
payload's first field was called `dream` and carried the dream's title while a
field called `title` carried the fixed word „Dnešní sen“ — which no platform
has ever shown, because the service worker read `dream` as the heading and the
constant was dead from the day it was written. The payload now carries the
heading in `title` and nothing else, and the worker reads the old name second
so a nudge encrypted in the seconds a deploy takes still says the dream's name.
And the envelope was only ever tested as an envelope: `NudgeWorkerTests`
decrypts what the phone would have received, using the browser's half of RFC
8291 lifted out of `WebPushCryptoTests` into `PushEnvelope`, so the choice and
the Czech are checked together rather than one of them being checked twice.

It cannot be seen working before September 2027, when the first dream on the
board has an anniversary. That is what the worker test is for.

---

### D58 — A heart shortens the wait, and ten of them halve it

`POST /dreams/{id}/likes` has counted since M1 and no rule has ever read the
number. The tile and the dream's screen showed it and nothing else happened,
which makes it a score — and a score on a photograph is exactly what §2's
third principle keeps off the board. Either the heart does something or it
should not be there.

**The rule, in both languages:**

```
fuel = days since shown × (1 + min(likes, 10) / 10)
```

The daily pick keeps the shape D25 gave it — a dream already shown today
holds all day, a dream never shown comes before any that has — and where the
_oldest_ used to win, the most fuel wins now.

- **Ten hearts double it**, so a loved dream comes round twice as often as one
  with none: twenty days unloved and ten days at ten hearts are the same
  number. The eleventh heart does nothing.
- **The cap is the point, not a detail.** With no ceiling, ten loved dreams
  would take every morning between them and the rest of the board would never
  come back — D25's habituation with extra steps, which is the thing the daily
  pick exists to prevent.
- **Whole days, counted as the device counts days**, not hours elapsed. Two
  phones on one board work the pick out separately and must agree without
  being told (D36); a difference of seconds between their clocks must not be
  able to decide a morning. Ties fall to board order for the same reason.
- **Nothing else reads the count.** The tail of the reel is still a uniform
  shuffle (D30). One rule, one place to find out whether the heart speaks too
  loudly, and a fortnight of real swiping to say.

**The wallpaper reads the same rule.** `wallpaperPick` is the day's dream
first when it has a photograph, then the reel's dreams with a ready dreamt
photograph in fuel order, and Tapeta opens on those six already chosen instead
of on an empty grid — a lock screen is a thing you want to be right before you
are asked anything. `wallpaperCandidates` stays as it was for the choice by
hand, the wall included: a dream already lived is exactly the kind you want on
a lock screen, but what is _automatic_ should be what is still ahead.

**The count leaves the reel tile** — PLAN.md §8's eighth question, answered
yes. The tap stays, with the haptic, and the tile keeps the one bit that
matters: the heart is filled on a dream that has been fuelled at all and an
outline on one that has not. Filled in `--photo-ink` rather than in the ember,
because the accent stays off a photograph and the interface steps back there;
the ember heart and the number are both on the dream's own screen, where the
facts are. The count stays in the button's accessible name, because a tap with
no visible change needs to be answerable to something.

Two things found while writing it down. The plan's own worked example was
wrong arithmetic — „a hundred days with no hearts equals nine hearts and ten“
is true of no cap at all, and the sentence is fixed in place. And the C# twin
of `wallpaperPick` is **not** here, though §22.2 listed it: its only caller is
the keyed link §22.5 has not built yet, and a rule with no caller is a rule
that drifts from its twin unnoticed. It arrives with the endpoint that needs
it.

---

### D59 — The automatic six are the reel's, not the wall's

`GET /api/v1/wallpaper` with no `dreams` is today's six rather than an error:
`WallpaperPick`, the twin of the rule the Tapeta screen already starts from
(D58) — the day's dream first, then by fuel — so the phone and the server
answer the same question the same way, and the person sees on the screen what
the automation will fetch at 6:55.

- **The reel only.** `wallpaperCandidates` keeps the wall for the choice by
  hand, because a dream already lived is exactly the kind you want on a lock
  screen. But what is chosen _for_ somebody should be what is still ahead:
  a lock screen is the surface §3.5 exists to put a future in front of.
- **It stamps nothing.** A lock screen is not the board opening, and „shown“
  is a word about the board's own turn (D25, D36). If fetching the collage
  marked a dream shown, the automation would quietly spend the morning's pick
  before the person woke up, and the board would open on a different dream
  than the one it was about to.
- **Fixed order, no shuffle.** Two fetches of one morning have to be one
  wallpaper. The randomness the daily pick uses among dreams never shown is
  the only thing that can move it, and only on a board where nothing has been
  shown yet — which is a board that has not been used, not a board in use.

The old „Vyber aspoň jeden sen.“ is gone with it: no dreams named now means
the six, on both roads in.

---

### D60 — The lock-screen link is its own key

§3.5's purpose is the dream seen a hundred times a day without opening
anything. Built as a one-time export, the same six dreams stay on the lock
screen until somebody remembers Nastavení exists — and a picture seen a
hundred times a day stops being seen inside a week. That is the habituation
D25 and D30 fight on the reel, on the surface that habituates fastest.

So the phone fetches a new collage every morning by itself. Shortcuts can run
a URL at a time of day and set the picture from it; what it cannot do is hold
a bearer token without somebody typing one into it by hand, and a token typed
by hand is a token out of the app and a typo away from a wallpaper that
silently never changes.

**The link carries its own key.** `boards.link_key`: 32 bytes from the
operating system's randomness, base64url, unique, and `GET /api/v1/w/{key}`
answers with that board's six and nothing else.

- **The key is the whole permission**, and it is exactly one collage wide. No
  dreams to read, no writes, no token, no way from it to anything else. A link
  that leaks costs six photographs a day until it is replaced, and replacing
  it is one tap: `POST` makes a new key over the old one, so making is also
  revoking and there is only ever one to keep track of.
- **Not hashed, unlike the pairing code.** A code is twelve digits somebody
  types, so a dump of the table is worth guessing against and PBKDF2 earns its
  milliseconds (D21). This is 256 bits nothing guesses, and it has to be looked
  up by rather than compared against, because the request arrives with no board
  attached to it at all.
- **It stays out of the access log.** `location ^~ /api/v1/w/` in nginx turns
  logging off for that path. A capability in a log file is a capability handed
  to whoever reads, rotates or copies the file — and an access log is the most
  copied file on a box.
- **Ten a minute per address**, in the API and again in nginx. The fence is
  for the rendering — six full-size photographs decoded, cropped and drawn on
  every hit — not for the key, which needs none. Per address rather than per
  key, because the cost is paid by an address whichever key it presents; and
  ten rather than the one a morning needs, because a 429 to a phone's
  automation is the silent failure this was built to avoid.
- **`no-store`**, so nothing between the server and the phone keeps yesterday's
  morning.
- **404 for a key that opens nothing**, with no sentence: „revoked“ and „never
  existed“ must not be two different answers.

**What comes back to the client is the path, not the URL.** The browser knows
its own origin for certain; the server behind nginx would be reading its own
scheme out of a header a client can set. The screen puts the two together and
bakes in this phone's canvas and offset, because the link is static and
whatever it carries is what will be asked for every morning from now on.

The screen says the rest in Czech — Zkratky → Automatizace → Denně v 6:55 →
_Získat obsah URL_ → _Nastavit tapetu_ — including the two things that make it
fail silently: the wallpaper has to be a plain photo wallpaper rather than a
shuffle, and _Zeptat se před spuštěním_ has to be off.

Checked end to end against a running server: the link answers a request with no
`Authorization` header at all with a 107 kB JPEG at exactly the phone's canvas,
a wrong key and a revoked key both 404, an impossible offset is a 400, and
making a new link stops the old one in the same request.

This is also the mechanism §3.7's share link wants — a key per dream rather
than per board, the same shape — so the last line of the plan comes nearly
free.

---

### D61 — A shared dream is a page this server writes

PLAN.md §3.7 has had one line since M0 — _share a single dream via a signed
link_ — and it is the last thing in the plan that is not a milestone. The
mechanism arrived with the lock screen (D60): a key that is its own permission,
against one dream instead of one board. `dreams.link_key`, `POST` to make one
over the old, `DELETE` to remove it, and `GET /s/{key}` to read it.

**It is a page, not a route of the app.** The whole point is somebody who has
nothing — no app, no pairing code, no account — and who is sent a link in a
message. A message shows a preview only if the _server_ put the picture in the
head; a client-rendered route of the PWA arrives at WhatsApp as a bare URL with
no title and no image. So the API writes HTML, for the first and only time.

- **One file with nothing in it.** No script, no stylesheet, no font, no
  analytics, nothing fetched from anywhere else, and no link back into the
  board. `noindex, nofollow`, because a dream somebody was sent is not a page
  to be found by searching, and `no-store`, because a page held in a cache is
  a dream outliving the decision to share it.
- **The colours are literals**, which rule 1 forbids everywhere else. Same
  reason `CollageLayout` has them: this is rendered by C#, which cannot read
  `tokens.css`. It is the reel's tile in miniature — the photograph, a scrim,
  the name and the line — and it is the one place in the app where that has to
  be said twice.
- **Somebody's own words are escaped, and only five characters are.**
  `WebUtility.HtmlEncode` also turns every character above ASCII into a numeric
  entity, and this app is written in Czech: „Bydlím u lesa“ would go out as a
  string of `&#…;` for twice the bytes and no benefit, because the page says
  `charset=utf-8`. The five that matter are escaped in element text and in
  quoted attributes alike. Checked against a dream whose title is an `<img>`
  tag with an `onerror`: the page hands it back as text, and a browser parsing
  the result creates no element and runs nothing.
- **The preview card is a JPEG the server renders** at 1200×630, through the
  collage renderer that already knows how to fill a rectangle with a photograph
  cropped where the person put it (D54). The files on disk are WebP, and some
  chat apps still will not draw one in a preview card — a card that silently
  fails is the whole reason this is a page rather than a route. A dream with no
  photograph claims no card at all, because a preview with a picture that 404s
  looks broken where no preview looks deliberate.
- **The path is `/s/{key}`, outside `/api/`.** This is the one URL in the app a
  person reads off a screen and pastes into a message. `access_log off` on it
  in nginx, for the reason D60 gives, and the Vite dev proxy learned it too so
  the link the app shows is not broken on a laptop.
- **The affirmation is on the page; the message says only the name.** A
  message's own text lands in a chat list, on a lock screen, in a notification
  somebody else can be standing next to — a different audience from the one the
  link was sent to. The why is on neither: it is the sentence the dream is
  explained to himself with.
- **The dreamt photograph**, the achieved one only when that is all there is.
  The page is a tile, and a tile shows the dream (D28).

`ShareKey` is now where both keys are made, so „a key that is its own
permission“ is one idea with one implementation rather than two that drift.

Checked end to end against a running server: the page renders the photograph
with the name and the line over it; the card comes back as a 30 kB JPEG at
exactly 1200×630; the head carries an absolute `og:image`, `og:title`, the
affirmation as `og:description` and `summary_large_image`; the page has zero
scripts and zero `<link>` elements; a wrong key and a revoked key both 404.

### D62 — The reel reads the rung the phone is, and there is no `srcset`

The reel is full-bleed on a portrait phone with `object-fit: cover`. A 3:4
photograph at the 1280 rung is 960×1280, and on a 3× iPhone — 1290×2796
device pixels — cover scales it by the larger of 1290/960 and 2796/1280,
which is 2.2×; on a 2× phone it is 2.0×. Every photograph on the reel was
drawn at twice its pixels, and the crop editor's zoom (D54) multiplied that.
Instagram's stories are 1080×1920 on the same screens, a 1.5× stretch, and
that is the most a photo app that lives on quality accepts. The 2048 file is
1.4× and 1.2× on the same screens, and it already existed for every upload,
written as the archive and read by nobody.

- **A phone reads `large`, a laptop reads `screen`.** `rungFor` in
  `dreams/photos.ts` is the one rule: the 2048 rung when
  `devicePixelRatio` is 2 or more and Save Data is off, 1280 otherwise.
  Every phone is 2× or 3×; a laptop at 1× and a person who asked the browser
  to spend less get 1280, which is right for both. The reel, the dream's own
  screen and the cache's prefetch read `reelUrl`; nothing picks a rung by
  hand (rule 12).
- **Not `srcset`.** The browser choosing per image would put a URL in the
  cache that the prefetch never asked for, and D39's window is a promise
  about bytes that a browser picking for itself would break. One
  deterministic rule, the same on the tile and in `offline/cache.ts`, is
  what keeps the swipe served from the cache.
- **The cost is the compromise.** The reel's dream goes from 200 kB to
  about 350 kB at D23's amended quality — the whole board on wifi from
  20 MB to 35 MB, a morning of ten swipes from 2 MB to 3.5 MB. `policy.ts`
  already says storing tens of megabytes is nothing and fetching them on a
  metered plan is the question; the numbers change, the answer does not.
  §8's fourteenth question, answered as recommended; the alternative was a
  new 1600 rung and a sweep to make it for every photograph already there.
- **`full` keeps its name on disk and becomes `largeUrl` on the wire.** A
  year of cached URLs is a year of cached URLs (D23), and the collage and the
  share card read the file by its name. The wire names the rung for what it
  is to a phone.
- **The wall, the circle, the nudge stay at `screen` and `thumb`.** A pair
  at half width, a 40 px circle and a notification's picture have the pixels
  they need.
- **§24.1's measurement is his.** The arithmetic says the difference is
  visible; the plan asked for one dream on his phone at both rungs before
  the change, and the change went in on the arithmetic. If the phone says
  otherwise, `rungFor` is one line to revert and nothing else moves.

### D63 — The thumb goes under the reel's picture, blurred

Between the swipe and the photograph there was the sky (D26, D52), and on a
slow connection the sky is what a swipe past the window showed for a second.
Pinterest paints each pin its dominant colour before the picture lands;
Instagram draws a tiny blurred copy. This app had the tiny copy already:
`thumb` is on the order of 25 kB, and on a device that has opened the Seznam
it is in the cache.

- **A second `<img>` in the same absolutely-positioned frame**, `.dream__under`,
  blurred by CSS, bled 6 % past the frame so the blur has no soft edge, with
  the same `photoStyle` as the picture so it is cropped to the same point;
  the picture comes after it in the flow and covers it as it decodes. It
  adds no height to the scroll region, so rule 14 stands.
- **The sky stays for a dream with no photograph at all.** That is a
  different sentence (D52), and the tile without a picture is where the
  picture is asked for.
- **The cache asks for the thumb before the picture of the same dream**, in
  `tileUrls`, so on a metered window the shape arrives before the pixels.
  `WINDOW_KEEP` goes from forty files to eighty: forty dreams' worth, two
  files each. The whole board on wifi already fetched the thumbs for the
  Seznam, so there the cost is nothing; a ten-swipe morning goes from
  3.5 MB to 3.75.
- **Not a dominant colour.** It is Pinterest's trick and it would be seven
  bytes on the wire, but colour at scale is the sky and nowhere else
  (rule 4), and a tile painted a photograph's own colour is a third accent.

### D64 — A board weighs what its photographs weigh, and two gigabytes is full

PLAN.md §7 said "500 MB per user" and it was never built; it would also have
been wrong, because a hundred dreams at two photographs is about 150 MB and
a board is meant to reach a hundred (D30).

- **`dream_images.bytes`**, the sum of the three files, written by the worker
  beside `width` and `height`; migration `Bytes`. The worker's sweep weighs
  the rows made before the column existed, once, from the disk. A board's
  size is one sum over the column rather than a walk of the volume, which is
  why the column exists.
- **Two gigabytes a board**, `ImageService.MaxBoardBytes`: the number at
  which something other than dreaming is happening. The upload's own length
  counts as what is arriving, and a board at the ceiling answers 409 with
  „Nástěnka je plná. Smaž pár fotek, které už nepotřebuješ.“ — a 409 as a
  full Teď is (D53), because it is not a bad request but a board that has to
  lose something first. §8's fifteenth question, answered as recommended.
- **The number is shown before the sentence is.** `GET /api/v1/board`
  answers the board's name, its photographs, their bytes and the ceiling;
  Nastavení → Stahování has a „Na serveru“ line under „Na telefonu“, and
  `board list` prints the photographs and the megabytes beside the code, so
  the operator sees the same number from the other side.
- **The rule for the disk, decided now and not built.** Media leaves the VPS
  only when the volume passes half of what `df -h /var/lib/docker` shows,
  and on this arithmetic never for photographs. When it does, it is one
  S3-compatible bucket in the same jurisdiction behind nginx's `proxy_pass`
  and `proxy_cache` under the same `/media/` path, so no URL changes and the
  service worker's cache-first stays true; `MediaStore` grows an interface
  and the bucket is its second implementation. Video, if it is ever built,
  is capped by length rather than by bytes — fifteen seconds — transcoded in
  the same worker, never in the request, with its poster frame written
  through `ImageProcessor` so every screen that shows a photograph keeps
  working.

### D65 — Smazat waits six seconds instead of asking

Deleting a dream was one tap and no way back. The server takes the row and
its photographs together, so there was nothing left to put back afterwards
either — „je pryč“ in a toast was a statement, not an offer.

A dialog was the obvious fix and is the wrong one. It taxes every deletion a
person meant to make, which is nearly all of them, to catch the rare one
they did not; and the dream screen's own note has said since M1 that this
app does not ask, as Prosper does not. So the fix is not to ask but to
**wait**: `dreams/deleting.svelte.ts` holds the request for as long as the
toast that announces it stands, and „Vrátit“ cancels it before it is ever
sent. Nothing is deleted and then restored, because a photograph cannot be.

- **Every screen drops it at once, and keeps it dropped.** The board, the
  Seznam, Teď and the Síň slávy all filter the held id. It stays on that list
  after the request has gone, because a board fetched before the deletion
  landed still carries the dream, and a tile that flickers back for a frame
  reads as a deletion that failed. Only „Vrátit“ takes an id off.
- **The board keeps the server's answer and a filtered view of it**, so
  „Vrátit“ puts the tile back without a second trip.
- **Leaving commits.** The window is for the finger that slipped while the
  screen was being looked at; anybody who closes the app or switches away
  meant it. `pagehide` and `visibilitychange` send what is held, with
  `keepalive` so the request outlives the document.
- **A request that fails puts the dream back**, because the server still has
  it and hiding it would be a lie.
- **Six seconds**, which is what a toast carrying an action already stood
  for (`ui/toast.svelte.ts`). One constant, `UNDO_MS`, so the two cannot
  drift.

Petr asked for this after M8; it changes a behaviour M1 chose deliberately,
which is why it is here rather than only in the diff.

### D66 — The rung reads the connection, not only the pixel ratio

D62 gave the 2048 rung to every screen at 2× and up. Every phone is 2× or 3×,
so every phone read it — including one on mobile data, at about 350 kB a
swipe against 200. The compromise D62 struck is worth striking where the
bytes are free and not where somebody is paying for them.

`rungFor` now asks two questions: enough pixels to want it, and a connection
that is not metered. `offline/policy.ts`'s `metered()` already folded Save
Data in, so asking the browser to spend less gives the same answer and
`savesData()` is gone — one question, one place.

**A browser that will not say is treated as free.** Safari has no Network
Information API, so `metered()` is `null` on every iPhone. Reading that as
metered would mean the phone this app is built for never sees the rung that
was built for it; reading it as free costs a few hundred kilobytes a swipe
to somebody on cellular Safari. The second is the better mistake.

### D67 — A control that writes wears the lock instead of remembering it

D24 says every write rests without a signal, and each control remembered it
by hand: `disabled={busy || !connection.online}`, with the second half each
screen's to forget. `use:writes` in `offline/writes.svelte.ts` is that rule
with a name — a control wears it instead of a `disabled` of its own, and
`use:writes={() => busy}` where it has a reason of its own, the two or-ed.
A control that writes now cannot be built without the lock on it.

- **The screens' own sentences stay where they are.** „Bez připojení. Seznam
  je z paměti a nový sen počká na signál.“ is true about the Seznam and says
  nothing about Upozornění. A lock is one rule everywhere; what to tell
  somebody about it is that screen's to say. Only the two a _form_ shows are
  shared, as `cannot(online, verb)`, because a form has no room to say more
  than which verb it cannot do.
- **The tab bar's ＋ keeps `aria-disabled`.** It is a link, and a link has no
  `disabled` to set.
- **Tapeta's link buttons were already covered** by a guard on the whole
  screen, which is the other honest shape: a screen that cannot work at all
  without a signal says so once and shows nothing.

### D68 — Components get tests, in happy-dom rather than a browser

Every rule in this app lives in a `.ts` module a component imports, and all
of them were tested; not one test rendered a component. So what a screen
does with those rules — which element is on the page, whether the control
that writes is dead — was checked by hand or not at all, and the extraction
of `ReelTile` is exactly the kind of change that can break one silently.

Vitest grows a second project: `server` in node as before, `client` in
happy-dom for `*.svelte.test.ts`. The config already half had this shape —
the `server` project excluded `*.svelte.test.ts` with nothing on the other
side of the exclusion.

- **happy-dom, one dev dependency**, against `vitest-browser-svelte` and a
  Playwright download. What these tests ask is which element is on the page
  and what a click does, not how it paints; the things a real browser would
  add — layout, scrolling, frames — are the things `apps/web/CLAUDE.md`
  already says cannot be checked in a hidden pane either.
- **Svelte's own `mount` and `flushSync`**, no testing library: rule 2's
  reason applies to test helpers too.
- **`resolve.conditions: ['browser']` on that project**, or Svelte hands
  back its server build, whose `mount` exists only to say it is not the
  browser.
- **The rules keep their own tests.** A component test that re-checks
  `photoComing` is a slower copy of a test that exists; these check the
  wiring only.

### D69 — The endpoints get tests over a real request

Every API test reached past the request and called a service with a
`DbContext` the test had made. That checks the rule and not the road to it:
the bearer header, the status code a problem comes back as, the multipart a
phone actually sends, the camelCase and kebab-case the TypeScript client is
written against (rule 6). All of that is wiring only a real request touches,
and the `vapid` entrypoint and the media volume were both failures of wiring
that no unit test could have caught.

`ApiFactory` is `WebApplicationFactory<Program>` on the laptop's own
configuration — SQLite in a temp file, a media root beside it, pairing on
`000000` — so it exercises `Program.cs` whole: the JSON options, the rate
limiters, `EnsureWritable`, the schema guard, the seed. `public partial
class Program` was already there for exactly this.

Fifteen tests, and each one is a thing the road can get wrong rather than a
rule restated: 401 without a token and for a token nobody knows, a wrong
pairing code, a dream through create-read-update-delete with its diacritics
intact, a 404 for another board's dream, a 400 with a sentence for an empty
one, the eleventh dream on Teď as a **409 and not a 400**, the board's usage
against its ceiling, a photograph as multipart answering 202 with three URLs
and its focal point, a file that is not a picture, a crop outside the
photograph, and a key that opens nothing as a bare 404 (rule 17).

One test-only package, `Microsoft.AspNetCore.Mvc.Testing`. Nothing ships.

### D70 — The notification's badge is the mark as a silhouette

The push notification's `badge` pointed at `icon-192.png`, and on Android it
arrived in the status bar as a plain white square.

Android does not draw a badge as a picture. It takes the image's **alpha
channel** and fills it with the system accent, so the shape has to _be_ the
transparency — and `icon-192.png` is a full-colour sun on an opaque rounded
plate, whose alpha is a rounded square and nothing else. It was doing exactly
what it was asked.

`badge-96.png` is the same sun and horizon as `static/icon.svg`, white on
nothing, with the plate gone because the plate was the thing showing. Drawn
from the icon's own geometry at 8× and resampled, because 24dp is where a
hard edge goes jagged. 96 px is Android's own size for this, and iOS ignores
`badge` and uses the app icon, so nothing there changes.

**Amended the same day.** The large `icon` does not stay the colour one
either: a notification is chrome, and chrome in this app is ink and white.
Both are the mark as a **white outline** now — stroked rather than filled,
the same arc and horizon line the rest of `ui/Icon.svelte` is drawn in.

`icon-notify-192.png` carries it on the app's own ink; `badge-96.png` is the
same outline with the plate taken off. The plate is there on the first
because the notification shade is white in light mode, and a white outline
on transparent is an empty square there — both were rendered against black
and against white before this was settled.

### D71 — Across the reel to change which reel it is

The two reels (D53) were reachable only through the segment in the floating
chrome — a small target at the top of a screen usually held in one hand. A
reel is a thing you move with your thumb, so moving sideways on it now moves
sideways through it.

- **It sits beside the pager, not inside it.** `ui/pager.ts` owns the
  vertical — the fence, the wheel, rule 14's one dream per gesture — and
  `ui/sideways.ts` takes only a gesture the pager ignores. The two never read
  the same numbers.
- **The doubt always goes to the pager.** `touch-action: pan-y pinch-zoom`
  means the browser keeps every vertical pan for itself, and a swipe counts
  only when it went **1.4 times further across than down**, which is about 36
  degrees off the horizontal. A thumb flicking up the reel at an angle is
  still a dream. Nothing calls `preventDefault`, so a gesture that turns out
  to be vertical after all was never interrupted.
- **Fifty-six pixels**, a third of the way across the narrowest screen this
  runs on: far enough to mean it, near enough one-handed.
- **It stops at both ends rather than wrapping.** A swipe that came back
  round would mean one gesture going two ways, and neither answer would be
  guessable.
- **Teď with nothing on it takes the swipe too.** It is a page and not a reel
  (D53), and a gesture that carried somebody there has to carry them out or
  it strands them.
- **Left and right arrows do the same**, since the pager has up and down and
  the segment is a choice of one from two.

### D72 — Up to five reminders a day

The nudge was one a day per device: a single `at_minutes`, and a date stamp
that kept the day to one (D34). Petr asked for several.

- **`times` on the row: one to five, in order, no repeats.** A short string,
  „420,720“, through a value converter. At most five small numbers that are
  only ever read and written together with nothing pointing at one of them;
  a child table would buy a join and a second place for one device's
  schedule to be half-written.
- **Five.** What is being built is a habit, not an alarm clock: past about
  five a day a notification stops being noticed and starts being dismissed,
  and a dreamboard that is dismissed five times a day is worse than one that
  speaks once. It is also what fits on the screen without the list scrolling.
- **`last_sent_minutes` beside `last_sent_on`.** A date alone cannot say
  which of today's reminders have gone. The whole clock would be too much:
  the list is sorted, so „everything up to and including this minute is
  done“ is one number that cannot drift out of step with it.
- **`DueAt` replaces `IsDue`** and answers _which_ reminder is owed rather
  than whether one is. **The latest one wins**: a worker that comes back to
  two inside their grace sends the most recent, and stamping it takes the one
  it overtook with it. The point is a dream now, not a backlog at nine. The
  120-minute grace is unchanged, and still the reason nothing arrives at
  bedtime.
- **The migration adds, carries across, and only then drops.** EF scaffolded
  the `DropColumn` first, which would have thrown away the hour every
  subscriber had chosen. It also carries `last_sent_on` into
  `last_sent_minutes`, so a device already nudged that morning does not get a
  second one on the morning this ships. Going back keeps the first of
  somebody's reminders rather than seven o'clock for everybody.
- **The screen is a row per reminder**, and the last one cannot be removed:
  „no reminders“ is what Vypnuto means, and a list that could empty would be
  a second way to say it that the mode would then disagree with. The ＋ adds
  the first free hour after the last, because two reminders at the same
  minute are one reminder and the button would look broken.

### D73 — A shared dream is a card, not the window

The shared page (D61) put the photograph on `main` as a background at
`cover` over the whole viewport. Whatever shape the window was, the
photograph became that shape: a portrait 4:5 picture opened on a laptop was
cropped to a letterbox, and on a tall phone cropped the other way.

It is a card now — the picture as an `<img>` at its own **4:5**, which is
what `.dream` already is, in a 25rem card centred in whatever window this is.
That is the same phone-shaped column the app itself is on a desktop, so a
dream somebody was sent looks like the dream it is rather than like a
wallpaper.

**The crop is the person's own.** Rule 16 says every surface that shows a
photograph reads `focus_x`, `focus_y` and `zoom`, and this page is a surface
like any other; `SharePage.FocalStyle` is the server's copy of `photoStyle`,
with the invariant culture on the numbers because CSS has never heard of a
Czech decimal comma.

Everything D61 decided stands: one file, no script, no font, no stylesheet,
`noindex`, and no link back into the board. „Aspire“ under the card is a
word, not a way in.

### D74 — Glass is one decision, and the segment wears it

Two things that had drifted apart: how a glass surface frosts, and which
surfaces get to be glass at all.

**The blur was a literal at every call site** — 24 px on the tab bar and the
toast, 20 on the chips and the round buttons, 16 on a pill over a
photograph, 12 on a badge — and `@supports not (backdrop-filter)` existed on
the tab bar and nowhere else. So a browser that cannot blur got an opaque
bar and seven translucent things over text, and „turn this down“ had no
answer at all.

`--glass-blur-bar`, `--glass-blur` and `--glass-blur-photo` are in
`tokens.css` now and every surface reads one of them. Three strengths
because there are three jobs: what floats over a whole screen, what sits on
the ground, and what sits directly on a photograph where the scrim is
already doing half the work.

- **`prefers-reduced-transparency: reduce`** is answered once, at the bottom
  of `tokens.css` — last in the file so it wins in either theme, and written
  as `var(--surface)` rather than a colour so it is the right ground in
  both. The glass simply becomes the surface it was pretending to be.
- **`@supports not (backdrop-filter)`** is one rule in `app.css` covering
  every glass class, with the things on a photograph going to a flat dark
  scrim rather than 18 % white over a picture.
- **It already works on all three platforms**: iOS Safari through
  `-webkit-backdrop-filter`, Android Chrome and every desktop through the
  unprefixed one. The prefixed line is not legacy and cannot be dropped —
  WebKit still ships only the prefixed property. What actually breaks glass
  is an ancestor with `filter`, `transform`, `perspective`, `will-change` or
  `contain`, which makes a new backdrop root and leaves the blur sampling
  nothing; so a glass surface's ancestors are part of its contract.

**The reel segment is the bar's glass.** It was a glass track with a dark
`--pill` stamped into it, which is the card's way of marking a choice — and
over a photograph the ink is supposed to step back (rule 4). It now carries
the same masked rim, the same top-edge sheen and the same sliding lens as
`.tabbar`, so the two controls on the board read as one material. `--slots`
on the element and `--slot` from the component, exactly as the bar does it,
and the chosen segment is full `--ink` on the lens with nothing of its own to
paint.

That retires D70's sibling fix from the day before: there is no pill left to
be the wrong colour on.

---

## After §28 · 2026-09-17 — nine from using it

Nine things Petr asked for after living with the finished app (PLAN.md §29).
Four of them overturn something written above, and he was asked about those
before a migration was written: the order he drags is the Seznam's and not the
reel's, a collage shows on the reel and on the dream's own tile, a mat is one
of a palette chosen per photograph, and starting over takes the dreams and
leaves the pairing.

### D75 — The frame is pinned to the viewport, not measured from it

An installed app launched cold on Android sometimes came up with the tab bar
below the bottom edge of the screen, and a reload put it back. The frame was
`height: 100dvh` on `.app`, with `html` and `body` the same: a length worked
out from the viewport once. At a cold start that viewport can still be the
splash's — system bars not yet taken out of it — and a `dvh` length is not
always worked out again when the real size arrives.

`.app` is `position: fixed; inset: 0` now and `html, body` are `height: 100%`.
`inset: 0` is the viewport itself rather than a length measured from it, so
there is nothing to go stale. It is still positioned, so it is still the
containing block the bar and the toast are placed against, and the two insets
with `margin-inline: auto` are what centre a 34rem column in a wider window.

**Not reproduced on the laptop**, where a viewport never starts the wrong
size. The layout is unchanged there at phone and desktop widths; the phone is
the test of the fix.

### D76 — A sheet is pulled down to put it away

Prosper's sheets are; Aspire's had a grab bar that was „not a control“. Where
Prosper does X, Aspire does X.

- **The handle takes the gesture and the body is its sibling.** The grab bar
  and the title are `touch-action: none`; what the sheet holds scrolls in
  `.sheet__body` under them. So there is nothing to arbitrate: a pull cannot
  start inside the form and a scroll cannot be stolen by the sheet. The panel
  itself no longer scrolls, which also keeps the handle where a thumb can find
  it in a long sheet.
- **Prosper's numbers**: a third of the panel's own height and never under
  72 px, or a flick past 0.5 px/ms that has gone at least 12 px. Upward it
  gives a sixth. `ui/pull.ts` holds them, with the test Prosper never wrote.
- **The screen still decides.** A pull asks to close exactly as escape and the
  dim do, so a sheet in the middle of saving stays up and springs back.
- **A hidden „Zavřít“** is the last thing in the panel: a pull is not
  something every assistive technology can produce, and a touch screen reader
  has no escape key.

### D77 — The board is counted, and a dream says when it was last changed

`UpdatedAt` had been on the row since M0 and nothing read it. It is on the
wire as `updatedAt`, on the dream's screen as „Naposledy upraveno“, and the
latest of them is under the Seznam's new stats bar.

- **What moves it is the dream being changed**: its words, its state, its
  area, its year, a photograph put on it, moved or taken off, its collage's
  template. **What does not** is anything about the board: a heart, the day's
  „shown“ stamp, a place in the Seznam, and — changed here — being put on Teď
  or reordered there, which used to stamp all ten. The date has to mean „I
  last worked on this one then“ or it means nothing.
- **Four numbers and a date**: celkem · sním · plním · splněno. No share
  done, no streak, no pace. A count says what is on the board; a percentage
  starts saying how he is doing, and §2 says this is not a tracker.
- **A number is a button.** It narrows the list to the dreams it counted, the
  way the search narrows it to the ones it found, and the two stack. The line
  numbers stay each dream's place in the whole list (D48).
- **„dnes“ and „včera“** for the two days that have a name, by the device's
  own calendar, and the long date after that.

### D78 — A line of the Seznam slides aside

Swiped to the left, a line shows a tray: the hearts with their count, Sdílet,
Smazat.

- **D58 is amended, not broken.** It took the count off the _reel's tile_
  because a number on a photograph is a score. A line in an inventory is where
  a count belongs — and even here it is behind a swipe rather than in the
  column. The heart in the tray is a heart: a tap adds one.
- **Across has to win clearly.** A thumb scrolling a list drifts sideways all
  the time, so a gesture is a swipe only when it has gone past 8 px _and_
  further across than down, and the first answer stands for the rest of the
  gesture. `touch-action: pan-y` leaves the scroll the browser's.
- **One tray at a time**, shut by a tap anywhere else or by the list
  scrolling, and `inert` while it is shut — a tray nobody can see must not be
  something a keyboard can land on. The swipe is a shortcut: all three are on
  the dream's own screen, which is where a keyboard reaches them.
- **Smazat here is D65's**, the same six seconds and the same „Vrátit“.
  `dreams/letgo.ts` is the one function both screens call, and `ShareSheet`
  the one sheet, because two screens doing the same thing is one thing.

### D79 — The Seznam's order is his own (D30, D44 and D48 amended)

D30 dropped drag to reorder, D44 made the Seznam newest-first, D48 numbered it
on that basis. Petr asked for both ways of moving a line: drag it, and type
over its number — „it is number 3, I rewrite it to 1, what was 1 is 2 and what
was 2 is 3“.

- **Both are one move**: this dream, to that line. `PUT /dreams/{id}/place`
  takes one number, the server counts the whole board off again from nought,
  and `dreams/order.ts` does the same on the device so the row moves under the
  finger rather than after a round trip. A refusal puts the board back.
- **`sortOrder` finally means what its comment always said.** Lowest first. A
  new dream goes in front of the lowest, so it is still line one, which is
  D44's reason and still true. The migration turns every `sort_order` over —
  no column changes — so the list reads exactly as it did the day before.
- **D30 stands for the reel.** Vše is still the day's pick and a shuffle
  (rule 22): a fixed route through a hundred dreams is still the wrong idea
  for the _queue_. The Seznam is the inventory, and an inventory is allowed a
  shelf order. Petr was asked, and chose this over the reel following it.
- **A narrowed list cannot be reordered.** Between the lines numbered 4 and 17
  there are twelve nobody can see, and „above 17“ does not say which.
- **The server is asked for a neighbour's line, not a count.** A dream inside
  its undo window (D65) is off the screen and still on the server, so the
  screen's third line can be the server's fourth; `serverPlace` finds the
  dream that will be under the moved one and asks for _its_ line.
- **Dragging is by the grip, or by the line after a long press.** The grip is
  `touch-action: none` and starts at once; the long press is 420 ms, and a
  `touchmove` listener that was on the list before the touch began is what
  stops the browser scrolling under a held line. Every row is exactly as tall
  as the next — the hairline is drawn on the face, not a border between two —
  because a drag is arithmetic on that one number (`ui/reorder.ts`), and near
  either edge the list scrolls under the line.
- **The number is the accessible way.** A grip does nothing when it is
  pressed, so it is hidden from a screen reader and out of the tab order, and
  the number beside it is the same move said in a way a keyboard can say.

### D80 — Začít znovu takes a sentence

A button that deletes every dream and every photograph on the board.

- **D65 does not scale to it.** Smazat waits six seconds because it is one
  dream and the request can be held. A hundred dreams and their files cannot
  be held in a toast: they are gone from the disk the moment the server says
  yes. So this is the one place in the app where friction is the point.
- **Prosper's friction, word for word**: „začínám znovu“ typed out, case,
  accents and stray spaces forgiven. A confirm is dismissed by the same tap
  that opened it; thirteen characters cannot be muscle memory. The server
  checks the phrase again (`ResetPhrase`), so emptying a board takes the
  sentence as well as the URL.
- **What goes**: dreams, photographs, Teď, share links. **What stays**:
  pairing, devices, nudges, the lock-screen link, appearance — everything
  about the board rather than about a dream. Petr chose this over unpairing.
- **It says how many** before it takes them, and that it is every device
  paired to the board. Prosper offers a backup first; Aspire has no export to
  offer, and the VPS's nightly `backup.sh` is the only way back.

### D81 — A photograph can be shown whole, on a mat (D54 extended, rule 16)

Photographs that looked right on the dream's tile and in the Seznam were
„zoomed, in bad quality“ on the reel. The reel's frame is a phone screen,
about 9:19. A landscape photograph filling it keeps a third of itself and is
drawn at about 1.7× its pixels even from the 2048 file.

- **`fit` on the photograph: `fill`, as every one was, or `whole`.** Whole is
  all of it, contained, on a mat, and `zoom` then scales up from there.
- **The point means the same in both.** Along each axis the picture sits at
  `p × (frame − picture)`, which is `object-position`'s own rule and is as
  true of room to spare as of overhang. So one line of arithmetic moves a
  picture that hangs over its frame and one that floats in it (`room`,
  `dragged`); only the sign differs, and with it which way the point runs
  under a finger. The client still sets `object-position` and a `transform`
  and does no arithmetic at render time.
- **The mat is one of six**, per photograph: `--mat-night`, `charcoal`,
  `umber`, `dusk`, `ember` in `tokens.css`, and `blur` — the photograph's own
  thumb, D63's, put to a second use. A palette rather than a colour picker
  because a colour exists in one file (rule 1); all dark, because the words on
  a tile are white; theme-independent, because a mat belongs to the print.
  Dusk and ember are the two accents with the light taken out, not a third.
  Petr chose this over one colour for the app and over a free picker.
- **Surfaces with no room for a mat still fill**: the Seznam's circle, the
  wall's pair, a collage's cell, the wallpaper, the shared card. They read the
  point alone (`DreamImage.CropZoom`, `photoStyle`), because twice the whole
  picture is not twice the crop.
- **A landscape picture picked a moment ago opens whole** in the editor, and
  can be turned over. Photographs already on the board are left as they are.

### D82 — Up to five photographs, cut into a collage (rule 12 extended)

- **The five are all dreamt.** Rule 12's two kinds stand: the achieved
  photograph is its own picker and is not a sixth cell (D28). The sixth dreamt
  one is a 409 with a sentence.
- **A template is a CSS grid and nothing else** — tracks, and one `grid-area`
  per photograph — so one set fits a phone screen and a 4:5 print alike. Three
  for every count from two to five (`dreams/collage.ts`), and a test that each
  tiles its grid with no hole and no overlap. Every cell crops to its
  photograph's own point (D54).
- **A dream stores a variant, 0 to 2, not a name**, because what it picks
  among depends on the count, and a number survives a photograph being added
  or taken away. The server never draws one.
- **The first photograph matters twice**: it is the cover everything small
  shows — circle, wallpaper, shared card, notification, the wall's pair — and
  in every template with a big cell it is the big cell. So „Jako první“ is the
  only reordering there is. Petr chose the reel and the dream's tile for the
  collage over the dream's tile alone.
- **A new photograph stands behind the last**, not at „how many there are“:
  with one taken out of the middle that was a number two rows shared, and the
  tie fell to the id, which is random.
- **The shelf** on the dream's screen adds (a file or a link), places, puts
  first, takes away, and offers the three templates as diagrams drawn with the
  collage's own grid, so the diagram cannot drift from the collage. With one
  photograph the tile is the `PhotoPicker` it always was.
- **Offline keeps every cell** at the one rung a cell reads — the 1280 — so
  the prefetch still asks for the file the tile will show (D62).

### D83 — The Seznam narrows to Teď (D48 and D77 extended)

Petr asked to filter the Seznam to the ten on Teď „using some sort of switch“.

- **The board's own words, in the board's own control**: a full-width
  `.seg--soft` Vše · Teď under the counts, as the board's empty Teď wears it.
  Teď carries how many are on it, quieter than the word.
- **Not a fifth count.** Teď is not a state: the four figures add up to
  celkem, and a dream on Teď is already counted in one of them. A switch also
  says there are two things to choose between, which is what the board's
  segment says.
- **In the list's order, with the list's numbers** (D48). The ten in rank
  order would read 9, 2, 31, 4; their order is what `/ted` and the reel are
  for. Like any narrowed list it cannot be reordered (D79).
- **It stacks** with a count and the search, and „Ukázat všechno“ clears all
  three. An empty Teď answers about Teď — „Na teď zatím nic nemáš“ and
  „Vybrat sny“ to `/ted` — rather than „nic takového“.
- **The three narrowings are a SvelteKit snapshot**, so back from a dream
  opened off a narrowed list is the same list; the tab bar still opens the
  whole one. A dream written while the list is narrowed shows the whole list
  again: it lands on line one, which a narrowed list may not be showing.

### D84 — Up to five photographs in one pick (D82 extended)

D82 made five photographs possible, but the shelf that adds them appeared only
after the first had been sent, and every way a dream got its first photograph
took one file. Petr: „it only works since you add one image. I want it to work
right away.“

- **Every way in takes several**: the empty tile on the dream's screen — the
  pill reads „Vybrat fotky“ and a hint under it says up to five — Přidat, the
  reel tile's pill and the shelf's ＋. A pick of one still replaces and still
  opens the editor; a pick of several opens nothing, because the first is
  about to be one cell of a collage (D85).
- **A picker cannot be told a number**, so seven can be chosen. The first
  ones that fit are kept in the order picked and a toast says so — „Ke snu se
  vejdou ještě 2 fotky, beru první 2.“ `roomFor` counts rows still being
  resized, as the server does.
- **Sent one behind another, waited for once** (`addPhotographs`): the server
  stands each behind the last, so the first picked is the cover. A refusal
  halfway does not throw — what was sent is on the dream, and the toast says
  why the rest are not.
- **A file that is not a photograph costs only itself** (`downscaleAll`): the
  rest are sent, and a sentence says one could not be read. One file at a
  time, because five camera photographs decoded at once is a lot of a phone's
  memory.
- Přidat counts what it has sent, so a second tap after a failure sends only
  the rest. No API change: an upload already appended.

### D85 — A collage is placed as a collage (D54 and D82 extended)

The first photograph was placed on the reel's page with the title on it; the
second and later ones were placed the same way — alone, filling a whole page,
which is not the shape of their cell. Petr wanted to see the collage he chose
while moving each photograph in it.

- **`CollageEditor` is the same page**: the app column, the height of the
  screen, the scrim, the words where they sit, Zrušit · Na střed · Hotovo —
  with the chosen template drawn on it and every cell under a finger. The
  fingers are `ui/placing.ts`, lifted out of `CropEditor` so both move a
  picture identically, and the page is `.crop` in `app.css`.
- **Touching a cell takes it in hand** and the drag has already begun; a ring
  in `--photo-ink` says which one the arrows, a wheel and Na střed move. A
  second finger on the next cell is still the pinch on the first. Each cell is
  a button, so a keyboard reaches every one.
- **The three templates are across the top**, on glass with the segment's
  lens, because which cells there are is the other half of how the collage
  looks — and here they are chosen with the photographs in them.
- **A cell always fills** (D82), so a photograph shown whole is taken in hand
  as its cell shows it — its point, filling (`cellFocal`) — and saved that way
  only if it is moved. Its mat is kept for the day it is alone again.
- **Nothing is sent until Hotovo**: the template if it changed, then one `PUT`
  per photograph moved. A refusal leaves the editor up with the work in it.
- „Upravit koláž“ on the tile and „Umístit“ on any photograph of the shelf
  open it. With one photograph the shelf still opens `CropEditor`.
- **Found on the way**: the editor's title and pills were under the page's
  scrim and read dimmed — in `CropEditor` too, since D54. They sit above it
  now, as a tile's words do.

### D86 — A dream's tile swipes through its photographs

„Next to it I want to be able to have photo carousel … swipe left and then
right, like on Instagram, to see the images.“

- **On the dream's own screen**, from two photographs up: the collage first,
  with the words and „Upravit koláž“, then each photograph on its own as it
  would be shown alone — filling, or whole on its mat. Dots under the tile say
  where the row is and take a tap.
- **The browser swipes.** A row scrolled across with `scroll-snap-type: x
  mandatory` and `scroll-snap-stop: always` — one slide per swipe, momentum
  and the edge's give native — and `overscroll-behavior-x: contain`, so a
  swipe past the last photograph is not the browser's back. `ui/carousel.ts`
  is the arithmetic for the dots, with its test.
- **Not on the reel.** A swipe across the reel is the other reel (D71), and a
  collage tile that kept that gesture would take it away on exactly the dreams
  with the most to show. Putting the carousel there means changing D71, which
  has not been asked.

### D87 — The reel swipes through a dream's photographs, and a dream is a collage or a carousel (D71 and D86 amended)

Petr asked for the carousel on the reel too, and for a dream to be able to drop
the collage and keep the carousel only. D86 had kept the carousel off the reel
because a sideways swipe there is the other reel (D71).

- **Photographs first, then the reel.** Petr chose this over photographs only
  on those tiles and over dropping D71. A tile with several photographs is a
  row the browser scrolls across; when the finger lands, `sideways.ts` asks
  the row under it how far it can still go (`roomAcross`), and if it could go
  the way the swipe went, the swipe was the photographs'. Past the last
  photograph — or back past the first — the row cannot, and the same swipe is
  the other reel, exactly as on a tile with one photograph. The arrow keys ask
  the row on the screen and step it while it can.
- **The browser moves the row**, as on the dream's tile (D86): `scroll-snap`
  a slide at a time, `touch-action: pan-x pan-y pinch-zoom` on the row, which
  is its own scroll container, so the reel's `pan-y` does not reach it.
  Vertical pans still chain to the reel, and the row adds no height to a page
  (rule 14). Every slide is the dream's link — one announced, the rest hidden
  from a screen reader — and the words, the heart and the dots stay put while
  the photographs pass under them.
- **Per dream, `photoView`** — `collage` or `carousel` — chosen on the shelf as
  Koláž · Karusel. Petr chose this over one app-wide setting. Koláž is the
  collage and then each photograph; Karusel is the photographs alone, with no
  templates offered and no „Upravit koláž“, and Umístit places a photograph as
  the whole page it is there. One photograph is the photograph either way.
  `dreams/slides.ts` says what a tile swipes through, for the reel and the
  dream's tile alike. It moves `updatedAt`, as the template does (rule 24).
- **One migration** (`DreamPhotoView`: `dreams.photo_view`, text, default
  `collage`, so every dream reads as it did) and one endpoint,
  `PUT /dreams/{id}/view`.
- **Offline keeps every slide**: each photograph's thumb, the cells if there
  is a collage, and each photograph at the rung the screen reads — a rung that
  is also a cell's asked for once (D62).
- **One crop per photograph** still (D54): a photograph zoomed in its cell is
  zoomed on its own slide too.

### D88 — A photograph can stand whole in its cell (D82, D85 and rule 16 amended)

Petr: in a collage „the user sees only a part of“ a photograph, and he wanted
it to be changeable. D82 had every cell fill, so a landscape photograph in a
tall cell was a sliver of itself.

- **A cell reads the photograph as a tile does**: `tileStyle` and `matStyle`,
  filling or whole on its mat, with the thumb blurred under it when that is
  the mat. Petr chose this over draggable lines between cells.
- **The collage editor has Vyplnit · Celá and the mats for the cell in hand**,
  the same `FitControls` the single-photograph editor has, and the drag and the
  zoom work in the room a whole photograph floats in (`room`). `cellFocal` is
  gone: the editor holds a cell's crop as saved, and saves it when anything a
  cell shows has changed (`sameCrop`).
- **Surfaces with no room for a mat still fill**: the Seznam's circle, the
  wall's pair, the wallpaper and the shared card read the point alone.
- A photograph set whole while it was alone is now whole in its cell too,
  because that is what was saved for it.
