# Aspire API

ASP.NET Core 10 minimal API + EF Core 10 + Postgres 16. Prosper's server
shape, with a domain: three projects rather than one, because this server
will resize images and pick the day's dream, and Prosper's never had to do
more than store rows.

---

## Endpoints

| Method | Path               | What                                            |
| ------ | ------------------ | ----------------------------------------------- |
| `GET`  | `/api/v1/health`   | `{ ok, version }`. No auth                      |
| `POST` | `/api/v1/pair`     | Code in, `{ deviceId, token }` out. Rate-limited |
| `GET`    | `/api/v1/dreams`            | The device's board, in board order            |
| `POST`   | `/api/v1/dreams`            | `DreamInput` in, the dream out, 201           |
| `GET`    | `/api/v1/dreams/{id}`       | One dream; 404 when it is not on this board   |
| `PUT`    | `/api/v1/dreams/{id}`       | `DreamInput` in, the dream out                |
| `DELETE` | `/api/v1/dreams/{id}`       | 204                                           |
| `POST`   | `/api/v1/dreams/{id}/likes` | One more on the heart; the dream out          |
| `POST`   | `/api/v1/dreams/{id}/shown` | The board opened on it today; 204             |
| `POST`   | `/api/v1/dreams/{id}/images` | Multipart `file`; 202 with the image, `ready` once resized |
| `DELETE` | `/api/v1/dreams/{id}/images/{imageId}` | 204                                |

Everything but `health` and `pair` needs `Authorization: Bearer <token>`.
The wire types live in `packages/contracts` and are mirrored in
`Contracts.cs`; enums travel kebab-case (`in-progress`). A bad input is a
400 problem whose `detail` is the Czech sentence the screen shows.

---

## Run it

### Locally, without Docker

```bash
dotnet run --project src/Aspire.Api
```

`Properties/launchSettings.json` starts it in Development on
`http://127.0.0.1:5300`, where `appsettings.Development.json` picks SQLite
(`aspire.db` beside the project), a `media/` folder for the photographs, and
the pairing code `000000`. Both files are git-ignored.

### In production

One of three containers in `deploy/docker-compose.yml`, behind the same
nginx that serves the client. `docs/DEPLOYMENT.md` is the runbook.

---

## The database

Postgres in production, SQLite on a laptop, the same EF model either way.

**Migrations are Postgres-only.** On startup the API calls `MigrateAsync()`
when the provider is Postgres and `EnsureCreatedAsync()` when it is SQLite,
both gated behind `Database:MigrateOnStart` (default `true`). The SQLite side
therefore never runs a migration; it creates the schema as the model stands,
which is all a laptop needs. It also never updates it: after a model change,
delete `aspire.db` and start the API again.

`dotnet-ef` is pinned per repository:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add NameOfTheChange --project src/Aspire.Infrastructure
```

`DesignTimeDbContextFactory` hands the tool an Npgsql context whatever
environment it runs in, so a migration is never accidentally generated for
SQLite. The connection string it names is never opened.

```bash
dotnet ef migrations has-pending-model-changes --project src/Aspire.Infrastructure
```

is what CI runs: it exits non-zero when the model and the last migration
disagree.

Tables and columns are `snake_case` (`AppDbContext.OnModelCreating` renames
every column), so `psql` reads the way the plan is written.

---

## Boards

A board is the tenant (D21): a name, a pairing code stored as a PBKDF2
hash, and every device and dream that belongs to it. `Pairing:Code` seeds
the first one, *Nástěnka*, on the first start and is ignored afterwards.
The rest is the operator's, through the API's own binary:

```bash
dotnet run --project src/Aspire.Api -- board list
```

```bash
dotnet run --project src/Aspire.Api -- board add Zuzana 483920174635
```

```bash
dotnet run --project src/Aspire.Api -- board code Nástěnka 209384756123
```

On the VPS the same three run as `docker compose exec api dotnet
Aspire.Api.dll board …` (`docs/DEPLOYMENT.md`). A code is digits only, six
at least, twelve in production.

---

## The photographs

An upload lands in the system's temp directory, gets a `dream_images` row,
and goes into a channel; one worker (`ImageWorker`) reads it, applies the
orientation, strips EXIF, XMP and IPTC, and writes three WebP files under
`Media:Root` as `{dreamId}/{imageId}/{thumb|screen|full}.webp` at 400,
1280 and 2048 px on the longest edge (D23). The row's `processed_at` says
when; until then the wire says `ready: false` and the board shows the sky.
On a laptop the root is `media/` beside the project and the API serves it
as `/media/`; on the VPS it is the volume and nginx serves it.

---

## Tests

```bash
dotnet test
```

82 tests. The ones that reach the database use SQLite in memory rather than
the EF in-memory provider: this code relies on a unique index, and the
in-memory provider does not honour one. They cover pairing, token hashing,
name trimming, and which address the pairing limiter counts a request
against.

---

## Security notes

Prosper's, unchanged:

- **Tokens are stored as SHA-256 hashes.** A database dump does not hand
  anybody a working device.
- **Tokens do not expire.** Revocation is deleting the row.
- **The pairing code is compared in constant time**, and stored as a PBKDF2
  hash, one per board. A dump of the table is not a list of codes.
- **`/api/v1/pair` is rate-limited per client address and as a whole.**
  `ClientAddress` decides what "client address" means: `X-Real-IP` from a
  private-range peer, never `X-Forwarded-For`, which the caller can prepend
  to. The same fences are repeated in `deploy/nginx/app.conf`.
- **An unset pairing code refuses to pair.**
