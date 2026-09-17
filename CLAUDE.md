# CLAUDE.md

Guidance for Claude Code working in this repository.

**Last revised:** 2026-09-17 · §31 closed, D51–D88 · 472 web tests · 439 API tests

---

## What this is

**Aspire** — a personal dreamboard PWA in Czech. Each dream is a full-screen
image swiped through like a reel, so attention goes to his own dreams instead
of other people's content. Pure fuel: not a task app, not a goal tracker.

It is the third app in a trio with Prosper (finance) and Planner, built to
Prosper's conventions so the three feel like one family and share a VPS.
**Where Prosper does X, Aspire does X**, and the exceptions are recorded in
`docs/DECISIONS.md` with reasons.

`PLAN.md` at the root is the plan. M0 (the scaffold) and M1 (boards, dreams,
photographs, the reel, offline) are done; §10 there says what M1 built and
what it left out, and §11 what followed it: the daily pick, and the reel
keeping only what is not yet done. §12 is M3, done — the affirmation, the achieved
photograph beside the dreamt one, the anniversary, and the voice memo
dropped (§8.4). §13 is what followed: the reel shuffled on every open in
place of drag to reorder, and windowed so a hundred dreams are not a
hundred tiles. Categories followed (§8.3 answered: a fixed set — D32),
§14 is M4, the wallpaper, and §15 is M5, the morning nudge.
**M0 through M5 are done**, and §16 closes the last thing M1 left behind:
the desktop grid is dropped, not built — a grid of thumbnails is a gallery
and this app is a queue, so there is no breakpoint at which a screen
changes its shape (D38). §17 answers D37's question: the board fetches a
window of the reel and the whole of itself only where the browser says the
connection is free, because a hundred dreams is 20 MB (D39). §18 makes the
reel a reel: every page is the screen, a swipe is worth one dream however
long it is, and the chrome floats on the photograph (D40). §19 is the
newest: the areas are three — Chtít · Být · Dělat — rather than Yager's
nine (D43), and Seznam is a fourth tab where the board is one column and
＋ opens a sheet of the five fields a sentence can answer (D44). §20 is the
deployment as a script the button only presses (D45), the deploy key pinned
to one command on a user of its own (D47), and `board invite`, which is a
second person's whole onboarding (D46). §21 gives the list a shape: every
line numbered with its place in the whole list (D48), a field that narrows it
on everything a dream says about itself (D49), and a note that says when a
dream is already written down without ever stopping it being written again
(D50). §22 is M6, done: the late nudge was the urgency and
now says so in the log (D51), a tile without a photograph is where that
photograph is asked for (D52), the anniversary is the morning's notification on
the one morning a year a dream has one (D57), the heart has a job — it shortens
the wait for the dream it is tapped on, and its count comes off the tile
(D58) — and the lock screen refreshes itself, from six the board chooses (D59)
through a link that is its own key (D60). §23 is
M7, done — two reels, a photograph from a link, and the photograph's own edges.
§25 closes §3.7, the last line of the plan
that was never a milestone: a dream shared as a page this server writes, behind
the same key (D61). §24 is M8, done: the photographs at scale — the reel
drawn at the rung the phone is (D62), the encoder tuned once (D23 amended),
the thumb under the picture (D63), and a board that weighs what its
photographs weigh, with two gigabytes as full and the rule for when the disk
stops being enough decided rather than built (D64). §26 is what a walk
through the finished app found, and is not a milestone: Smazat waits six
seconds and „Vrátit“ cancels it rather than a dialog asking (D65), a
photograph still being resized says so instead of painting the same sky as a
dream with none (D52 amended), the reel's rung reads the connection and not
only the pixel ratio (D66), a control that writes wears `use:writes` instead
of remembering the lock (D67), and components and endpoints have tests of
their own (D68, D69). §27 is five things found using the app on a phone:
the notification's badge is the mark as a silhouette rather than the icon
Android renders as a white square (D70), the chosen reel is readable again,
a shared link is a card and not a stretched background (D73), the nudge is
up to five reminders a day (D72), and a sideways swipe on the board changes
which reel it is (D71). §28 is two more: the notification wears the mark as
a white outline rather than the colour icon (D70 amended), and the reel
segment wears the tab bar's glass — rim, sheen and a sliding lens — off the
back of making glass one decision in `tokens.css` instead of eight literals
(D74). §29 is nine things asked for after living with it: a line of the
Seznam slides aside to its hearts, Sdílet and Smazat (D78), is dragged or has
its number typed over because the Seznam's order is his own now — and the
reel's is still a shuffle (D79) — the board is counted above it and a dream
says when it was last changed (D77), a sheet is pulled down as Prosper's is
(D76), the frame is pinned to the viewport rather than `100dvh` tall (D75),
Začít znovu empties the board behind a typed sentence (D80), a photograph can
be shown whole on a mat instead of filling its frame (D81), and a dream has
up to five of them, cut into a collage (D82) — which is §3.1's „1–5 images“,
finally. §30 is four more the same afternoon: the Seznam narrows to Teď with
the board's own Vše · Teď (D83), a dream takes up to five photographs in its
very first pick (D84), a collage is placed as a collage, every cell under a
finger on the reel's page (D85), and the dream's tile swipes through its
photographs (D86). §31 puts that carousel on the reel — photographs first, and
past the last one the swipe is the other reel — lets a dream be a collage or
a carousel (D87), and lets a photograph stand whole in its cell (D88). Nothing
is left in the plan.

