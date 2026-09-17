# Aspire — Project Plan

Status: M0–M8 done · §3.7 done · §26–§30 done · Owner: Petr · Created: 2026-09-09 · Revised: 2026-09-17

Third app in the personal self-improvement trio (Prosper → Planner → **Aspire**, the dreamboard). Building first. Standalone. No integration with the others for now.

---

## 1. Vision

Open phone anywhere — train, queue, bad meeting — swipe through your dreams in 10 seconds. Remember _why_.

Not a task app. Not a goal tracker. Pure fuel. Images first, words second.

Yager: "Dream building" is step one of everything. Dream must be visual, specific, and seen daily. This app is the digital dream wall that is always in your pocket.

## 2. Principles

1. **Instant.** Cold open → first dream image in < 1 s. Cached offline. No login screen on repeat visits.
2. **Visual.** Full-screen image per dream. Text overlay minimal: title + one-line "why".
3. **Emotional, not analytical.** No progress bars on the board itself. Achieved dreams celebrated, not measured.
4. **Frictionless input.** Add a dream from phone camera / photo library / URL in 3 taps.
5. **Private.** Your dreams, your VPS. A share link for a single dream, done
   2026-09-12: a page this server writes, behind a key that is its own
   permission and dies the moment it is revoked (D61).

## 3. Features

### 3.1 Dreams (MVP)

- Dream = title · 1–5 images · "why" (1–3 sentences) · category · target year (optional) · status (dreaming / in progress / achieved) · created_at.
- Categories (**fixed**, §8.3): Want · Be · Do — Chtít · Být · Dělat. Optional on a dream. Yager's nine until 2026-09-11, when the set became the three questions a dream can answer as it is being written (D43, in place of D32).
- Image upload: phone camera, library, paste URL. Server resizes to 3 sizes (thumb / screen / full), WebP.
- ~~Reorder dreams by drag (board order = priority).~~ Dropped 2026-09-10: the
  reel is shuffled on every open instead, so no order is a route you learn (D30).
  Back on 2026-09-17 **for the Seznam only**, by drag and by typing over a
  line's number (D79); the reel is still shuffled.
- „1–5 images“ above was one until 2026-09-17: up to five dreamt photographs,
  cut into a collage on the reel and on the dream's tile (D82, §29).

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

- ~~Share single dream via signed link (for Zuzana, upline).~~ — done
  2026-09-12 (§25, D61). The key is D60's, against one dream; the link opens a
  page the API writes, because a message shows a preview only if the server
  put the picture in the head.
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

| #                                              | Milestone                             | Scope                                                                                                                                                                                                                                                                                          | Est.       |
| ---------------------------------------------- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- |
| M0                                             | Scaffold                              | Copy Prosper/Planner skeleton, media volume, deploy                                                                                                                                                                                                                                            | 1 weekend  |
| M1                                             | Dreams + board                        | Done 2026-09-10 (§10). Categories followed (§13); the desktop grid dropped 2026-09-11 (D38)                                                                                                                                                                                                    | 2 weekends |
| M2                                             | PWA offline                           | Folded into M1 on 2026-09-10 (D24): the photographs and the board cached, read-only offline                                                                                                                                                                                                    | done       |
| **→ MVP live. Load real dreams. Use 2 weeks.** |                                       |                                                                                                                                                                                                                                                                                                |            |
| M3                                             | Hall of Fame + affirmations           | Done 2026-09-10 (§12): affirmation, before/after, anniversary. Voice memo dropped (§8.4)                                                                                                                                                                                                       | done       |
| M4                                             | Wallpaper export                      | Done 2026-09-10 (§14): collage renderer, the phone's own canvas, share or save                                                                                                                                                                                                                 | done       |
| M5                                             | Daily nudge                           | Done 2026-09-11 (§15): subscription, schedule, the crypto by hand, the morning notification                                                                                                                                                                                                    | done       |
| M6                                             | The review's four, and the late nudge | Done 2026-09-12 (§22): the nudge is urgent and reports its offset (D51), a sky tile takes a photograph (D52), the anniversary is the morning's nudge (D57), the heart weighs the pick and the wallpaper (D58), and the lock screen refreshes itself from a link that is its own key (D59, D60) | done       |
| M7                                             | Teď, a link, and the crop             | Done 2026-09-12 (§23): the second reel of ten in his order (D53), a focal point and zoom per photograph (D54), a photograph from a pasted link (D56)                                                                                                                                           | done       |

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
8. ~~Does the heart's count leave the reel tile once the heart weights the
   pick (§22.2)?~~ — answered 2026-09-12: yes. The tap stays with its haptic,
   the tile keeps one bit — filled heart or outline — and the number is read
   on the dream's own screen, where the facts are (D58).
9. **What is the second reel called** (§23.1)? Recommended „Teď“ — a state
   rather than a number, one syllable, reads with sním · plním. „Top 10“
   says how many, which the cap already enforces.
10. **Does the board remember which reel was open** (§23.1)? Recommended
    yes, per device, Vše as the default; the alternative is always Vše,
    because that is where the day's pick is.
11. **Does a linked picture come back to the phone, or get imported on the
    server** (§23.3)? Recommended back to the phone: one endpoint, a preview
    before the dream exists, the same upload and the same crop editor.
12. ~~How does the morning automation get in (§22.5)?~~ — answered
    2026-09-12: a link that is its own key, made and revoked from Tapeta. A
    device token typed into a Shortcut is a token out of the app and a typo
    away from a wallpaper that silently never changes (D60).
13. **Should the nudge prefer Teď** once there is a Teď (§23.1)? Open;
    default no — the nudge stays the day's pick from the whole reel until a
    fortnight of using both says otherwise.
14. ~~**Which rung does the reel read on a phone** (§24.2)?~~ — answered
    2026-09-12 as recommended: the 2048 file, `largeUrl` on the wire, on any
    screen at 2× and up that has not asked to save data (D62).
15. ~~**How full is a full board** (§24.6)?~~ — answered 2026-09-12: 2 GB,
    a 409 with a sentence, and the number on Stahování first (D64).

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
a small trophy, the way back to the dream itself (D29). It is also the
morning's notification since §22.4, replacing the day's dream on that one
morning (D57); the line stays, because the board is where it is read by
somebody who never turned notifications on. The sentence's verb agrees
with _sen_ rather than with the person, because a board is a pairing code
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

`docker compose run --rm -T api vapid` makes the key
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
not that. But the deciding argument is what a grid _feels_ like: §2 asks for
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
hundred dreams is about **20 MB**. That is nothing for a phone to _store_ —
the app is not going to run anybody out of room — and it is real money to
_fetch_ on a metered plan, in the minute somebody is looking at the first
tile. Size matters for a second reason too: iOS drops a PWA's caches after a
week unused (§7), and a fatter origin is evicted sooner.

So the board fetches **the reel's own window** — the tiles in the document
plus a screenful ahead, ten photographs on open rather than a hundred — and
the whole of itself only where the browser says the connection is free.
Because the reel is shuffled on every open (D30), a window that only ever
grew would still arrive at the whole board in time, so there is a ceiling of
40 photographs on what windowing keeps.

And because a window narrows D24's promise that the board reads without a
signal, it is a choice rather than a default, in Nastavení · Stahování: _co
prolistuješ · na wifi · vždy celá_. Safari cannot tell wifi from mobile data,
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
nine is a decision you make _about_ the dream, after writing it, and the
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

---

## 21. The list gets a shape · 2026-09-11

**Every line in the Seznam is numbered** (D48). The reel is deliberately
placeless — one dream a screen, shuffled on every open — so the one screen
that is the board as a whole may as well have landmarks in it. 1 is the top,
which is the newest, and the last number is how many dreams there are, which
is a fact the app otherwise never says. The number is the dream's place in
the whole list rather than in what is on the screen, so a narrowed list reads
4, 17, 38 and emptying the field is a way back to them.

