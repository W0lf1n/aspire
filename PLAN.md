# Aspire — Project Plan

Status: M0–M5 done · Owner: Petr · Created: 2026-09-09 · Revised: 2026-09-11

Third app in the personal self-improvement trio (Prosper → Planner → **Aspire**, the dreamboard). Building first. Standalone. No integration with the others for now.

---

## 1. Vision

Open phone anywhere — train, queue, bad meeting — swipe through your dreams in 10 seconds. Remember *why*.

Not a task app. Not a goal tracker. Pure fuel. Images first, words second.

Yager: "Dream building" is step one of everything. Dream must be visual, specific, and seen daily. This app is the digital dream wall that is always in your pocket.

## 2. Principles

1. **Instant.** Cold open → first dream image in < 1 s. Cached offline. No login screen on repeat visits.
2. **Visual.** Full-screen image per dream. Text overlay minimal: title + one-line "why".
3. **Emotional, not analytical.** No progress bars on the board itself. Achieved dreams celebrated, not measured.
4. **Frictionless input.** Add a dream from phone camera / photo library / URL in 3 taps.
5. **Private.** Your dreams, your VPS. Optional share link for a single dream (later).

## 3. Features

### 3.1 Dreams (MVP)
- Dream = title · 1–5 images · "why" (1–3 sentences) · category · target year (optional) · status (dreaming / in progress / achieved) · created_at.
- Categories (**fixed**, §8.3): Want · Be · Do — Chtít · Být · Dělat. Optional on a dream. Yager's nine until 2026-09-11, when the set became the three questions a dream can answer as it is being written (D43, in place of D32).
- Image upload: phone camera, library, paste URL. Server resizes to 3 sizes (thumb / screen / full), WebP.
- ~~Reorder dreams by drag (board order = priority).~~ Dropped 2026-09-10: the
  reel is shuffled on every open instead, so no order is a route you learn (D30).

### 3.2 Board view (MVP)
- **Mobile:** full-screen vertical swipe, one dream per screen (Instagram-stories feel). Tap → detail with "why" and gallery.
- ~~**Desktop/tablet:** masonry grid, click → detail.~~ Dropped 2026-09-11:
  a grid is a gallery, a reel is a queue, and desktop stays the phone layout
  with room around it (D38).
- "Daily dream": on open, first card is a random dream (weighted toward least-recently-seen). Prevents habituation.
- Filter by category / status. Default: dreaming + in progress.

### 3.3 Achieved wall (v1.1)
- Mark dream achieved → date + optional "achieved photo" (real photo vs dream photo side by side).
- Separate "Hall of Fame" screen. Motivational proof that system works.
- Anniversary reminder: "1 year ago you achieved X".

### 3.4 Affirmations & audio (v1.1)
- Per dream: optional affirmation line shown on card ("I drive it in 2028").
- ~~Optional short voice memo per dream (record on phone, play on detail).~~
  Dropped 2026-09-10 (§8.4, D31).

### 3.5 Wallpaper / collage export (v1.2)
- Generate lock-screen collage from N selected dreams (server-side rendering, phone aspect ratio presets).
- One tap → save image → set as wallpaper. Dream seen 100× per day without opening app.

### 3.6 Daily nudge (v1.2)
- Push notification at user-set time (default 07:00): one dream image + title. Tap opens board.
- Configurable: off / daily / weekdays.

### 3.7 Later / maybe
- Share single dream via signed link (for Zuzana, upline).
- Link dream → Planner goal (explicitly out of scope now).
- "Dream cost" field + link to Prosper savings goal (out of scope now).

## 4. Architecture

Same stack as Prosper and Planner. Lighter backend — mostly media handling.

```
aspire/
├── backend/            ASP.NET Core 9 Web API
│   ├── Aspire.Api/
│   ├── Aspire.Domain/       dream entity, daily-pick logic
│   └── Aspire.Infrastructure/  EF Core, image processing, storage, push
├── frontend/           SvelteKit + TypeScript, PWA
├── docker-compose.yml  api + postgres (+ reverse proxy as Prosper)
└── PLAN.md
```

