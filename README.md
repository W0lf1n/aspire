<div align="center">

<img src="docs/brand/icon.svg" width="96" alt="Aspire">

# Aspire

**A personal dreamboard: your dreams, one on the whole screen, one swipe each.**

Open the phone anywhere, swipe through your own dreams in ten seconds, and
remember why. Not a task app, not a goal tracker. Pure fuel.

</div>

---

## Status

**M0, the scaffold.** The repository, the API with pairing auth and the first
migration, the client shell with the design system, the deployment, and CI.
Nothing can be added to the board yet; that is M1 in [`PLAN.md`](PLAN.md).

The look is reviewable at `/styleguide` in either theme before M1 builds on it.

---

## Run it locally

Requires Node 24+, pnpm 11+ and the .NET 10 SDK. No Docker and no Postgres
needed: the API runs on SQLite on a laptop.

```bash
pnpm install
```

```bash
pnpm api
```

```bash
pnpm dev
```

Then <http://localhost:5173>. The API answers on `127.0.0.1:5300` and Vite
proxies `/api` to it, so the client is same-origin the way it is in
production. The development pairing code is `000000`; the token flow arrives
with M1.

| Script          | What it does                                                 |
| --------------- | ------------------------------------------------------------ |
| `pnpm dev`      | The client with hot reload, proxying `/api` to the API       |
| `pnpm api`      | The API in Development: SQLite in `apps/api/src/Aspire.Api`  |
| `pnpm test`     | The client's unit tests (Vitest)                             |
| `pnpm api:test` | The API's tests (xUnit, SQLite in memory)                    |
| `pnpm check`    | `svelte-check` under TypeScript strict                       |
| `pnpm lint`     | Prettier + ESLint                                            |
| `pnpm build`    | Precompressed static output in `apps/web/build`              |
| `pnpm budget`   | The entry route against the 150 kB brotli budget, after a build |

---

## How it is built

Prosper's shape, because the two are siblings and share a VPS:

```
apps/web/            SvelteKit + TypeScript PWA, adapter-static, no component library
  src/lib/styles/    tokens.css (the only place a colour exists) and app.css (the primitives)
  src/lib/ui/        hand-rolled components: Icon, TabBar, AppBar, Toaster, theme, nav
  src/lib/api/       the API client and the device token
  src/routes/        / · /pridat · /sin-slavy · /nastaveni · /nastaveni/vzhled · /nastaveni/parovani · /styleguide
apps/api/            ASP.NET Core 10 minimal API
  src/Aspire.Api/            endpoints, pairing auth, rate limits
  src/Aspire.Domain/         entities, no EF
  src/Aspire.Infrastructure/ EF Core + Npgsql, migrations
  tests/Aspire.Api.Tests/    xUnit
packages/contracts/  the wire types, shared by both sides
deploy/              compose, both nginx configs, the host vhost, the nightly backup
docs/                the runbook, the decisions, the brand
```

Auth is Prosper's: a pairing code typed once, a device-bound token stored as a
hash. No accounts, no passwords. The UI is Czech; code, comments, commits and
docs are English.

---

## Deploy

Three containers behind one loopback port, on the same VPS as Prosper:

```bash
cd deploy && cp .env.example .env && docker compose up -d --build
```

**[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)** is the runbook: the VPS, the
nginx vhost for `aspire.petrbohac.eu`, the certificate, the media volume, and
the backup.

---

## Add a migration

The schema lives in EF Core migrations under `apps/api/src/Aspire.Infrastructure/Migrations`.
`dotnet-ef` is pinned per repository in `apps/api/.config/dotnet-tools.json`,
so nothing global is needed:

```bash
cd apps/api && dotnet tool restore
```

```bash
cd apps/api && dotnet ef migrations add NameOfTheChange --project src/Aspire.Infrastructure
```

Migrations are generated for Postgres whatever environment the command runs
in (`DesignTimeDbContextFactory`), and the API applies them on startup. The
SQLite laptop mode has no migrations: it creates the schema from the model
directly. CI fails when the model and the last migration disagree, so a
model change without a migration cannot reach the VPS.

To roll the last one back before it has been committed:

```bash
cd apps/api && dotnet ef migrations remove --project src/Aspire.Infrastructure
```

---

## Documentation

| Document                                     | What it is                                                     |
| -------------------------------------------- | -------------------------------------------------------------- |
| [`PLAN.md`](PLAN.md)                         | The plan: vision, features, data model, milestones, open questions |
| [`docs/DECISIONS.md`](docs/DECISIONS.md)     | Every answered question and every deviation from the plan      |
| [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)   | Running it on the VPS                                          |
| [`apps/web/DESIGN.md`](apps/web/DESIGN.md)   | The design system: tokens, components, the two themes          |
| [`apps/web/PRODUCT.md`](apps/web/PRODUCT.md) | Product truth, for design work                                 |
| [`CLAUDE.md`](CLAUDE.md)                     | Working rules for Claude Code                                  |
