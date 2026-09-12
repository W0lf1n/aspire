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
| `POST`   | `/api/v1/dreams/{id}/focus` | Put it on Teď, last; the dream out. 409 when Teď is full or the dream is achieved (D53) |
| `DELETE` | `/api/v1/dreams/{id}/focus` | Take it off Teď; 204, and 204 again when it was not on it |
| `PUT`    | `/api/v1/focus` | `{ dreamIds }` — the whole of Teď, in this order and nothing else on it; the ten out |
| `POST`   | `/api/v1/dreams/{id}/images` | Multipart `file`, `?kind=dreamt\|achieved`, optional `?focusX&focusY&zoom`; 202 with the image, `ready` once resized |
| `PUT`    | `/api/v1/dreams/{id}/images/{imageId}` | `{ focusX, focusY, zoom }` — where the photograph is looked at; no file is touched (D54) |
| `DELETE` | `/api/v1/dreams/{id}/images/{imageId}` | 204                                |
| `GET`    | `/api/v1/nudge/key` | The VAPID public key, or empty when the server has no pair. No auth |
| `GET`    | `/api/v1/nudge` | `?endpoint=`; this device's `{ mode, atMinutes }` |
| `PUT`    | `/api/v1/nudge` | `NudgeInput` in; `off` deletes the subscription. 503 with no key pair |
| `POST`   | `/api/v1/nudge/offset` | `{ endpoint, utcOffsetMinutes }`; moves the offset on a subscription that exists, makes none, 204. Sent on every open and resume (D51) |
| `DELETE` | `/api/v1/nudge` | `?endpoint=`; 204 |
| `GET`    | `/api/v1/images/fetch` | `?url=`; the picture behind a link as a JPEG at most 2048 px, made on request and kept nowhere. Public addresses only, three redirects, 10 s, 10 MB; 20 a minute per address (D56) |
| `GET`    | `/api/v1/wallpaper` | `?dreams=<id,…>&width&height&offset`; the lock-screen collage as JPEG, made on request and kept nowhere (D33). With no `dreams` it is today's six, and it stamps nothing (D59) |
| `GET`    | `/api/v1/board/link` | This board's lock-screen link as `{ path }`, or `{ path: null }` (D60) |
| `POST`   | `/api/v1/board/link` | A new key, which also stops the old link opening anything; `{ path }` |
| `DELETE` | `/api/v1/board/link` | No link at all; 204, and 204 again when there was none |
| `GET`    | `/api/v1/dreams/{id}/link` | This dream's share link as `{ path }`, or `{ path: null }` (D61) |
| `POST`   | `/api/v1/dreams/{id}/link` | A new key, which also stops the old link opening anything; `{ path }` |
| `DELETE` | `/api/v1/dreams/{id}/link` | Unshared; 204, and 204 again when it was not shared |
| `GET`    | `/s/{key}` | **HTML, and no token** — one dream's page: its photograph, its name, its affirmation (D61). `noindex`, `no-store`, 404 for a key that opens nothing. Outside `/api/`, because a person reads this URL off a screen |
| `GET`    | `/s/{key}/card.jpg` | The 1200×630 preview a chat app draws, as JPEG — the files on disk are WebP, which some of those apps will not render in a card |
| `GET`    | `/api/v1/w/{key}` | Today's six as a JPEG, **with no token** — the key in the path is the whole permission (D60). `?width&height&offset`; `no-store`; 404 for a key that opens nothing; 10 a minute per address |

Everything but `health`, `pair`, `w/{key}` and `s/{key}` needs
`Authorization: Bearer <token>`.
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

The API will not let that be a surprise. Before serving anything in SQLite
mode it asks every table for one row, which makes SQLite name every column the
model expects; a file that is behind stops the start with one line naming the
file and the fix, rather than answering 500 to the first request for a dream
(D55).

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

`invite` is the one to reach for. It makes the board and its code in the same
breath — twelve digits from the operating system's randomness, never a
person's — and prints the code once, because what is kept is the hash (D46):

```bash
dotnet run --project src/Aspire.Api -- board invite Zuzana
```

`add` is the same thing with a code you brought yourself:

```bash
dotnet run --project src/Aspire.Api -- board add Zuzana 483920174635
```

```bash
dotnet run --project src/Aspire.Api -- board code Nástěnka 209384756123
```

The morning nudge's key pair has its own command, which touches neither the
database nor the disk — it runs before the migration, because the moment you
need it is while setting a box up:

```bash
dotnet run --project src/Aspire.Api -- vapid
```

It prints `Push__PublicKey`, `Push__PrivateKey` and `Push__Subject` for the
environment. Generate a pair **once**: the public half is in every
subscription every browser has already made, so a new one silently stops
all of them (D34). Run a second time it warns before it prints.

On the VPS `board` runs as `docker compose exec api dotnet Aspire.Api.dll
board …` and `vapid` as `docker compose run --rm -T api vapid`, since it may
be needed before anything is up (`docs/DEPLOYMENT.md`). The two spellings
differ because `exec` runs a bare command in a container that is already up,
while `run` appends what you type to the image's entrypoint — which is
already `dotnet Aspire.Api.dll`, so naming it again hands the app arguments
it does not recognise and it starts a web server instead. A code is digits only, six
at least, twelve in production.

### The laptop's key pair goes in user secrets, not in git

`appsettings.Development.json` is in the repository and should stay readable
— the pairing code in it is `000000` on purpose. The VAPID private key is not
that kind of value, so on a laptop it lives in the .NET user secrets store
(`UserSecretsId` in `Aspire.Api.csproj`, the file itself outside the
checkout). Production reads the same three settings from the environment and
never sees it.

```bash
cd apps/api && dotnet run --project src/Aspire.Api -- vapid
```

```bash
cd apps/api && dotnet user-secrets set "Push:PublicKey" "<the public half>" --project src/Aspire.Api
```

```bash
cd apps/api && dotnet user-secrets set "Push:PrivateKey" "<the private half>" --project src/Aspire.Api
```

```bash
cd apps/api && dotnet user-secrets set "Push:Subject" "mailto:you@example.com" --project src/Aspire.Api
```

`dotnet user-secrets list` says what is set; `dotnet user-secrets clear` puts
the laptop back to a server that does not do notifications, which is the
state the Upozornění screen has a sentence for. With a pair in place `pnpm
api` starts the nudge worker — it is off, and says so once, when there is
none.

A laptop's pair is its own. It is not the VPS's, and copying one to the other
is the mistake D34 warns about from the other direction: whichever server a
browser subscribed through is the only one whose key can reach it again.

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

## Fetching a picture from a link

`/api/v1/images/fetch` is the one endpoint that makes this server open a
connection somewhere else, so it is also the one with a fence around it
(D56). `Net/PrivateAddress.cs` decides which addresses are on the public
internet; `Net/ImageFetcher.cs` does the rest — scheme, port, three redirects
walked by hand so every hop is checked, ten seconds, ten megabytes, and a
content type that is an image or a page.

The check lives in the client's `ConnectCallback` in `Program.cs`, not before
the request: a name checked and then resolved again is a name that can answer
differently the second time. The socket is opened to an address that passed
and to no other.

Every failure is the same Czech sentence on purpose. A message naming the
address or the status would make this a way to ask the internet questions
from inside the VPS and read the answers.

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