Backend
- .NET 9, EF Core 9 + Npgsql.
- Image processing: `SixLabors.ImageSharp` → resize + WebP, strip EXIF. Run in background queue (`Channel<T>` + hosted service), return placeholder until ready.
- Storage: local disk volume `/data/media/{userId}/{dreamId}/{size}.webp` served via reverse proxy with cache headers. Option: MinIO on VPS or S3-compatible later if disk fills. NAS backup via existing homelab flow.
- Push: `WebPush` NuGet, VAPID, hosted service for daily nudge.
- Collage: ImageSharp compositing, presets 1170×2532 (iPhone), 1080×2400 (Android).
- Auth: same as Prosper.

Frontend
- SvelteKit PWA. ~~Service worker caches **all** screen-size images of active dreams (typ. < 20 dreams × ~200 KB = fine). Board fully usable offline.~~ The 200 KB was right — 197 measured — but "all" was a twenty-dream assumption: at a hundred it is 20 MB, so the board caches a window of the reel and the whole of itself only on a connection the browser says is free (§17, D39).
- Swipe: CSS scroll-snap, no heavy lib. `Motion One` or plain CSS for transitions.
- Upload: `<input capture="environment">` for camera, drag-drop on desktop, client-side downscale before upload (max 2048 px) to save mobile data.
- ~~Voice memo: `MediaRecorder` API → webm/opus upload.~~ Dropped (§8.4).
- Screens: Board · Dream detail · Add/Edit · Hall of Fame · Settings.

Infra
- Contabo VPS, Docker Compose, Postgres + media volume, nightly backup of both.
- Subdomain e.g. `aspire.petrbohac.eu`.

## 5. Data model

No `users`: a board is the tenant and a device belongs to one (D6, D21).
Columns are snake_case as built (D9).

```
boards             id, name, code_hash, created_at
devices            id, board_id, name, token_hash, paired_at, last_seen_at?
                   (no `categories` table: the three are fixed, so the category
                   is an enum column on the dream — §8.3, D32, D43)
dreams             id, board_id, category?, title, why, affirmation?, target_year?,
                   status, sort_order, likes, achieved_at?, last_shown_at?, created_at, updated_at
dream_images       id, dream_id, sort_order, width, height, is_achieved_photo, processed_at?
                   (no `dream_audio`: the voice memo is dropped — §8.4, D31)
push_subscriptions id, board_id, endpoint, p256dh, auth
```

Daily pick rule: `ORDER BY last_shown_at NULLS FIRST, random() LIMIT 1` among status ∈ (dreaming, in progress); update `last_shown_at` on view.

## 6. Milestones

| # | Milestone | Scope | Est. |
|---|---|---|---|
| M0 | Scaffold | Copy Prosper/Planner skeleton, media volume, deploy | 1 weekend |
| M1 | Dreams + board | Done 2026-09-10 (§10). Categories followed (§13); the desktop grid dropped 2026-09-11 (D38) | 2 weekends |
| M2 | PWA offline | Folded into M1 on 2026-09-10 (D24): the photographs and the board cached, read-only offline | done |
| **→ MVP live. Load real dreams. Use 2 weeks.** | | | |
| M3 | Hall of Fame + affirmations | Done 2026-09-10 (§12): affirmation, before/after, anniversary. Voice memo dropped (§8.4) | done |
| M4 | Wallpaper export | Done 2026-09-10 (§14): collage renderer, the phone's own canvas, share or save | done |
| M5 | Daily nudge | Done 2026-09-11 (§15): subscription, schedule, the crypto by hand, the morning notification | done |

Smallest of the three apps. Good candidate to build **first** if you want a quick win, or **second** right after Planner MVP — both are one-month projects at weekend pace.

## 7. Risks

- Media on VPS disk: check free space on Contabo plan; set upload cap (e.g. 10 MB/image, 500 MB/user).
- iOS PWA cache eviction after ~7 days unused → app must re-fetch gracefully; keep thumbnails small.
- iOS `MediaRecorder` webm support is partial → accept mp4/aac fallback or skip voice memo on iOS initially.
- Push on iOS requires home-screen install (same as Planner).

