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
- Categories (Yager-style, editable): Home · Car · Travel · Family · Freedom/Time · Giving · Business · Health · Fun.
- Image upload: phone camera, library, paste URL. Server resizes to 3 sizes (thumb / screen / full), WebP.
- Reorder dreams by drag (board order = priority).

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
- Optional short voice memo per dream (record on phone, play on detail). Yager: hear your own voice state the dream.

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
- Voice memo: `MediaRecorder` API → webm/opus upload.
- Screens: Board · Dream detail · Add/Edit · Hall of Fame · Settings.

Infra
- Contabo VPS, Docker Compose, Postgres + media volume, nightly backup of both.
- Subdomain e.g. `aspire.petrbohac.eu`.

## 5. Data model

```
users              id, email, pwd_hash, timezone, nudge_time?, created_at
categories         id, user_id, name, icon, sort_order
dreams             id, user_id, category_id?, title, why, affirmation?, target_year?,
                   status, sort_order, achieved_at?, last_shown_at?, created_at
dream_images       id, dream_id, sort_order, original_path, thumb_path, screen_path, full_path,
                   width, height, is_achieved_photo, processed_at
dream_audio        id, dream_id, path, duration_s
push_subscriptions id, user_id, endpoint, p256dh, auth
```

Daily pick rule: `ORDER BY last_shown_at NULLS FIRST, random() LIMIT 1` among status ∈ (dreaming, in progress); update `last_shown_at` on view.

## 6. Milestones

| # | Milestone | Scope | Est. |
|---|---|---|---|
| M0 | Scaffold | Copy Prosper/Planner skeleton, media volume, deploy | 1 weekend |
| M1 | Dreams + board | CRUD, upload + resize pipeline, mobile swipe board, desktop grid, categories | 2 weekends |
| M2 | PWA offline | Service worker image cache, add-to-home-screen, instant cold open | 1 weekend |
| **→ MVP live. Load real dreams. Use 2 weeks.** | | | |
| M3 | Hall of Fame + affirmations | Achieved flow, before/after, affirmation line, voice memo | 1–2 weekends |
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
3. Categories: fixed Yager set above, or fully free-form?
4. Voice memo: worth it in v1.1, or drop?
5. ~~Czech / English UI?~~ — answered 2026-09-09: Czech UI, English code, like Prosper (D2).
6. Will Zuzana use it too (shared board vs. separate accounts)? — narrowed 2026-09-09: with Prosper's auth it is one board, many devices; a *second* board would be a real design question, not a column (D6).
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