**And typing narrows it** (D49). From six lines up there is a field above the
list, and it searches everything a dream says about itself: the title, the
why, the affirmation, the state, the area and the year — the last two in the
Czech the screen shows them in, so „splněno“ finds what is done. Diacritics
are folded off both sides, because Czech typed at speed is Czech with half
the marks missing, and words are an AND rather than a phrase. It runs on the
client over the board that is already in memory, so it answers on the
keystroke and answers offline; `dreams/search.ts` is the whole of it.

**A dream that is already written down gets said out loud** (D50). While the
title is being typed, the form compares it against the board and names what
it looks like — „Podobný sen už v seznamu máš: Island na kole · plním“ — and
then does nothing else. The pill stays live and says the word it always said:
writing the same dream twice is allowed, sometimes meant, and always his
call. The note is ember rather than red because nothing failed. The
comparison is generous about phrasing and mean about everything else
(`dreams/duplicates.ts`): Barcelona and Barcelona 2027 are the same dream,
naučit se španělsky and naučit se anglicky are not.

**§3.7's share link is still the only thing left in this plan.** This is not
a milestone either: it is one column, one field and one note.

---

## 22. M6 — the review's four, and the nudge that came late · 2026-09-12

**All five are built** (D51, D52, D57, D58, D59, D60), and the order was the
order to build in: the late nudge was a bug and went first; the tile was the
hour it looked like; the anniversary was the hour it looked like too, though it
cannot fire before September 2027; the heart got its one job, which wants a
fortnight of real swiping before anybody says whether it speaks too loudly; and
the lock screen now refreshes itself, which was the largest and the one with a
key in it.
Each item says what it decides, so `docs/DECISIONS.md` gets its entry the day
the item lands.

### 22.1 The nudge set for 7:00 arrived at 8:17 — done 2026-09-12

**Answered by the box.** The dream the nudge named was stamped
`2026-09-12 05:00:02Z`, which on +120 is two minutes past seven: the server
was on time and the push service held the message for 77 minutes. So it was
the urgency, and the rest below was built with it — the log line, the offset
on every open, the envelope's first test (D51).

**What the code says.** `NudgeSchedule.IsDue` fires from the chosen minute
until two hours after it (D34), so a worker that was not running at seven
sends at the first tick it is alive for — 8:17 is inside the window and is
what a restart at 8:17 looks like. Seventy-seven minutes is not a whole
number of hours, so it is not the offset and not summer time. And
`WebPushSender` sends every nudge with `Urgency: normal`, under a comment
that argues for the opposite — „the phone is asleep; waking it is the whole
point“. RFC 8030 lets a push service hold anything below `high` until the
phone is convenient to reach, and Apple's service carries web push through
the same queue as app notifications, where the normal priority is the one
the phone may defer while it sleeps in a pocket. A dream at a minute the
person chose is the specification's own example of a time-sensitive alert.

Two more things the code cannot tell us from here: a **sent** nudge writes
no log line — only a failure does — so the API's log cannot say whether the
server sent at seven or at 8:17. But the send stamps the dream's
`last_shown_at` with the instant it went, and that column can. And D34
promises the offset is sent again every time the app opens; it is sent only
from Upozornění when the switch is moved (`turnOn` in `lib/push/nudge.ts`).
Not this bug, but a promise the code does not keep.

**Diagnosis, on the box, before the fix** — the SSH keys are passphrase
locked, so these are for Petr's terminal:

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select title, last_shown_at from dreams order by last_shown_at desc nulls last limit 3;'
```

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select mode, at_minutes, utc_offset_minutes, last_sent_on from push_subscriptions;'
```

```bash
cd /opt/aspire/deploy && docker inspect -f '{{.State.StartedAt}}' $(docker compose ps -q api)
```

Reading it: `last_shown_at` near **05:00Z** means the server sent on time
and the hour was lost between Apple and the phone — the urgency fix is the
whole answer. Near **06:17Z** means the worker was not running at seven,
and the container's start time says whether a deploy or a restart is why.
The row should say `at_minutes 420` and `utc_offset_minutes 120`. What
only the phone can rule out: a Sleep Focus or a scheduled summary that
held it.

**The fix, whichever it was:**

1. `Urgency: high`, and the comment made true. A nudge is the only thing
   this server sends and it is sent at a chosen minute; nothing about it is
   `normal`.
2. One log line per nudge sent — board, local time, dream id — so the next
   late morning is a `grep` rather than a guess.
3. D34's promise kept: when the app opens with a push subscription in the
   browser, the device sends its offset — `POST /api/v1/nudge/offset
{ endpoint, utcOffsetMinutes }`, 204, which updates a row that exists and
   makes none. A separate verb rather than `PUT`, because `PUT` without a
   mode today writes _daily at seven_ over whatever the row said, and an
   open of the app must never move the time. It belongs with the other
   „when the app opens“ effects in `routes/+layout.svelte`.
4. A test on the sender's headers through a fake `HttpMessageHandler` —
   `Urgency`, `TTL`, the encoding — beside `NudgeServiceTests` for the
   offset verb.

**Files.** `Push/WebPushSender.cs`, `Nudges/NudgeWorker.cs`,
`Nudges/NudgeEndpoints.cs`, `Nudges/NudgeService.cs`, `Contracts.cs` and
`packages/contracts`, `lib/push/nudge.ts`, `lib/api/client.ts`,
`routes/+layout.svelte`; `apps/api/README.md` (the endpoint),
`docs/DEPLOYMENT.md` (a „when the nudge is late“ paragraph with the three
commands). **Decided** D51. **Took** an hour; tomorrow at seven is the proof.
One thing the plan did not foresee: the offset's own test caught `FindAsync`
reading through the change tracker after an `ExecuteUpdate` write, which is
D34's lesson about `DueAsync` in the method next door. It reads untracked now.

### 22.2 The heart gets a job — done 2026-09-12

**What is true today.** `POST /dreams/{id}/likes` counts, the tile and the
dream's screen show the count, and no rule reads it — not `pickDaily`, not
`DailyPick`, not `wallpaperCandidates`. On a photograph the number is a
measurement, which §2's third principle keeps off the board.

**The rule: a heart shortens the wait, and ten hearts halve it.** The pick
stays what D25 made it — the dream already stamped today holds; a dream
never shown comes before any that has — and where today the _oldest_ wins,
the one with the highest **fuel** wins:

```
fuel = days since shown × (1 + min(likes, 10) / 10)
```

Ten hearts make a dream come round twice as often as a dream with none,
and the eleventh does nothing. The cap is the point: without it ten loved
dreams would take every morning between them and the rest of the board
would never come back, which is D25's habituation with extra steps. Still
deterministic, so two devices holding the same board agree without a
stamp; ties fall to `sortOrder` as before. The same numbers in
`dreams/board.ts` and `Aspire.Domain/DailyPick.cs`, because the nudge
names a dream while the phone is asleep (D36), each with the test: a dream
with no hearts and twenty days equals one with ten hearts and ten — the
number in the first draft of this line was arithmetic no cap produces, and
the rule above is what was built; a dream stamped today is never picked
twice; never-shown still comes first.

**The wallpaper reads the same rule.** `wallpaperPick(dreams, 6)` — the
day's pick first when it has a photograph, then by fuel among the reel's
dreams with a ready dreamt photograph — is what Tapeta starts with instead
of an empty choice, and what §22.5's automatic collage is made of. One
rule, two callers, a C# twin for the endpoint; `wallpaperCandidates` stays
for the by-hand choice, achieved dreams included.