## 8. Open questions

Answered ones are struck through; the reasoning is in `docs/DECISIONS.md`.

1. ~~Same auth as Prosper — which?~~ — answered 2026-09-09: Prosper's pairing code + device-bound token, copied whole. No users table; §5 loses `users` and every `user_id` (D6).
2. ~~Images stay on VPS disk, or MinIO/S3 from the start?~~ — answered 2026-09-09: disk, in a named `media` volume served by nginx as `/media/`; path `/data/media/{dreamId}/{size}.webp` (D7).
3. ~~Categories: fixed Yager set above, or fully free-form?~~ — answered
   2026-09-10: the fixed set, as an enum beside the status, and optional on
   a dream. The set is the method; a board that could rename it would need a
   screen to rename it in (D32). Which set, re-answered 2026-09-11: three —
   Chtít · Být · Dělat — because that is a question a dream can answer while
   it is being written, and nine is a taxonomy you stop to place it in (D43).
4. ~~Voice memo: worth it in v1.1, or drop?~~ — answered 2026-09-10: dropped.
   The app's promise is ten seconds of swiping in a queue, and audio is the
   one thing there you cannot use (D31).
5. ~~Czech / English UI?~~ — answered 2026-09-09: Czech UI, English code, like Prosper (D2).
6. ~~Will Zuzana use it too (shared board vs. separate accounts)?~~ — answered 2026-09-10: boards are tenants, each with its own pairing code and still no accounts; she gets her own board, or a device on Petr's, by which code she types (D21).
7. ~~Build order~~ — decided: Aspire first.

## 9. M0 — what was built · 2026-09-09

Repository in Prosper's shape (`apps/api`, `apps/web`, `packages/contracts`,
`deploy/`), not the `backend/` + `frontend/` tree in §4 (D1). .NET 10 rather
than 9 (D3). The three backend projects from §4, real EF migrations with a
SQLite laptop mode (D5). Pairing auth, `/api/v1/health`, `/api/v1/dreams`
answering an empty list. The client shell: the design system with light and
dark, the empty board, the four-slot bar, Settings with the theme choice, the
styleguide at `/styleguide`. Compose, nginx, the host vhost for
`aspire.petrbohac.eu`, the nightly backup, CI.

## 10. M1 — what was built · 2026-09-10

Boards as tenants (D21): a pairing code opens a board, devices and dreams
belong to one, and `board add | code | list` in the API's image makes more.
Dreams made, changed, felt and let go — POST, PUT, DELETE and the heart, a
counter within the board (D22) — through one form behind Přidat and
Upravit, a dream's own screen, and the board as a reel: one tile the height
of the screen per dream, snapping, the heart on each. Photographs downscaled
on the device to 2048 px, resized by a background worker into thumb, screen
and full WebP under `/media/{dreamId}/{imageId}/` with the EXIF gone (D23).
M2 folded in (D24): the photographs and the board cached on the device, the
board readable without a signal, every write locked while there is none,
and a dead token named rather than shown as an empty board. Not built:
categories (§8.3), the desktop grid, the daily pick, drag to reorder — the
daily pick and the achieved filter followed the same day (§11).

## 11. After M1 — the daily pick and the wall · 2026-09-10

§3.2's two missing halves, taken out of order because both are small and
both change what daily use feels like (D25). The board's reel is now what is
still ahead: a dream marked splněno leaves it. It arrives instead in the Síň
slávy, which was a stub and is now the wall — the achieved dreams, most
recent first, each with its photograph and the date. And the reel opens on
the day's dream: the one shown least recently, chosen on the device from the
board it already has, stamped through `POST /dreams/{id}/shown`, and held
for the rest of the day so the tile does not move under a thumb.

M3 keeps the rest of §3.3 and §3.4: the achieved photograph beside the
dreamt one, the affirmation line, the voice memo (§8.4), the anniversary.
Still not built from M1: categories (§8.3), the desktop grid, drag to
reorder.

---

## 12. M3 — in progress · 2026-09-10

