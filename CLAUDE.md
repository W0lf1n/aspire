# CLAUDE.md

Guidance for Claude Code working in this repository.

**Last revised:** 2026-09-10 · M3 · 65 web tests · 95 API tests

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
hundred tiles. Next is categories (§8.3, still an open question), then M4.
Still unbuilt from M1: the desktop grid.

---

## Commands

Run from the repository root.

```bash
pnpm install
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

`pnpm api` runs the API in Development: SQLite in `apps/api/src/Aspire.Api`,
port 5300, pairing code `000000`. `pnpm dev` proxies `/api` to it. `pnpm
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
│                  seg, chip, toggle, facts, well, field, btn, dream.
├─ lib/ui/         Hand-rolled components. No component library.
├─ lib/api/        client.ts (fetch + bearer), token.ts (localStorage),
│                  pairing.ts (the flow) and errors.ts (the sentences).
├─ lib/dreams/     rules.ts — what a dream may be — board.ts — what the reel
│                  shows, which dream it opens on, what order the rest are
│                  in, what a tile says and whose anniversary today is —
│                  photos.ts — which of a dream's two photographs a screen
│                  shows — and format.ts.
├─ lib/images/     downscale.ts — the photograph to 2048 px on the device.
├─ lib/offline/    status.svelte.ts — the connection flag every screen reads —
│                  and cache.ts, which fills and prunes the photo cache.
├─ routes/         / · /pridat · /sen/[id] · /sen/[id]/upravit · /sin-slavy
│                  · /nastaveni · /nastaveni/vzhled · /nastaveni/parovani
│                  · /styleguide (unlinked, for review)
└─ service-worker.ts  three caches: the shell per build, the photographs
                      cache-first, the board network-first (D24).

apps/api/src/
├─ Aspire.Api/             Program.cs (minimal APIs), Auth/, Boards/, Dreams/,
│                          Images/ (the queue and the worker), Contracts.cs
├─ Aspire.Domain/          Board, Dream, DreamImage, Device. No EF.
└─ Aspire.Infrastructure/  AppDbContext, Media/ (the store, the resize), Migrations/
apps/api/tests/Aspire.Api.Tests/   xUnit, SQLite in memory

packages/contracts/  the wire types; mirrored in Contracts.cs
deploy/              compose, nginx, the host vhost, backup.sh
```

---

## Rules that are not negotiable

1. **Colours only from `tokens.css`.** A literal hex in a component is a bug.
   Two accents exist, `--signal` (ember) and `--dusk`; there is no third.
   The weight ladder is **400 / 500 / 600** and **nothing is uppercase**.
2. **No component library, no CSS framework.** The primitives live once in
   `app.css`; a screen declares only its difference.
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
11. **A photograph has two kinds**, `dreamt` and `achieved`, and no screen
    picks one by hand. `dreams/photos.ts` says which one a screen shows and
    which ones a replacement takes with it, so changing the dreamt
    photograph never takes the proof with it (D28).

---

## Traps

**SQLite cannot order by `DateTimeOffset`.** `ThenBy(d => d.CreatedAt)`
threw a 500 on the laptop and would have passed on Postgres. Order by the
id instead, and run the laptop mode before calling an endpoint done.

**The laptop's `aspire.db` never migrates.** SQLite mode creates the schema
as the model stands and then leaves it alone, so after a model change the
old file answers "no such column". Delete it and start the API again.

**A Bash heredoc over about 8 kB is cut short on this machine** and fails
with an unmatched quote. Write large files with the Write tool.

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
write before it serves. A check that a directory *exists* is not a check that
you can write into it.

## Where the answers are


| Document              | What it is                                                     |
| --------------------- | -------------------------------------------------------------- |
| `PLAN.md`             | The plan and its open questions                                |
| `docs/DECISIONS.md`   | Every answered question and every deviation. Binding           |
| `docs/DEPLOYMENT.md`  | The VPS runbook                                                |
| `apps/web/DESIGN.md`  | The design system as built                                     |
| `apps/web/PRODUCT.md` | Product truth for design work (Impeccable)                     |
| `apps/api/README.md`  | The API: endpoints, running it, migrations                     |