**The count leaves the tile.** The heart stays on the reel as a tap — the
haptic, the ember — and the number is read on the dream's own screen,
where the facts are. This is the half Petr may want the other way; it is
§8's eighth question, and the rule above is right either way.

**Not changed.** The tail of the reel is still a uniform shuffle (D30). The
heart speaks through the pick and the wallpaper, and one place is enough to
find out whether it speaks too loudly.

**Files.** `lib/dreams/board.ts` (+test), `lib/dreams/wallpaper.ts`
(+test), `Aspire.Domain/DailyPick.cs` and `DailyPickTests.cs`, a
`WallpaperPick` beside it, `routes/+page.svelte`,
`routes/nastaveni/tapeta/+page.svelte`, `apps/web/DESIGN.md` (the reel's
heart), `PRODUCT.md`. **Decided** D58 — D53 was spent on the two reels while
this sat in the plan. **Took** half a day, and answered §8's eighth question
yes: the number is off the tile, the heart filled on a dream that has been
fuelled and an outline on one that has not.

Two departures. The `WallpaperPick` in C# is **not** built: its only caller is
§22.5's keyed link, and a rule with no caller drifts from its twin without
anybody noticing, so it arrives with the endpoint that needs it. And the fill
is `--photo-ink`, not the ember — the accent stays off a photograph (rule 4),
and the ember heart is already on the dream's own screen.

### 22.3 A sky tile asks for its photograph — done 2026-09-12

**Why.** Since Seznam (D44), the fastest way to write a dream leaves it with
no picture, and the reel fills with words on a gradient. A tile without a
photograph has one control on it, the heart; the way to give it a picture
is two screens away.