**The affirmation** (§3.4, done): a dream can be said as though it were
already true — one optional line, 120 characters, in the person's own words.
On a tile it takes the why's place rather than standing under it, because a
third block of type on a photograph is one thing too many and the line meant
to be said beats the line that explains (D27); on the dream's own screen
both are there, the why on the photograph and the affirmation opening the
card. The choice is `tileLine` in `dreams/board.ts`, with its test.

**The achieved photograph** (§3.3, done): a photograph knows which of the
two it is — `dreamt` or `achieved`, a column and a word on the wire (D28).
A dream marked splněno gets a second picker on its own screen, under the
dreamt one and only while it is achieved; the Síň slávy stands the pair
side by side, 4:5 each with a 2 px seam and one shadow around both, because
the argument is that the right-hand picture looks like the left-hand one.
Replacing one kind leaves the other where it is (`photos.ts`), and the
media cache keeps both so the wall is whole without a signal.

**The anniversary** (§3.3, done): on the one morning a year a dream has one,
the wall reaches the board — a single line above the reel, dusk-washed with
a small trophy, the way back to the dream itself (D29). Push is M5's, so
this is where an anniversary can be seen for now. The sentence's verb agrees
with *sen* rather than with the person, because a board is a pairing code
and the server has never been told anybody's gender (D21).

**The voice memo is dropped** (§8.4, D31), which finishes M3: audio is the
one thing that does not work in a queue or on a train, and the affirmation
carries the same idea in a form the board can show every morning.

---

## 13. After M3 — the reel at a hundred dreams · 2026-09-10

Drag to reorder (§3.1) is dropped and the reel is **shuffled on every open**
instead (D30). At a hundred dreams a fixed order is a route you learn by
heart, and a dream always reached on the ninetieth swipe is one you never
see — the same habituation the daily pick exists to break, one tile further
down. The head stays the day's pick, so the board still opens on the dream
two devices agree about; the tail is new every time. The order is a
sequence of ids worked out once per open, so a heart tapped does not move
the tiles under a thumb, and `sortOrder` stays as the board's own stable
order underneath.

And the reel is **windowed**: five tiles in the document, five more each
time the end of them is neared, watched by an `IntersectionObserver` on a
one-pixel mark. Nothing is removed from the top — taking a tile out of a
snapping scroll region moves the one under the thumb.

**Categories** (§3.1 and §3.2, §8.3 answered): a fixed set, as an enum
beside the status — optional on a dream, because a question a dream must
answer before it can be written is a dream that does not get written. The
form offers them as chips that wrap; the board offers the ones it has
something in as a rail above the reel, and asking for one narrows the reel
after the shuffle so the tiles that stay do not move. The tile says the
area in the tag it already had: `sním · Cestování` (D32). The set itself is
three since §19: `sním · Dělat` (D43).

Next: M4's wallpaper export (§14). Still unbuilt from M1: the desktop grid.

---

## 14. M4 — the wallpaper · 2026-09-10

§3.5 built. A few chosen dreams onto one phone-shaped canvas, so the dream
is seen a hundred times a day without opening anything.

The collage is **made on request and kept nowhere** (D33): the full-size
photographs are already on disk, ImageSharp draws them into a stream, and
the stream is the answer. There is no second tree under the media root to
own or to prune, which is the failure D26 was.

The canvas is **the phone's own screen** in real pixels rather than one of
§4's presets, so nothing is scaled up afterwards; the presets survive as the
default for a request that names no size. Up to six dreams: up to three they
stack in full-width bands, above three they pair up, and the cells sum
exactly to the canvas — `CollageLayout` is pure geometry with its own test,
so the arrangement is checked without rendering anything. No gutter, so no
colour: the one place a colour exists is `tokens.css`, which a C# renderer
cannot read.

JPEG, because the file leaves the app for a photo library and then for the
phone's own wallpaper settings. The screen offers it through the share
sheet where there is one — that is where „Uložit obrázek" lives — and a
download link otherwise, and keeps the finished picture on screen either
way. It sits in Nastavení: the bar is four slots and a wallpaper is
something you make now and then, not a place you go.