---

## Commands

Run from the repository root.

```bash
pnpm install
```

```bash
pnpm start
```

```bash
pnpm api
```

```bash
pnpm dev
```

```bash
pnpm test
```

```bash
pnpm check
```

```bash
pnpm lint
```

```bash
pnpm build
```

```bash
pnpm budget
```

```bash
pnpm api:test
```

`pnpm start` runs both halves at once and is the one to use. `pnpm api` runs
the API alone in Development: SQLite in `apps/api/src/Aspire.Api`, port 5300,
pairing code **`000000`** — the laptop's own board, which has nothing to do
with any code generated on the VPS. `pnpm dev` runs the client alone and
proxies `/api` to port 5300, so on its own every request is
`ECONNREFUSED`. `pnpm
budget` runs after a build and fails over 150 kB brotli on the entry route.

To add a migration:

```bash
cd apps/api && dotnet ef migrations add Name --project src/Aspire.Infrastructure
```

---

## Layout

```
apps/web/src/
├─ lib/styles/     tokens.css — the only place a colour exists — and app.css,
│                  which owns the primitives: page, card, row, circle, badge,
│                  seg, chip, toggle, facts, stats, well, field, btn, dream,
│                  swipe, crop — and the five mats a photograph shown whole
│                  stands on, `--mat-*`, which are the print's and not a
│                  third accent (D81).
├─ lib/ui/         Hand-rolled components. No component library;
│                  pager.ts — one dream per swipe, however long the swipe —
│                  sideways.ts, across the reel to change which reel it is
│                  (D71) — ReelTile.svelte, one page of the reel and every
│                  style that makes it exactly one scrollport tall —
│                  Sheet.svelte, the `<dialog>` a form rises in (D44), and
│                  pull.ts, how far down it is pulled before it goes (D76) —
│                  DreamRow.svelte, a line of the Seznam, with swipe.ts for
│                  the tray behind it (D78) and reorder.ts for dragging it
│                  (D79) — Collage.svelte, PhotoShelf.svelte and
│                  PhotoLinkSheet.svelte, a dream's five photographs (D82) —
│                  CollageEditor.svelte, where they are placed as one
│                  picture, with placing.ts, the fingers it shares with
│                  CropEditor (D85), and FitControls.svelte, Vyplnit · Celá
│                  and the mats for both editors (D88) —
│                  PhotoCarousel.svelte and carousel.ts, the tile swiped
│                  through, and how far a row can still go before a swipe
│                  is the reel's (D86, D87) — and
│                  ShareSheet.svelte, which two screens open (D78).
├─ lib/api/        client.ts (fetch + bearer), token.ts (localStorage),
│                  pairing.ts (the flow) and errors.ts (the sentences).
├─ lib/dreams/     rules.ts — what a dream may be, and how it reads in a
│                  list — board.ts — what the reel shows, which dream it
│                  opens on, what order the rest and the Seznam are in,
│                  what a tile says and whose anniversary today is —
│                  focus.ts — Teď: which ten are on the second reel, in
│                  what order, and which reel this device opens on (D53) —
│                  photos.ts — which of a dream's two photographs a screen
│                  shows, and which rung the reel reads on this phone (D62)
│                  — search.ts — what typing in the Seznam looks in
│                  and how Czech is folded before it does (D49) —
│                  duplicates.ts — whether a dream being written is one
│                  already written down (D50) — deleting.svelte.ts, the few
│                  seconds a deleted dream is not gone yet (D65), and
│                  letgo.ts, the one Smazat every screen calls (D78) —
│                  order.ts, a dream moved to a line of the Seznam (D79) —
│                  stats.ts, the board counted (D77) — collage.ts, the
│                  templates for two to five photographs (D82) — slides.ts,
│                  what a tile swipes through: the collage and each
│                  photograph, or the photographs alone (D87) — reset.ts,
│                  the sentence that empties a board (D80) — upload.ts — the order a
│                  photograph replaces another in, which two screens do
│                  (D52), and the ones that fit when several are picked at
│                  once (D84) — wallpaper.ts — who can be on a collage and how
│                  big it is — and format.ts.
├─ lib/images/     downscale.ts — the photograph to 2048 px on the device —
│                  and focal.ts, where a photograph is looked at and how far
│                  in, as a drag and a pinch become two numbers (D54).
├─ lib/offline/    status.svelte.ts — the connection flag every screen reads,
│                  and writes.svelte.ts, the `use:writes` every control that
│                  writes wears instead of remembering it (D67) —
│                  cache.ts, which fills and prunes the photo cache a window
│                  at a time — policy.ts, how much of the board this device
│                  keeps and what the connection costs (D39) — and shell.ts,
│                  which shell caches a new build may throw away (D42).
├─ lib/push/       nudge.ts — the browser's half of the morning notification
│                  — and schedule.ts, the time as a field shows it.
├─ routes/         / · /seznam · /ted · /pridat · /sen/[id]
│                  · /sen/[id]/upravit · /sin-slavy
│                  · /nastaveni · /nastaveni/vzhled · /nastaveni/upozorneni
│                  · /nastaveni/tapeta · /nastaveni/stahovani
│                  · /nastaveni/parovani · /nastaveni/zacit-znovu
│                  · /styleguide (unlinked, for review)
└─ service-worker.ts  three caches: the shell per build, the photographs
                      cache-first, the board network-first (D24).

apps/api/src/
├─ Aspire.Api/             Program.cs (minimal APIs), Auth/ (pairing, devices,
│                          and ShareKey — the 32 bytes both links are),
│                          Boards/ (the board commands, the lock-screen
│                          link — D60 — and starting over, with the phrase
│                          that guards it — D80), Dreams/ (and the one page this server
│                          writes, for a dream somebody else opens — D61),
│                          Images/ (the queue and the worker), Wallpaper/
│                          (the lock-screen collage), Nudges/ (the morning
│                          notification and its worker), Contracts.cs
├─ Aspire.Domain/          Board, Dream, DreamImage, Device, and the rules a
│                          sleeping phone cannot work out for itself —
│                          DailyPick, Anniversary (D57) and WallpaperPick
│                          (D59). No EF.
└─ Aspire.Infrastructure/  AppDbContext, Media/ (the store, the resize, the
                          collage, the focal crop), Net/ (which addresses may
                          be reached, and the picture behind a link — D56),
                          Push/ (RFC 8291 + 8292, by hand), Migrations/
apps/api/tests/Aspire.Api.Tests/   xUnit, SQLite in memory

packages/contracts/  the wire types; mirrored in Contracts.cs
deploy/              compose, nginx, the host vhost, backup.sh, and deploy.sh —
                     the deployment itself, which the Deploy workflow only
                     runs over SSH (D45) — beside aspire-deploy, the forced
                     command the deploy key is pinned to (D47)
scripts/             check-bundle.mjs, and invite.sh — a board and its code
                     for somebody else, printed in one terminal (D46)
.github/workflows/   ci.yml on every push; deploy.yml on a button
```