**What.** On the reel, a tile with no dreamt photograph carries the photo
pill beside the heart — `btn--photo`, the camera, „Vybrat fotku“ — opening
the phone's own picker the way `PhotoPicker` does, with the `<input
type="file">` inside the label. The picked file is downscaled, shown on
the tile at once from an object URL so the sky becomes the photograph
before the server has answered, uploaded as `dreamt`, and the dream is
asked for again until its sizes are ready. The dream is replaced in place
in the board's state; the sequence (D30) holds, so nothing moves under the
thumb. Disabled without a signal like every write (D24). The pill lives in
`.reel__body` with `pointer-events: auto` and adds no height to the scroll
region, so rule 14 stands.

**How.** The upload-and-wait that `/sen/[id]` does in `replacePhoto` moves
to `lib/dreams/upload.ts` as `replacePhotograph(dream, blob, kind, api)`,
with the client's functions passed in so the orchestration has a test
without a browser; the dream's screen and the reel both call it. The
sentences stay: „Fotka je na nástěnce“.

**Files.** `lib/dreams/upload.ts` (+test), `routes/+page.svelte`,
`routes/sen/[id]/+page.svelte`, `apps/web/DESIGN.md` (the reel).
**Decided** D52 — a tile without a photograph is the way to give it one.
**Took** an hour or two, as estimated, and the plan held: the only addition
was letting go of the preview's object URL lazily rather than when the upload
finishes, because the two pictures swap a frame apart.

### 22.4 The anniversary is the morning's nudge — done 2026-09-12

**Why.** §3.3 asks for the reminder, and §12 left it as one line on the
board „until push is M5's“. Push is built, and the worker still picks by
`last_shown_at` and never reads `achieved_at`: the line is seen only by
somebody who opens the app that day.

**What.** On the one morning a year a dream has an anniversary, the nudge
is that instead of the day's dream. Title „Před rokem“ — „Před 2 lety“
above one — body „Splnil se ti sen „X“.“, the two lines reading as one
sentence; the photograph is the achieved one where there is one, because
the proof is the picture (D28), the dreamt one otherwise; tapping opens
the dream. One nudge a morning, so the anniversary replaces rather than
joins; the board keeps its line (D29) and opens on its pick as before.

**How.** `Aspire.Domain/Anniversary.cs`: `Today(dreams, localDate)` — the
achieved dream with this day and month in an earlier year, the most
recently achieved winning, whole years and at least one, 29 February on
29 February — a mirror of `anniversaryToday` in `board.ts`, its test a
mirror of that one's. The day is the device's, through its offset, as the
pick's is. `NudgeWorker.SendDueAsync` asks it before `DailyPick` per board;
`NudgeMessage.ForAnniversary` is the payload. The send stamps
`last_sent_on` and nothing else — the dream is achieved and the pick
ignores it. The Czech is `rokem` for one and `lety` above it, as
`formatAnniversary` has it.

**Files.** `Aspire.Domain/Anniversary.cs` (+`AnniversaryTests.cs`),
`Nudges/NudgeMessage.cs` (+test), `Nudges/NudgeWorker.cs`; a worker test
with a fake sender if the test project can reach `SendDueAsync`.
**Decided** D57 — D54 was spent on the focal crop while this sat in the plan.
**Took** the hour. The test project can reach `SendDueAsync`, and the fake
sender turned out to be worth more than a fake: `NudgeWorkerTests` decrypts
the body with the browser's half of RFC 8291, so the choice and the Czech are
checked in one place rather than a mock being asked whether it was called.

Two things the plan did not foresee, both of them already wrong. The payload's
heading was carried in a field called `dream` while a field called `title`
carried „Dnešní sen“, a constant no platform has ever shown — so the heading is
`title` now and the old name is read second, which is what an anniversary
needed before it could be headed by anything but the dream's own name. And the
anniversary stamps nothing rather than merely nothing that matters: the line
about `last_sent_on` was right, and the reason it is right is that „shown“ is a
word about the board's own turn, so tomorrow names the dream today would
have.

### 22.5 A lock screen that refreshes itself — done 2026-09-12

**Why.** §3.5's purpose is the dream seen a hundred times a day without
opening anything. Built as a one-time export, the same six dreams are the
lock screen until somebody remembers Nastavení, and a picture seen a
hundred times a day stops being seen in a week — the habituation D25 and
D30 fight on the reel, on the surface that habituates fastest.

**Check first, one minute on the phone.** Shortcuts has a „Nastavit
tapetu“ action that takes an image and aims it at the lock screen, the
home screen or both, and a time-of-day automation can run it every morning;
the wallpaper has to be a plain photo wallpaper, not a shuffle. It went
missing in one iOS 16 beta and came back, so the check is whether it is in
_his_ Shortcuts today. If it is not, steps 1 and 3 below are still worth
building and step 2 waits.

**1. Six without being told which.** `GET /api/v1/wallpaper` with no
`dreams` answers with today's six — `WallpaperPick`, the C# twin of §22.2's
`wallpaperPick`, built here because this is its only caller (D58): the day's
pick first, then by fuel among the reel's dreams with a ready dreamt
photograph — without stamping anything, because a lock screen is not the
board opening. An optional `offset` names the device's day; it is a static
link, so the screen bakes this phone's offset and canvas into it. The Tapeta
screen gets a „Dnešních šest“ pill that fills the choice with the same six,
from the same rule in TypeScript, so he sees which before he makes it.

**2. A link the phone's automation can hold.** Shortcuts fetches a URL and
sets the picture; it needs a way in. Two ways, one recommended:

- _The device token as a header in the Shortcut._ No server work. But the
  token leaves the app for a Shortcut typed by hand, and a typo is a
  wallpaper that silently never changes.
- _A link that is its own key_ — recommended. `boards.link_key`: 32 random
  bytes, base64url, made on request, unique; `GET /api/v1/w/{linkKey}` is
  the collage for that board with no header, `width`, `height` and `offset`
  in the query. Made by `POST /api/v1/board/link`, revoked and replaced by
  calling it again, removed by `DELETE`. Rate-limited per key to one a
  minute, because it renders on every hit and the pairing limiter already
  exists to copy. The key is the capability: whoever holds the link holds
  the lock screen, nothing else, and one tap makes a new one. It is also
  the mechanism §3.7's share link needs — a per-dream key the same way —
  so the last line in the plan comes almost free. Built: `BoardLinks` is the
  shape, and a `DreamLinks` beside it is the same 32 bytes against a dream's
  row instead of a board's (D60).

**3. The screen says how.** Tapeta gains „Každé ráno sama“: the link with a
copy button, and the steps in Czech — Zkratky → Automatizace → Denně v 6:55
→ „Získat obsah URL“ → „Nastavit tapetu“ (zamčená obrazovka, bez náhledu) —
and the one sentence about a photo wallpaper. Around a megabyte of JPEG at
6:55 over whatever the phone has; acceptable, and said on the screen.

**Files.** `Wallpaper/WallpaperEndpoints.cs`, a `WallpaperPick` in
`Aspire.Domain` (+test), `Board.cs` and migration `LinkKey`, a
`Boards/LinkEndpoints.cs`, `Contracts.cs` and `packages/contracts`
(`LinkResponse`), `lib/api/client.ts`, `lib/dreams/wallpaper.ts`
(+test), `routes/nastaveni/tapeta/+page.svelte`, `apps/api/README.md`,
`docs/DEPLOYMENT.md`. **Decided** D59 (the automatic six are the reel's, not
the wall's) and D60 (the link is the key) — D55 and D56 went to the laptop's
database and the link fetcher while this waited. **Took** one session rather
than two.

Four things the plan did not say. The contract carries the **path**, not the
whole URL: the browser knows its own origin for certain, and the server behind
nginx would be reading its own scheme out of a header a client can set. The
fence is **ten a minute per address**, not one a minute per key — the cost
being fenced is the rendering, which an address pays for whichever key it
presents, and a 429 to a phone's automation is exactly the silent failure this
was built to avoid. `access_log off` on that path in nginx, because the key is
in it and an access log is the most copied file on a box. And Tapeta already
opened on today's six from §22.2, so the „Dnešních šest“ pill became what it
should have been: a way back, shown only when the choice has moved.

---

## 23. M7 — Teď, a link, and the photograph's own edges · 2026-09-12 · done

**All four are built** (D53, D54, D56). A dream can now be written as a
sentence, given a photograph from the phone or from a link, cropped where the
person wants it, and put on a second reel of ten in their own order.

**What is left in this plan** is §24's M8 — §3.7's share link went with this
session (§25). The signed link §22.5 needs is the same mechanism §3.7
wants, so the two go together.

### 23.1 Two reels: Vše · Teď — done 2026-09-12

**What.** The board gets a second reel, the ten dreams he is on now, the way
Instagram has _For you_ beside _Following_. A segmented pill `seg` at the
top-left of the floating chrome — **Vše · Teď** — and the areas rail under
it on Vše only; ten dreams need no narrowing. The chrome still floats (D40)
and adds no height to the scroll region, so rule 14 stands; `--band`
measures itself as it does today.

**Teď keeps his order.** Rank 1 at the top, unshuffled. D30's argument
against a fixed order was the ninetieth swipe — a dream always reached last
is never reached — and ten are seen in ten swipes. A ranking of ten is the
point of having ten. The day's pick and its stamp stay with Vše; Teď opens
at rank 1. A dream marked splněno leaves Teď the way it leaves the reel,
and the server clears its rank so the slot is free.

**Which reel opens** is remembered per device — a `reel` key in
`localStorage` beside `offline` — with Vše as the default, because a week
of focusing should not mean a tap every morning. §8's tenth question, with
the alternative being always Vše because that is where the pick is.

**Empty Teď** is a page rather than a reel, as the empty board is: „Zatím
nic na teď“, one sentence — „Vyber až deset snů, na které se teď
soustředíš.“ — and the way to choose (§23.2).

**Everything else is the same reel.** `pager.ts`, the window of five, the
photograph cache's window (D39), the heart, the tile: Teď is `reelOrder`
over a different list. Switching resets the window and the scroll to the
first tile as `ask(area)` does. The ranks ride on the dream on the wire, so
Teď is in the board cache and works without a signal like the rest.

**Data.** `dreams.focus_rank int null`, migration `Focus`; a partial unique
index on `(board_id, focus_rank)` where the rank is not null — the same
filter SQL on Postgres and SQLite. `focusRank: number | null` on `Dream`
in both contracts; `FOCUS_MAX = 10` in `dreams/rules.ts` and
`Dream.FocusMax`. `focusDreams(dreams)` in `board.ts` with its test.

**The name** is the ninth question. „Teď“ recommended: a state rather than
a number, one syllable, fits a segment, and reads with the status words —
sním · plním · teď. „Top 10“ says how many, which is a fact the cap already
enforces.

### 23.2 Choosing the ten — done 2026-09-12

Three places, most used first.

1. **The dream's own screen.** A „Teď“ pill beside the heart, on-state
   ember. On: `POST /api/v1/dreams/{id}/focus` puts it last (rank = highest
   - 1. and answers the dream; off: `DELETE …/focus`, 204, and the ranks
        above close the gap. Ten already: 409 with „Na teď máš už deset snů.
        Některý nejdřív odeber.“ — the client says it on the tap from the board
        it holds, the way `rules.ts` says things on the keystroke. Without a
        signal the pill rests like every write.
2. **The order.** Teď's chrome carries a small `.round` pencil that opens
   `/ted`: a numbered `.card--list` of the ten in rank order, each row with
   ↑ ↓ and ×, and a „Přidat“ row that opens a sheet listing the rest of the
   reel with the Seznam's search (D49) — so the ten are assembled in one
   place. Arrows rather than drag: D30 dropped drag and nothing in the app
   has it, and ten rows is what arrows are for. One request saves the
   whole order — `PUT /api/v1/focus { dreamIds }`, ranks set atomically,
   a foreign or achieved id refused. `/ted` lights Nástěnka in `nav.ts`.
3. **The Seznam** says „teď“ first in a line that has it — `teď · plním ·
Dělat · 2027` — so the list shows the ten and typing „teď“ finds them
   (D49).

**Files.** `Dreams/DreamEndpoints.cs` and `DreamService.cs` (add, remove,
reorder, compaction, the cap, the clear on achieved; `DreamServiceTests`
for each), `Contracts.cs` and `packages/contracts` (`FocusInput`),
`lib/api/client.ts`, `lib/dreams/rules.ts` (+test), `lib/dreams/board.ts`
(+test), `routes/+page.svelte`, `routes/sen/[id]/+page.svelte`,
`routes/ted/+page.svelte`, `routes/seznam/+page.svelte`, `lib/ui/nav.ts`
(+test), `apps/api/README.md`, `apps/web/CLAUDE.md` and `DESIGN.md`.
**Decided** D53, the two halves in one entry rather than the planned D57 and
D58: they are one feature and the reasons overlap. **Took** one session, as
estimated. Two things the plan had wrong. The partial **unique** index it
asked for is not there — a unique index is checked per statement, so two
dreams swapping places collide on the way past each other, and ten rows a
board do not earn a two-phase write to prevent a tie. And §23.1 wanted an
empty Teď to be its own page _and_ `focus.ts` to fall back to Vše; the page
won, because somebody who taps Teď is owed an answer about Teď.

### 23.3 A photograph from a link — done 2026-09-12

**Can it be done.** A pin's page — `cz.pinterest.com/pin/<id>/`, or a
`pin.it/<short>` link that redirects to one — carries an `og:image` meta
tag pointing at `i.pinimg.com`, whose files are public and need no login.
The path names a size (`564x`, `736x`) and swapping it for `originals`
gives the full picture when it exists. Pinterest is an app with a login
wall and changes its markup, so the parser is written to fail with a
sentence, never a stack trace, and the first day of building includes one
`curl` of a real pin from the VPS — a bot wall answers some servers and not
others, and a browser-like `User-Agent` with `Accept-Language: cs` is the
first thing to try.

**Why the server fetches it.** The phone cannot: CORS. And a fetch from
inside the VPS is a fetch from inside the network, so the rules are the
SSRF rules, decided once (D59): `https` and `http` only; the host resolved
and every address refused if it is loopback, private, link-local or
multicast — and checked again after each redirect, at most three, because
`pin.it` is one; ports 80 and 443; ten seconds; ten megabytes, the upload's
own cap; `Content-Type` an image or `text/html` and nothing else. HTML is
read for `og:image`, then `twitter:image`, with a regular expression over
the first half-megabyte — no HTML parser, because rule 3 reaches the API
too and ImageSharp is the one media dependency it has. The page is never
kept. The image goes through `ImageProcessor.IsImageAsync` like an upload.

**The shape: the picture comes back to the phone.** `GET
/api/v1/images/fetch?url=` answers with the image itself, downscaled to
2048 on the server, and the phone treats it exactly as a picked file — the
preview on the tile, `downscale` (a no-op at that size), the same upload,
and from §23.4 the same crop editor. One endpoint and no new write path,
and a preview before the dream exists, which Přidat needs because it makes
the dream only on submit. The cost is the picture crossing the phone's
connection twice, a few hundred kilobytes each way. The alternative — `POST
/dreams/{id}/images/from-url`, imported on the server — saves the trip and
has neither a preview nor a dream to import into until one is saved. §8's
eleventh question; the first is recommended.

**The screen.** `PhotoPicker` gets a second pill, „Z odkazu“ with a link
icon, beside „Vybrat fotku“; it raises a `Sheet` with one field — „Odkaz na
obrázek nebo pin“ — and „Vzít“. Paste, fetch, preview. A direct image URL
takes the same road; the content type decides. The failure sentence: „Z
tohohle odkazu fotku nedostanu. Ulož si ji do telefonu a vyber ji.“ On an
iPhone the Pinterest app's share sheet copies a `pin.it` link and paste is
the way; Android gets `share_target` in the manifest for a shared link, so
sharing a pin opens Přidat with the field filled — a dozen lines, for
Zuzana's phone if it is one.

**Files.** `Images/ImageFetcher.cs` (the guard, the redirects, the meta
tag) with `ImageFetcherTests` through a fake handler and HTML fixtures,
`Images/ImageFetchEndpoints.cs`, `Program.cs` (`AddHttpClient`),
`lib/api/client.ts`, `lib/ui/PhotoPicker.svelte`, `lib/api/errors.ts`,
`static/manifest.webmanifest`, `apps/api/README.md`. **Decided** D56, not the
planned D59. **Took** one session. Pinterest's wall cost nothing in the end —
the fence and the parsing were the work, and both were proved against the live
internet rather than only against fixtures. Two things the plan had wrong: the
read cap must _truncate_ a page rather than refuse it, because a news page is
megabytes of script and refusing big pages means refusing most of the web; and
the failure sentence has to be said inside the sheet, not handed to the screen
underneath it where the sheet hides it.

### 23.4 The crop is his — done 2026-09-12

**What is true today.** The phone sends the photograph at 2048, the server
keeps three sizes, and every surface crops from the centre: the reel on a
9:19.5 screen with `object-fit: cover`, the Síň slávy at 4:5 and in the
pair, the Seznam in a circle, the wallpaper's cell through ImageSharp's
`AnchorPositionMode.Center`, the picker's preview. A portrait with the
face at the top loses the face on the reel and there is nothing to do
about it.

**A focal point and a zoom per photograph, not a cropped file** (D60).
Three numbers on `dream_images`: `focus_x`, `focus_y` in 0–1 with 0.5 as
the centre, `zoom` at least 1 with 1 as all of it. Every surface honours
them: in CSS as `object-position` from the point and `transform: scale()`
from the zoom with `transform-origin` at the point, set as custom
properties on the `<img>` by one helper, `photoStyle(image)` in
`dreams/photos.ts`, so the reel, the wall, the circle and the picker all
read it through `.dream__img`; on the server as `FocalCrop.For(image,
cell, fx, fy, zoom)` — the cover crop shrunk by the zoom and moved so the
point is as near the cell's centre as the edges allow — pure geometry
beside `CollageLayout` with the same kind of test, and what
`CollageRenderer` crops with. The three files on disk are untouched: the
crop is metadata, so changing it is instant and costs no resize.

**Why not crop the pixels on the phone.** Each surface is a different
shape. A crop made for the reel is wrong for the 4:5 pair and the circle,
and a file cropped once is cropped for every screen for good. One point
and one zoom are a truth every shape can read; the zoom is what makes
„size“ his as well as position.

**The editor.** After a pick in `PhotoPicker` — and on the dream's screen
through a „Posunout“ pill on the tile — the tile becomes the frame of the
surface that matters most, the reel: this phone's own aspect, the title
and the line overlaid where they will be. One finger drags the picture;
two pinch it, 1× to 3×; a wheel zooms on a desktop. Pointer events,
hand-rolled, `touch-action: none` inside the editor and nowhere else. The
arithmetic — from a drag and a scale to the point and the zoom and back,
clamped so the picture always covers the frame — is `lib/images/focal.ts`,
pure and tested. „Hotovo“ and „Na střed“, and nothing animates under
reduced motion. Why the reel's shape rather than a neutral square: the
reel is the product, and the person is positioning a photograph for a
screen he will see every morning.

**Saving.** A new upload carries the three with the multipart, so the
photograph arrives placed; an existing one takes `PUT
/api/v1/dreams/{id}/images/{imageId} { focusX, focusY, zoom }`. The DTO
carries them, so the board cache does (D24), and the achieved photograph
has its own point because it is its own row.

**Files.** `DreamImage.cs` and migration `Focal`, `Contracts.cs` and
`packages/contracts`, `Images/ImageService.cs` and `DreamEndpoints.cs`,
`Media/FocalCrop.cs` (+`FocalCropTests`) and `CollageRenderer.cs`
(+ a renderer test with an off-centre point), `lib/images/focal.ts`
(+test), `lib/dreams/photos.ts` (+test), `lib/styles/app.css`,
`lib/ui/PhotoPicker.svelte`, `routes/+page.svelte`,
`routes/sen/[id]/+page.svelte`, `routes/sin-slavy/+page.svelte`,
`routes/seznam/+page.svelte`, `apps/api/README.md`, `apps/web/DESIGN.md`.
**Decided** D54, not the planned D60. **Took** one session rather than two.
The plan held almost exactly; what it did not foresee was two browser-only
failures. A photograph already in the cache fires `load` before the editor
exists, so its size was never read and every gesture was a silent no-op — the
element is asked directly now. And `setPointerCapture` throws on a pointer the
browser has already released, so it is guarded. Neither would have shown up in
a unit test, and both made the editor look finished and do nothing.

## 24. M8 — the photographs at scale · 2026-09-12 — done

**Built 2026-09-12**, §24.4, §24.2 with §24.3 inside it, §24.5 and §24.6,
in that order (D62, D63, D64, and D23 amended). Three things differ from the
text below. §24.1's measurement on his phone did not happen — the change
went in on the arithmetic, and `rungFor` is one line to revert if the phone
disagrees. The board's number rides on a new `GET /api/v1/board` rather than
"beside the name", because nothing answered `/board` before. And the
cache's ceiling doubled to eighty files, because a dream is two files now.
The decision numbers below were written when D60 was the highest; D61 went
to §25 in between, so §24.2 is D62, §24.5 is D63 and §24.6 is D64.

**The question.** Hundreds of photographs today, maybe video and audio one
day: how does one VPS carry that without a rack behind it, and what is the
compromise between the bytes the server keeps and the picture the person
sees? The answer is mostly arithmetic, and the arithmetic says the disk is
not the problem. What is worth doing is what a photograph _is served as_,
which is where the phone can tell the difference.

**What is true today.** The phone downscales to 2048 px and sends a JPEG at
0.86 (`images/downscale.ts`), so the server never sees the original. The
worker makes three WebPs at quality 82 — thumb 400, screen 1280, full 2048
— strips EXIF, and nginx serves the tree immutable for a year (D23). The
service worker answers a photograph from its cache before the network
(D24), and the board fetches a window of the reel ahead of the thumb (D37,
D39). That is Pinterest's own model in miniature: one upload, a fixed
ladder of widths — their URLs literally say `236x`, `474x`, `736x`,
`originals` — a modern codec, and a cache in front. They add a CDN and
object storage because they have a few billion pins, not because the
per-picture scheme is different.

**What one photograph costs**, measured on the VPS for `screen` (§17) and
estimated for the rest at the same quality:

| Rung   | Longest edge | About  |
| ------ | ------------ | ------ |
| thumb  | 400          | 25 kB  |
| screen | 1280         | 200 kB |
| full   | 2048         | 500 kB |

A board of a hundred dreams with two photographs each is about 150 MB. A
200 GB disk holds a thousand such boards, and there are two. **Photographs
will never need a second server.** Video would, and §24.6 says what would
have to be true first.

So the milestone is five small things and one rule, in the order to build
them. §24.1 is measured before anything is changed, because it decides
whether §24.2 exists.

### 24.1 The reel is shown at 1280 on a 2796-pixel screen

**The arithmetic.** The reel is full-bleed, `object-fit: cover`, on a
portrait phone. A 3:4 photograph at `screen` is 960×1280. On an iPhone at
3× — 1290×2796 device pixels — cover scales it by the larger of 1290/960
and 2796/1280, which is **2.2×**; on a 2× phone at 1170×2532 it is 2.0×.
Every photograph on the reel is drawn at twice its pixels, and the crop
editor's zoom (§23.4) multiplies that. Instagram's stories are 1080×1920 on
the same screens, a 1.5× stretch, and that is the most a photo app that
lives on quality accepts. The `full` rung at 2048 is 1536×2048 for the
same photograph — 1.4× on the 3× phone, 1.2× on the 2× — and it already
exists on disk, written for every upload and read by nobody: no screen
in `apps/web` references `fullUrl`, and the only reader of the file is
the collage (§14).

**Measure first.** Day one is one dream, on the reel, on his phone, at
`screen` and at `full`, side by side. If he cannot tell them apart at
arm's length, §24.2 is dropped and `full` becomes the archive (§24.3)
and nothing more. If he can — and the arithmetic says he can — §24.2 is
the milestone's reason.

### 24.2 The reel reads the rung the phone is — done 2026-09-12

**What.** A screen chooses its rung from what the device is, in one place
— `reelUrl(image)` in `dreams/photos.ts` beside `photoStyle` — and
everything that shows a reel-sized photograph reads it: the reel, the
dream's own screen, the cache's prefetch (`offline/cache.ts`), so what is
fetched ahead is what is shown. The rule is small: `full` when
`devicePixelRatio` is 2 or more and `saveData` is off; `screen`
otherwise. A phone is 2× or 3×; a laptop at 1× and a metered connection
get 1280, which is right for both. No `srcset`: the browser choosing per
image would put a URL in the cache that the prefetch never asked for, and
D39's window is a promise about bytes that a browser picking for itself
would break.

**The cost is the point of the compromise.** The reel's dream goes from
200 kB to about 350 kB at §24.4's quality — the whole board on wifi from
20 MB to 35 MB, a morning of ten swipes from 2 MB to 3.5 MB. `policy.ts`
already says storing tens of megabytes is nothing and fetching them on a
metered plan is the question; the numbers change, the answer does not.
§8's fourteenth question, because it is his data plan: recommended
`full` on a phone, with the alternative being a new rung at 1600 — 1.7×
on a 3× phone, 250 kB, a fourth file for every upload and a sweep to
make it for the ones already there.

**The wall, the circle, the nudge stay where they are.** The Síň slávy
shows a pair at half width, the Seznam a 40 px circle, the notification
a picture the size of a thumb: `screen` and `thumb` are right for them
and none of this touches them.

**Files.** `lib/dreams/photos.ts` (+test), `lib/offline/cache.ts`
(+test), `routes/+page.svelte`, `routes/sen/[id]/+page.svelte`,
`apps/web/CLAUDE.md`. **Decides** D62. **Size:** half a session.

### 24.3 `full` stops being on the wire — done 2026-09-12

**What.** Whether or not the reel reads it, `full` is either the reel's
rung or the archive, and in neither case is it a thing a client should
be handed as a third URL to guess about. The client builds no URL; the
server names the rungs it serves and the client picks among named ones.
So `DreamImage` on the wire carries `thumbUrl`, `screenUrl` and — under
§24.2 — `largeUrl`: `full` renamed on the wire and nowhere else, because
the file keeps its name and a year of cached URLs is a year of cached
URLs (D23). If §24.1 says the phone cannot tell, `fullUrl` leaves the
contract and nothing replaces it: the file stays what it is, the
collage's source and the one from which a new rung can be made without
asking for the photograph again, which is what an archive is for.

**Why it is kept at all.** A ladder changes — this milestone may add a
rung — and a photograph that was only ever kept at 1280 can never be
made larger. 500 kB a photograph, 50 MB a board, is the price of never
having to ask him for a picture twice. The alternative, the client's own
JPEG kept as the archive, is a third encode avoided at a larger file:
a JPEG at 0.86 from a canvas is 700–900 kB at 2048. WebP wins.

**Files.** `Contracts.cs` and `packages/contracts` in the same change
(rule 6), `lib/dreams/photos.ts`, the four `*.test.ts` fixtures that
spell `fullUrl`, `apps/api/README.md`. **Size:** an hour, inside §24.2.

### 24.4 The encoder, tuned once — done 2026-09-12

**What.** Three settings in `ImageProcessor`, each measured on five of his
own photographs before it is kept — a beach, a face, a city at night, a
document, a screenshot — looked at on the phone at the rung they are for.

1. **Quality 82 → 75** for `screen` and `full`, **70** for `thumb`. On a
   photograph at phone density the step from 82 to 75 is invisible and
   the file is about a quarter smaller; Instagram and Pinterest sit
   around 70–75. It is a constant, and the measurement is the reason to
   trust it. Every existing file stays as it is: the URLs are immutable
   and re-encoding what is already served buys nothing.
2. **`WebpEncoder.Method = BestQuality`** — libwebp's effort 6 rather
   than the default 4 — is 5–10 % smaller at the same quality for a
   slower encode. The worker is a background queue on an idle box (D23);
   the seconds are free.
3. **Lanczos3 and a light sharpen after the downscale.** ImageSharp
   resizes bicubic by default; Lanczos keeps edges, and a `GaussianSharpen`
   at a small sigma after the resize is what every photo host does to make
   a downscaled picture look like a photograph rather than a soft copy of
   one. This is the one that most changes how 1280 _looks_ for no bytes
   at all, and the one most worth looking at before keeping.

**Not AVIF.** It would be a third smaller again, and ImageSharp does not
encode it; a native `libavif` binding is a dependency (rule 3) on the
API and a second decode path on every phone, for a saving the arithmetic
does not need. WebP is on every phone this app runs on. Revisit when
ImageSharp ships it.

**Not a hash.** A content hash to share files between two dreams that
were given the same photograph is one collision a year on a board like
this. A column and a shared directory for that is not worth the
deletion rules it needs.

**Files.** `Media/ImageProcessor.cs`, `ImageServiceTests` (a size
assertion per rung on the fixture, so a setting that regresses fails a
test), `docs/DECISIONS.md` (D23 amended, not replaced). **Size:** half a
session, most of it looking.

### 24.5 The tile knows what is coming — done 2026-09-12

**What is true today.** Between the swipe and the photograph there is the
sky (D26, D52), and on a slow connection the sky is what a swipe past the
window shows for a second. Pinterest paints each pin its dominant colour
before the picture lands; Instagram draws a tiny blurred copy. This app
has the tiny copy already: `thumb` is 25 kB, and on a device that has
opened the Seznam it is in the cache.

**The thumb under the screen.** The reel tile draws `thumbUrl` first,
blurred by CSS and covering the same frame with the same `photoStyle`, and
the reel's rung on top; when the large one has decoded the small one is
under it and nobody sees it go. It is a second `<img>` in the same
absolutely-positioned frame, so it adds no height to the scroll region
(rule 14). The sky stays for a dream that has no photograph at all — that
is a different sentence (D52). The cache's prefetch asks for the thumb
one tile _before_ it asks for the screen of the same dream, so on a
metered window the shape arrives before the picture does.

**What it costs.** 25 kB a dream on the wire, which is an eighth of the
picture, and the ten-swipe morning goes from 3.5 MB to 3.75. The whole
board on wifi already fetches the thumbs for the Seznam, so there it is
free.

**Files.** `routes/+page.svelte`, `lib/styles/app.css` (`.dream__under`),
`lib/offline/cache.ts` (+test, the order), `apps/web/DESIGN.md`.
**Size:** half a session. **Decides** D63.

### 24.6 The rule for the disk, and what video would need — done 2026-09-12

**What is measured.** Every upload writes a `bytes` column on
`dream_images`, the sum of its files, set by the worker beside `width`
and `height`; migration `Bytes`, backfilled for what is there by a sweep on
start. `GET /api/v1/dreams` is not the place — the board is fetched a
window at a time — so it rides on `/api/v1/board` beside the name, and
the `board` command prints it beside the code (D46). §7's "500 MB per
user" was never built and would be wrong: a hundred dreams at two
photographs is 150 MB and a board is meant to reach a hundred. The cap is
**2 GB a board**, the number at which something other than dreaming is
happening, refused with a sentence — „Nástěnka je plná. Smaž pár
fotek, které už nepotřebuješ.“ — and the count shown in Nastavení →
Stahování under what the device keeps, so the person sees the number
before the sentence does. §8's fifteenth question, because it is his
board: 2 GB recommended.

**The rule for moving off the disk.** Not until the media volume passes
half of what `df -h /var/lib/docker` shows (`docs/DEPLOYMENT.md`), and
not for photographs at all on the arithmetic above. When it comes, it is
one bucket, not one server: an S3-compatible store in the same
jurisdiction — Hetzner Object Storage in Falkenstein, or Cloudflare R2
with no egress to pay — behind nginx's `proxy_pass` and `proxy_cache`
under the same `/media/` path, so no URL changes, the service worker's
cache-first stays true and the year of `immutable` is kept. `MediaStore`
grows an interface with the disk behind it and the bucket the second
implementation; `AWSSDK.S3` is the dependency, asked for then (rule 3).
`backup.sh` stops tarring the volume and turns on the bucket's
versioning. Nothing of this is built now; this paragraph exists so the
next decision is already made.

**What video would have to be.** §3.7's later-maybe, and the thing that
changes the disk's arithmetic: fifteen seconds of 1080p H.264 at a
sensible bitrate is about 5 MB, ten photographs' worth. If it is ever
built: a cap on length rather than on bytes — fifteen seconds, because a
reel is a glance and a minute is a film — transcoded by `ffmpeg` in the
same worker and the same queue, never in the request; H.264 and AAC in
MP4 because that is what an iPhone plays without asking; the poster
frame written through `ImageProcessor` as the dream's `thumb` and `screen`,
so every screen that shows a photograph keeps working and the reel alone
learns `<video muted loop playsinline>`. An MP3 is a megabyte a minute and
needs nothing but a row and a file. Neither is in this milestone.

**Files.** `DreamImage.cs` and migration `Bytes`, `Images/ImageService.cs`
(+test for the cap), `Boards/`, `Contracts.cs` and `packages/contracts`,
`routes/nastaveni/stahovani/+page.svelte`, `Program.cs` (`board`),
`docs/DEPLOYMENT.md` (the rule and the runbook paragraph for the bucket).
**Size:** one session. **Decides** D64.

### 24.7 What this does not change

The phone's downscale to 2048 (rule: send once, send small), the staged
upload in the temp directory (D23), the 10 MB cap, the 202 and the worker,
the ask-again loop, immutable URLs by id, the three caches (D24), the
window (D39). They are the reason the arithmetic works, and the milestone
adds to them rather than moves them. **Order:** §24.1 measured, then
§24.4 (it changes every file written after it, so it goes first), §24.2
with §24.3 inside it, §24.5, §24.6. **Size:** three sessions.

---

## 25. §3.7, the last line — done 2026-09-12

**A dream somebody else can open** (D61). One line in §3.7 since M0, and the
last thing in the plan that was never a milestone. The mechanism came free
with §22.5: the same key, against one dream rather than one board.

**What.** `Sdílet` on a dream's own screen opens a sheet with a link,
`https://…/s/<key>`, and the phone's own share sheet sends it. What the other
person opens is a page this server writes: the photograph, the name, the
affirmation, and nothing else — no script, no stylesheet, no font, no link
back into the board, `noindex`, `no-store`. `Nový` replaces the key and
`Zrušit` removes it; either way the old link 404s from the next request.