M5 (§3.6) is started: the subscription, the schedule and the endpoints are
built and tested (D34); what is left is the sending itself and the screen
that turns it on. Still unbuilt from M1: the desktop grid, which §3.2 wants
as a masonry layout — that needs the 34 rem column to widen, and
`DESIGN.md` says desktop is deliberately the phone layout with room around
it, so it is a design question before it is a screen.

---

## 15. M5 — the morning nudge · 2026-09-11

§3.6 built. One dream, on the phone, at the hour you chose: off, every day,
or only on working days, and tapping it opens that dream.

**The subscription is per device** and the push endpoint is the device, so
subscribing again moves the row rather than making a second; off is the
absence of a row, which is also what a browser that revokes one leaves
behind. The device sends its **UTC offset in minutes, not a zone name** —
the API runs with `InvariantGlobalization` and has no zone database — and
sends it again every time the app opens, so it is right the morning after a
clock change (D34).

`NudgeSchedule` is pure and tested: a weekend, a day already sent, a server
that was down all morning. It fires within two hours of the time and then
skips the day, because a dream at bedtime is not the morning habit this is.

**The crypto is written here, not taken from a package** (D35). VAPID is an
ECDSA P-256 signature over a JWT and the body is ECDH + HKDF + AES-128-GCM,
all of it in `System.Security.Cryptography`, and the test is RFC 8291 §5's
own worked example — byte for byte, so the code is checked against the
standard rather than against itself. That test earned its keep immediately:
the HKDF labels held a raw `0x01` byte rather than the two characters
`\x01`, which `grep` cannot see and which would have failed silently on a
phone at seven in the morning.

The worker wakes every minute, and a sent nudge stamps `LastShownAt` — a
notification puts a dream in front of somebody, so the board opens on the
one they were told about and tomorrow's is different (D36). The service
worker shows the notification, because a push arrives when no page of the
app is running.

`docker compose run --rm api dotnet Aspire.Api.dll vapid` makes the key
pair, before the database exists, once. Without one the API runs with
notifications off and says so; the screen tells the person rather than
offering a switch that cannot work.

**M0 through M5 are done.** What M1 left behind — the desktop grid — is
settled in §16 rather than built. §3.7's later/maybe list is untouched and
out of scope.

---

## 16. The desktop grid, dropped · 2026-09-11

§3.2's masonry grid for desktop and tablet was the last thing in the plan
that was neither built nor decided. It is **dropped** (D38), so **every
milestone is closed and nothing in §3.1–§3.6 is outstanding.**

It was never only a layout. The app is one column at most 34 rem wide, and
above 35 rem it shows its edges as a hairline and is otherwise unchanged —
`DESIGN.md`'s whole shape. A masonry board would be the only screen that is
not that. But the deciding argument is what a grid *feels* like: §2 asks for
a full-screen image per dream, and a cell in a wall of twelve is a
thumbnail. The habituation D30 fixed by shuffling the reel comes back on a
wall where everything is visible at once — nothing is ever next, so nothing
is ever arrived at — and the day's pick (D25), the reason the board is a
different one each morning, has nowhere to be on it.

A desktop wall, if it is ever wanted, is a new route beside the reel with
its own answer to what the pick means there. It is not a breakpoint on the
board.

**What is actually left**, and it is not a milestone: §3.7's share link for
a single dream. D37's open question is answered in §17.

---

## 17. What the board downloads · 2026-09-11

D37 left one question open — whether a hundred-dream board should prefetch
every photograph at all. It should not, and now it does not (D39).

**Measured rather than guessed:** a `screen` photograph is 197 kB, so a
hundred dreams is about **20 MB**. That is nothing for a phone to *store* —
the app is not going to run anybody out of room — and it is real money to
*fetch* on a metered plan, in the minute somebody is looking at the first
tile. Size matters for a second reason too: iOS drops a PWA's caches after a
week unused (§7), and a fatter origin is evicted sooner.

So the board fetches **the reel's own window** — the tiles in the document
plus a screenful ahead, ten photographs on open rather than a hundred — and
the whole of itself only where the browser says the connection is free.
Because the reel is shuffled on every open (D30), a window that only ever
grew would still arrive at the whole board in time, so there is a ceiling of
40 photographs on what windowing keeps.