---

## Rules that are not negotiable

1. **Colours only from `tokens.css`.** A literal hex in a component is a bug.
   Two accents exist, `--signal` (ember) and `--dusk`; there is no third.
   The weight ladder is **400 / 500 / 600** and **nothing is uppercase**.
2. **No component library, no CSS framework.** The primitives live once in
   `app.css` — the sheet included; a screen declares only its difference.
3. **Ask before adding a dependency.** Every package is a bundle-size
   decision against 150 kB brotli. The client ships no runtime dependency.
4. **Photos are the hero.** Type on a photograph is `--photo-ink` over
   `--scrim`; the interface steps back. Colour at scale is the sky, nowhere
   else.
5. **A schema change is a new migration**, never an edit to an existing one.
   CI fails on a model that disagrees with its last migration.
6. **The `Contracts.cs` mirror moves with `packages/contracts`** in the same
   change, same names, camelCase on the wire, kebab-case enums.
7. **Auth is Prosper's.** Pairing code, device-bound token stored as a hash,
   no expiry, no accounts. Do not design a login. A tenant is a board with
   its own code (D21), never an account.
8. **The UI is Czech. Code, identifiers, comments, commits and docs are
   English.** No exceptions in either direction.
9. **Update `docs/DECISIONS.md`** whenever a question gets answered or an
   assumption turns out wrong, and `PLAN.md` §8 when an open question closes.