**Why a page rather than a route of the app.** The person it is sent to has
nothing — no app, no code, no account — and is reading a message. A message
shows a preview only if the _server_ put the picture in the head, and a
client-rendered route of the PWA arrives at WhatsApp as a bare URL. So the
API writes HTML for the first and only time, and renders its own preview card
at 1200×630 as a JPEG, because the photographs on disk are WebP and some chat
apps still will not draw one in a card.

**Files.** `Auth/ShareKey.cs` (both keys are made there now),
`Dreams/DreamLinks.cs` (+test), `Dreams/DreamLinkEndpoints.cs`,
`Dreams/SharePage.cs` (+test), `Dream.cs` and migration `DreamLinkKey`,
`Program.cs`, `Contracts.cs` and `packages/contracts`, `lib/api/client.ts`,
`lib/dreams/share.ts` (+test), `routes/sen/[id]/+page.svelte`,
`service-worker.ts` (it must not answer `/s/` with the app's own shell),
`vite.config.ts`, `deploy/nginx/app.conf`, `apps/api/README.md`,
`docs/DEPLOYMENT.md`. **Decided** D61. **Took** half a day.

Two things found while building it. `WebUtility.HtmlEncode` escapes every
character above ASCII, which in a Czech app is most of the sentence and twice
the bytes for nothing — the five that matter are escaped by hand instead. And
a dream with no photograph was claiming a preview card that would have 404'd;
it claims none now, because a broken picture looks broken where no picture
looks deliberate.

**What is left in this plan is §24's M8**, and nothing else.

## 26. Three screens and four seams · 2026-09-12 — done

Not a milestone: M8 closed the plan, and this is what a walk through the
finished app turned up. Petr picked three of the five things the walk found
on the screens and all four in the code.

**On the screens.**

1. **Smazat waits** (D65). It was one tap, no confirmation and no way back,
   on a server that takes the photographs with the row. The request is held
   for as long as the toast stands and „Vrátit“ cancels it; leaving the app
   commits it with `keepalive`. Every list drops the dream at once.
2. **The sky says it is working** (D52 amended). A photograph still being
   resized looked exactly like a dream that has none, under a pill asking
   for the one already on its way. The tile says „Zpracovává se…“ instead,
   in the action row so it adds no height (rule 14).
3. **The rung reads the connection** (D66). D62 gave 2048 to every 2×
   screen, mobile data included, at 350 kB a swipe. Metered reads 1280; a
   browser that will not say is still treated as free, so the iPhone keeps
   the rung that was built for it.

Two the walk found and Petr left: a jump from the reel to a dream in the
Seznam, and the board's name on the Nastavení hub.

**In the code.**

4. **A page of the reel is a component.** `ui/ReelTile.svelte`, with every
   style that makes a page exactly one scrollport tall — a scoped rule left
   behind by its markup stops matching and says nothing, and
   `.reel__fuel--lit svg` is the only thing on the board that shows a dream
   has been fuelled since D58 took the count off the tile. The route is 860
   lines, from 1023.
5. **A control that writes wears the lock** (D67), rather than each screen
   remembering `!connection.online`.
6. **Components get tests** (D68): a second Vitest project in happy-dom,
   nine on `ReelTile`.
7. **The endpoints get tests** (D69): fifteen over a real request, through
   `WebApplicationFactory<Program>`.

**`app.css` at 1213 lines was left alone.** It is long because it is the one
place the primitives live, and rule 2 is the reason for that; splitting it
into files would mean a screen could declare a primitive of its own without
anyone noticing, which is the thing the rule exists to stop. Long and
single is the shape that was chosen.

**Took** one session. **Decides** D65, D66, D67, D68, D69, and amends D52.

## 27. Five from the phone · 2026-09-13 — done

Five things Petr found using the app on his own phone. Not a milestone, and
none of them in the plan.

1. **The notification's badge was a white square** (D70). Android fills a
   badge's alpha channel with the system accent, so a full-colour icon on an
   opaque plate arrives as the plate. `badge-96.png` is the sun and horizon
   as a silhouette on nothing.
2. **The chosen reel could not be read.** `.seg--glass .seg__item` and
   `.seg__item[aria-pressed]` share a specificity, so the later one won and
   gave the selected segment `--ink` — which is the colour `--pill` already
   is, in both banks. Near-black on near-black in the light theme, near-white
   on near-white in the dark. One rule of higher specificity fixes it, and
   both themes were checked.
3. **A shared link stretched the photograph over the whole page** (D73). It
   is a 4:5 card centred in the window now, with the person's own crop on it.
4. **One reminder a day became up to five** (D72). `times` and
   `last_sent_minutes` on the row, `DueAt` in place of `IsDue`, a migration
   that carries the old hour across before it drops it, and a row per
   reminder on the screen.
5. **The board takes a sideways swipe** (D71) to change which reel it is,
   beside the pager rather than inside it, and biased 1.4 to 1 in the pager's
   favour so a flick up the reel is still a dream.

**Took** one session. **Decides** D70, D71, D72 and D73.

## 28. The mark, and the glass · 2026-09-13 — done

Two more from the phone, after §27.

1. **The notification wears the mark as a white outline** (D70 amended), in
   both places one shows: on the app's ink for the picture in the
   notification, and with the plate off for the status bar. The colour
   home-screen icon is not chrome, and chrome here is ink and white.
2. **The reel segment is the tab bar's glass** (D74): the same masked rim,
   the same sheen, and a lens that springs to the chosen reel, instead of a
   dark pill stamped into a glass track.

And the thing that made the second one possible without repeating itself:
**glass is one decision now**. Three blur tokens in `tokens.css` replace the
literals at eight surfaces, `prefers-reduced-transparency` is answered once
for both themes, and the no-backdrop fallback covers every glass class
rather than only the bar. It already worked on iPhone, Android and the web —
`-webkit-` for WebKit, unprefixed for the rest — and now it degrades the
same way everywhere too.

**Took** one session. **Decides** D74, amends D70.

## 29. Nine from using it · 2026-09-17 — done

Nine things Petr asked for after living with the finished app. Not a
milestone. Four of them overturn something the plan or a decision had
settled, so he was asked about those four before a migration was written; the
answers are in the decisions they became.

1. **A line of the Seznam slides aside** (D78): swiped left it shows the
   hearts and their count, Sdílet and Smazat. D58 took the count off the
   reel's tile, not off an inventory's line; Smazat is D65's six seconds.
2. **Začít znovu** (D80): every dream and photograph, behind Prosper's typed
   phrase, under Nastavení. Pairing, nudges, the lock-screen link and
   appearance stay.
3. **The tab bar hung below the screen on a cold start** (D75). The frame is
   pinned to the viewport's edges instead of being `100dvh` tall. Not
   reproducible on a laptop; the phone is the test.
4. **The Seznam's order is his own** (D79, amending D30, D44 and D48): a line
   is dragged, or its number is typed over, and both are one move. The reel is
   not told — Vše is still shuffled.
5. **A sheet is pulled down to put it away** (D76), with Prosper's numbers.
6. **A stats bar on the Seznam** (D77): celkem · sním · plním · splněno, each
   a button that narrows the list.
7. **„Naposledy upraveno“** (D77), for the board under the stats and for each
   dream on its own screen. Teď stopped stamping it.
8. **Up to five photographs, and collages** (D82): three templates for every
   count from two to five, on the reel and on the dream's tile; the first
   photograph is the cover everywhere small. §3.1 had said „1–5 images“ since
   the first draft, so this one closes a line of the plan rather than adding
   to it.
9. **A photograph can be shown whole, on a mat** (D81): `fit` and `mat` on the
   photograph, six mats, and a landscape picture opens whole. This is the
   answer to „zoomed, in bad quality on the board“.

Three migrations, none of which edits another: `SeznamOrder` (data only — it
turns `sort_order` over), `PhotoFit` (`fit`, `mat` on `dream_images`) and
`DreamLayout` (`layout` on `dreams`). Three new endpoints under a dream —
`place`, `layout`, `images/order` — and `POST /board/reset`. No dependency
was added; the entry route is 33.6 kB brotli of 150.

**Took** one session. **Decides** D75–D82; amends D30, D44, D48 and D58, and
extends D54 and rule 12.

## 30. Four more · 2026-09-17 — done

Four things Petr asked for the same afternoon, one after another. Not a
milestone, and none of them overturns a decision, so none was asked about.

1. **The Seznam narrows to Teď** (D83): the board's Vše · Teď under the counts,
   in the list's order and with its numbers, stacking with a count and the
   search, and kept when he comes back from a dream.
2. **Five photographs from the first pick** (D84): every way a dream gets a
   photograph — its empty tile, Přidat, the reel's pill, the shelf's ＋ — takes
   as many as fit, sent in the order picked.
3. **A collage is placed as a collage** (D85): the chosen template on the
   reel's page, every cell under a finger, the three templates over it. The
   fingers were lifted out of `CropEditor` into `ui/placing.ts`, and the
   editor's words and pills came out from under its scrim.
4. **A carousel on the dream's tile** (D86): the collage, then each photograph
   on its own, swiped across with dots under it. Not on the reel, where a
   swipe across is the other reel (D71).

No migration, no endpoint and no dependency; the entry route is still 33.6 kB
brotli of 150.

**Took** one session. **Decides** D83–D86; extends D48, D54, D77 and D82.