And because a window narrows D24's promise that the board reads without a
signal, it is a choice rather than a default, in Nastavení · Stahování: *co
prolistuješ · na wifi · vždy celá*. Safari cannot tell wifi from mobile data,
so on an iPhone the middle one is not offered and the screen says why.

---

## 18. The reel swipes like a reel · 2026-09-11

§2 asked for a full-screen image per dream, swiped like a reel. It was built
as a snapping list of 4:5 prints, and at a hundred dreams the difference
shows: a long drag ran past four of them, a trackpad flick past more, and
between two tiles there was always a strip of ground. It is a pager now
(D40), and nothing in §3 changed to make it one.

**Every page is the screen** — full-bleed, no gap, no radius, the scroll
region the reel and nothing else — so the snap offsets are exact multiples of
it. **A swipe is then worth one dream however long it is:** `lib/ui/pager.ts`
leaves the browser its own momentum and puts a fence one page either side of
wherever the gesture began, answers a wheel gesture once and ignores its
momentum tail, and moves the arrow and page keys a dream at a time.

**The chrome floats on the photograph** — the areas rail, the anniversary,
whatever the connection has to say — because a page of chrome at the top of
the scroll region is a page of a different height, and the paging is
arithmetic. The wordmark is dropped: the bar already says which screen this
is. The empty board is still a page rather than a reel, and keeps it.

**Nothing is left in the plan but §3.7's share link**, which was already true
after §16 and is still not a milestone.

---

## 19. The list, and three areas · 2026-09-11

**The areas are Chtít · Být · Dělat** (D43). §8.3 was answered with Yager's
nine and the answer held for a day of using them: placing a dream in one of
nine is a decision you make *about* the dream, after writing it, and the
field is optional so the decision was mostly not made at all. Want, be, do is
the same question asked in the second the dream is being written. The nine
are gone from the column with it — the `Areas` migration clears every word
that is not one of the three, because nine do not map onto three without
inventing an answer nobody gave.

**And §3.7's share link is still the only thing left in this plan**, which
§18 already said. This section is not a milestone either: it is one screen
and one vocabulary.

**Seznam is the fourth tab** (D44). The reel is deliberately bad at being a
list — one dream a screen, shuffled every open, the achieved ones gone — so
`/seznam` is the board as one column, newest first, achieved and not in the
same list. ＋ opens a sheet of the five fields a sentence can answer —
Název · Proč · Afirmace · Oblast · Kdy — and the dream is saved as „sním“
with no photograph; `⊕` on the bar is still Přidat, which is the way in when
the dream arrives as a picture. The bar goes to five slots to hold it, and
`.sheet` in `app.css` finally spends `--elev-sheet` and the 28 px radius the
tokens have held since M0.

---

## 20. The button, and inviting somebody · 2026-09-11

**Deploying is one script and one button** (D45). `deploy/deploy.sh` is
§Updating with its eyes open — it refuses to fast-forward over a working copy
somebody edited on the box, and it does not call a deployment finished until
`/api/v1/health` answers — and `.github/workflows/deploy.yml` is
`workflow_dispatch` and an `ssh` that runs it. The logic is in the repository
rather than in YAML, so the same deployment happens by hand, from the button,
or from cron if it ever wants to. The key GitHub holds is not a key to the
box: it belongs to an `aspire-deploy` user and carries a forced command that
takes `aspire-deploy <ref>` and refuses everything else — no shell, no file,
no forwarding (D47).

**And a second person is a board, which was always true** (D21) — what was
missing was the code. `board invite <name>` makes the board and twelve digits
from the operating system's randomness in one command and prints them once;
`scripts/invite.sh` runs it on the box over SSH, so the code is on one
terminal and in no log (D46). §8's sixth question closed on boards being
tenants with their own codes and no accounts; this is that answer with a
command under it — their own dreams, their own photographs, their own Síň
slávy, nothing shared either way.

Still no signup form and still no login (D6). A board is something the
operator makes.