10. **Both themes, always.** Dark is `--dark-*` on `:root`, pointed at twice
    (system preference and explicit choice); change a dark value in the
    bank and nowhere else.
11. **An area is one of three**, `want` · `be` · `do` — Chtít · Být · Dělat
    (D43) — fixed like the status, optional on a dream, and the same word in
    `packages/contracts`, in `DreamCategory` and in the column. Yager's nine
    were the set until 2026-09-11; a migration cleared them rather than
    guessing which of three each one had meant.
12. **A photograph has two kinds**, `dreamt` and `achieved`, and no screen
    picks one by hand. `dreams/photos.ts` says which one a screen shows and
    which ones a replacement takes with it, so changing the dreamt
    photograph never takes the proof with it (D28). **Up to five are dreamt**
    (D82): the first is the cover every small surface shows — `photoOf` —
    and `dreamtPhotos` is all of them, which two or more of makes a collage
    on the reel and on the dream's own tile (`dreams/collage.ts`). The
    achieved photograph is never a sixth cell.
13. **A duplicate is told, never stopped.** `dreams/duplicates.ts` says when
    a title is already on the board and the form shows a `.note` under the
    field; the pill stays live and keeps its word. It is his list (D50), and
    every screen that writes a title checks the same way.
14. **The reel is a pager.** Every page is exactly the scrollport, the
    offsets are multiples of it, and `ui/pager.ts` guarantees one dream per
    gesture on top of the browser's own snapping (D40). A dream's photographs
    scroll across inside its page, never down (D87). Anything that adds
    height to that scroll region — a mark, a header, a gap — breaks the
    arithmetic, and the guarantee with it.
15. **The link fetcher may only reach the public internet.** Every address is
    checked in the client's own `ConnectCallback`, every redirect hop with it,
    and every failure is one sentence that says nothing about what was found
    (D56). Anything added to `Net/` inherits that bargain.
16. **A photograph is cropped by a point and a zoom, never by a file.**
    `focus_x` · `focus_y` · `zoom` on the row, `photoStyle` on the client and
    `FocalCrop` on the server, and every surface that shows a photograph
    reads them (D54). The three files on disk are never re-cut. **And it
    either fills its frame or stands whole on a mat** — `fit` · `mat` (D81).
    A surface with room for a mat reads `tileStyle` and `matStyle` — a
    collage's cell is one since D88; one without — a circle, the wall's pair,
    the wallpaper, the shared card — reads `photoStyle` and `CropZoom`, which
    are the point alone for a photograph shown whole, because its zoom was
    chosen against a different scale. A mat is a token's name on the wire,
    never a colour (rule 1).
17. **A key that is its own permission is made in one place and fenced the
    same way.** `Auth/ShareKey` is those 32 bytes; `/api/v1/w/{key}` is one
    board's collage (D60) and `/s/{key}` is one dream's page (D61), and they
    are the only routes besides `pair` that answer with no token. Each opens
    exactly one thing, is `no-store`, answers a key that opens nothing with a
    bare 404, keeps `access_log off` in nginx because the key is in the path,
    and is revoked by making a new one. Anything else that ever answers
    without a token inherits that bargain whole.
