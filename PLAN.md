# Aspire — Project Plan

Status: planning · Owner: Petr · Created: 2026-09-09

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
- Categories (Yager-style, **fixed**, §8.3): Home · Car · Travel · Family · Freedom · Giving · Business · Health · Fun. Optional on a dream (D32).
- Image upload: phone camera, library, paste URL. Server resizes to 3 sizes (thumb / screen / full), WebP.
- ~~Reorder dreams by drag (board order = priority).~~ Dropped 2026-09-10: the
  reel is shuffled on every open instead, so no order is a route you learn (D30).

### 3.2 Board view (MVP)
- **Mobile:** full-screen vertical swipe, one dream per screen (Instagram-stories feel). Tap → detail with "why" and gallery.
- **Desktop/tablet:** masonry grid, click → detail.
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
- SvelteKit PWA. Service worker caches **all** screen-size images of active dreams (typ. < 20 dreams × ~200 KB = fine). Board fully usable offline.
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
categories         id, board_id, name, icon, sort_order            (§8.3 still open)
dreams             id, board_id, category_id?, title, why, affirmation?, target_year?,
                   status, sort_order, likes, achieved_at?, last_shown_at?, created_at, updated_at
dream_images       id, dream_id, sort_order, width, height, is_achieved_photo, processed_at?
dream_audio        id, dream_id, path, duration_s
push_subscriptions id, board_id, endpoint, p256dh, auth
```

Daily pick rule: `ORDER BY last_shown_at NULLS FIRST, random() LIMIT 1` among status ∈ (dreaming, in progress); update `last_shown_at` on view.

## 6. Milestones

| # | Milestone | Scope | Est. |
|---|---|---|---|
| M0 | Scaffold | Copy Prosper/Planner skeleton, media volume, deploy | 1 weekend |
| M1 | Dreams + board | Done 2026-09-10 (§10), less the desktop grid and categories (§8.3) | 2 weekends |
| M2 | PWA offline | Folded into M1 on 2026-09-10 (D24): the photographs and the board cached, read-only offline | done |
| **→ MVP live. Load real dreams. Use 2 weeks.** | | | |
| M3 | Hall of Fame + affirmations | Done 2026-09-10 (§12): affirmation, before/after, anniversary. Voice memo dropped (§8.4) | done |
| M4 | Wallpaper export | Collage renderer, presets, save flow | 1 weekend |
| M5 | Daily nudge | Push subscription, morning notification with image | 1 weekend |

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
   screen to rename it in (D32).
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

**Categories** (§3.1 and §3.2, §8.3 answered): Yager's nine, fixed, as an
enum beside the status — optional on a dream, because a question a dream
must answer before it can be written is a dream that does not get written.
The form offers them as chips that wrap; the board offers the ones it has
something in as a rail above the reel, and asking for one narrows the reel
after the shuffle so the tiles that stay do not move. The tile says the
area in the tag it already had: `sním · Cestování` (D32).

Next: M4's wallpaper export. Still unbuilt from M1: the desktop grid.