18. **The heart's one job is the wait.** `fuel = days since shown ×
(1 + min(likes, 10) / 10)` picks the day's dream and orders the automatic
    wallpaper, in `dreams/board.ts` and in `DailyPick.cs`, counting whole days
    so two phones cannot disagree (D58). Nothing else reads the count, the
    reel's tile shows no number — the Seznam shows it behind a swipe, where a
    count is a fact about a line and not a score on a photograph (D78) — and
    the cap is not negotiable: without it the
    loved dreams take every morning and the board stops turning over.
19. **A morning is one notification, and the anniversary outranks it.**
    `Aspire.Domain/Anniversary.cs` is asked before `DailyPick`, and when it
    answers, that is the nudge instead — „Před rokem“ over „Splnil se ti sen
    „X“.“, the achieved photograph, and no `last_shown_at` stamp, because the
    day's pick was never put in front of anybody (D57). It is the same rule as
    `anniversaryToday` in `board.ts` and the two tests mirror each other case
    for case.
20. **A control that writes wears `use:writes`**, never a `disabled` of its
    own that has to remember the connection (D67). Where it has a reason of
    its own it says so — `use:writes={() => busy}` — and the connection half
    is never the caller's. A link cannot, and keeps `aria-disabled`.
21. **A deleted dream is held, not sent** (D65). `dreams/deleting.svelte.ts`
    owns the window; every list that shows dreams filters `deleting.has`, and
    an id stays hidden after the request has gone. A new list of dreams
    inherits that filter or it will show a dream that was deleted a tab away.
22. **There are two reels, and only Vše is shuffled.** Teď is at most ten
    dreams in the person's own order, `focusRank` on the dream and
    `dreams/focus.ts` on the client (D53). The day's pick and its `shown`
    stamp belong to Vše alone: a board opened on Teď has put no dream in
    front of anybody, and „shown“ is what the word means (D25, D36). **The
    Seznam's order is a third thing and the reel never reads it** (D79):
    `sortOrder`, lowest first, moved one dream at a time by
    `PUT /dreams/{id}/place` and by `dreams/order.ts`, which number the board
    the same way — whole numbers from nought, every time.
23. **Every line of the Seznam is exactly as tall as the next.** Dragging one
    is arithmetic on that one number (`ui/reorder.ts`), so the hairline is
    drawn on the row's face rather than being a border between two, and
    nothing in a row wraps. Anything that makes one row taller than another
    puts every drop below it on the wrong line.
24. **`updatedAt` moves when the dream is changed and never when the board
    is** (D77): its words, its state, a photograph, its template — not a
    heart, a „shown“ stamp, a place in the Seznam or a place on Teď. A new
    write decides which of the two it is before it touches the stamp.
25. **Emptying a board takes the sentence as well as the URL** (D80).
    `ResetPhrase` on the server and `dreams/reset.ts` on the client say the
    same thirteen characters, and `POST /board/reset` is the only write that
    cannot be undone or held. Nothing else gets to delete more than one
    dream at a time.

---

## Traps

**SQLite cannot order by `DateTimeOffset`.** `ThenBy(d => d.CreatedAt)`
threw a 500 on the laptop and would have passed on Postgres. Order by the
id instead, and run the laptop mode before calling an endpoint done.

**The laptop's `aspire.db` never migrates.** SQLite mode creates the schema
as the model stands and then leaves it alone, so after a model change the
old file answers "no such column". Delete it and start the API again.

**A Bash heredoc over about 8 kB is cut short on this machine** and fails
with an unmatched quote. Write large files with the Write tool. A heredoc
also cannot carry a C# raw string: the `"""` inside a Python `'''…'''` or
`"""…"""` patch script ends the script's own string, so a test with a raw
JSON body goes in with the Edit tool.

**`dotnet test` and `dotnet build` fail with MSB3027 while the API is
running**, because the running process holds `Aspire.Domain.dll` and
`Aspire.Infrastructure.dll` open — ten retries, then an error that reads like
a broken build. Stop the `aspire-api` preview (or `pnpm api`) first.

**The laptop's `aspire.db` is behind by three columns after 2026-09-17**:
`dream_images.fit` and `.mat` (text, `'fill'` and `'night'`) and
`dreams.layout` (integer, 0). An `ALTER TABLE … ADD COLUMN … NOT NULL DEFAULT`
from python's `sqlite3` brings an old file up without losing its dreams;
deleting it, as above, still works.

**Czech in a `curl -d` argument does not survive this machine's shell.**
`-d '{"why":"Chci vidět…"}'` reaches the API as broken UTF-8 and comes back
as `The JSON value could not be converted … Path: $.why`, which reads like a
contract mismatch and is an encoding one. Write the body with the Write tool
and send it as `--data-binary @file`. Every payload this app takes has
diacritics in it, so this is most of them.

**The Browser pane produces no frames while it is hidden**, and a scroll
event is dispatched by the frame loop — so a `scrollTop` set from the console
fires no `scroll`, no `IntersectionObserver` and no `requestAnimationFrame`,
and anything checked on one of those looks broken when it is fine.
`visibilityState` says `visible` throughout. Layout and screenshots do work.
`apps/web/CLAUDE.md` has the long version.

**A control character can hide inside a string literal.** `WebPushCrypto`'s
HKDF labels ended up holding a raw `0x01` byte rather than the two
characters ``, so the info strings were one byte long and every derived
key was wrong. `grep` showed the line as correct, because a 0x01 prints as
nothing; `sed -n '34p' file | cat -A` shows it as `^A`. When a string
constant is definitely right and the output is definitely wrong, look at the
bytes before looking anywhere else.

**`compose run` appends to the API image's entrypoint, which already names
the dll.** `ENTRYPOINT ["dotnet", "Aspire.Api.dll"]`, so the command is the
bare word — `docker compose run --rm -T api vapid`. Spell it
`run --rm api dotnet Aspire.Api.dll vapid` and the app gets three arguments it
does not recognise, falls through every command branch and **starts a web
server**, which looks exactly like a hang. `exec` is the other way round: it
runs a bare command in a container that is already up, so `board` there is
`docker compose exec api dotnet Aspire.Api.dll board …`. The runbook had the
`run` form wrong from M5 until 2026-09-11, and the symptom on the box was one
stderr line about `libgssapi_krb5.so.2` — which is unrelated Npgsql noise the
API prints on every start.

**EF stores a `Guid` as UPPERCASE text in SQLite**, and SQLite compares text
case-sensitively. Staging a row in `aspire.db` by hand with a lowercase id —
python's `uuid.uuid4()` gives one — makes a row that is there, that `sqlite3`
finds, and that EF never joins to: the dream came back from `/api/v1/dreams`
with `images: []` and nothing failed anywhere. `select id from dreams limit 1`
shows the case the file actually uses. While staging, also note that
`category` is nullable but `''` is not a category: `DreamCategoryNames.Parse`
throws `ArgumentOutOfRangeException` and the whole endpoint 500s.

**The web batch in `docker build` runs from the repository root**, because
the client imports `@aspire/contracts`. `docker build -f apps/web/Dockerfile .`,
never from `apps/web`.

**`vite preview` caches the file list when it starts.** After a `pnpm build`
the new hashed assets 404 on a preview that was already running, the app
never boots, and a tab with the service worker keeps showing the old build
as if nothing had happened. Restart the preview after every build.

---

**An unanchored `.gitignore` rule swallowed a source directory.**
`apps/api/.gitignore` said `media/`, meaning the laptop's media root. This
checkout is case-insensitive, so it also matched
`src/Aspire.Infrastructure/Media/`: `MediaStore.cs` and `ImageProcessor.cs`
were never in git, every local build passed, and the VPS was the first thing
to notice — by failing to compile a tree that had no `Media` namespace in it.
Anchor a rule to the one path it means, and when a change adds a directory of
source, `git status --ignored` before believing the tree is complete.

**A named volume inherits ownership from the first container to mount it**,
and only when that container's image has a directory at the mount path. Both
`api` and `web` mount the media volume; the API's image chowns `/data/media`
to the app user and nginx's has no `/usr/share/nginx/media` at all, so which
one lands first decided whether the non-root API could write a photograph.
On the VPS it lost, and every upload saved a row that the worker then deleted
because it could not make the directory — the dream showed the sky and nothing
said why (D26). `media-init` in the compose file sets the owner rather than
hoping for it, and `MediaStore.EnsureWritable` makes the API prove it can
write before it serves. A check that a directory _exists_ is not a check that
you can write into it.

## Where the answers are

| Document              | What it is                                           |
| --------------------- | ---------------------------------------------------- |
| `PLAN.md`             | The plan and its open questions                      |
| `docs/DECISIONS.md`   | Every answered question and every deviation. Binding |
| `docs/DEPLOYMENT.md`  | The VPS runbook                                      |
| `apps/web/DESIGN.md`  | The design system as built                           |
| `apps/web/PRODUCT.md` | Product truth for design work (Impeccable)           |
| `apps/api/README.md`  | The API: endpoints, running it, migrations           |
