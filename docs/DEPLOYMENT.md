# Deployment — running Aspire on the VPS

**Revised:** 2026-09-11
**Audience:** whoever is holding the SSH key

Prosper's runbook, applied to a second app on the same box. Where the two
differ it is because Aspire holds photographs: the server is the **first**
copy here, not a second one, and there is a media volume beside the database.

---

## What runs where

```
                            aspire.petrbohac.eu
                                     │  443, TLS
┌────────────────────────────────────▼─────────────────────────────────────┐
│ VPS                                                                      │
│                                                                          │
│   nginx (host)  ── certbot renews the certificate                        │
│        │ proxy_pass http://127.0.0.1:8081        (Prosper is on 8080)    │
│        ▼                                                                 │
│   ┌────────────────── docker compose, deploy/ ───────────────────────┐   │
│   │                                                                  │   │
│   │   web    nginx + the static PWA          :80  ← the only         │   │
│   │    │     location /api/  ─────────┐           published port,    │   │
│   │    │     location /media/ ◄──┐    │           bound to loopback  │   │
│   │    ▼                        │    ▼                              │   │
│   │   /usr/share/nginx/html     │   api  ASP.NET Core 10  :8080      │   │
│   │   (200.html, _app/…)        │    │   not published               │   │
│   │                             │    ├──► /data/media  volume: media │   │
│   │                             └────┘    ▼                          │   │
│   │                                      db   Postgres 16            │   │
│   │                                           volume: pgdata         │   │
│   └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│   cron 03:23 ── deploy/backup.sh ── pg_dump + tar media → /var/backups   │
└──────────────────────────────────────────────────────────────────────────┘
```

**One origin, on purpose.** The client, the API and the photographs answer on
the same domain: the container's nginx proxies `/api/` to the API and serves
`/media/` straight from the volume. No CORS, one certificate, one port.

**Two apps on one box.** Prosper's compose project is `prosper` on `8080`;
this one is `aspire` on `8081`. They share nothing but the host's nginx, which
has one vhost per domain.

---

## What you need before you start

The box already has it all if Prosper runs there: Docker with the compose
plugin, nginx, certbot. What is new is one `A` record, `aspire.petrbohac.eu`,
pointed at the VPS.

Disk is the one thing to check. PLAN.md §7 caps uploads at 10 MB an image
and 500 MB in total; the resized copies are a fraction of that. Look before
the first upload:

```bash
df -h /var/lib/docker
```

---

## The deployment, start to finish

### 1. Get the repository onto the box

The repository is a laptop with no remote until somebody gives it one. A bare
repository on the VPS itself is the smallest thing that works: nothing leaves
hardware you own, and the `git pull` under **[Updating](#updating)** keeps
working exactly as written. On the box:

```bash
sudo git init --bare /opt/aspire.git
```

On the laptop, once:

```bash
git remote add origin ssh://root@aspire.petrbohac.eu/opt/aspire.git
```

```bash
git push -u origin master
```

Then, back on the box, the working copy the deployment runs from:

```bash
sudo git clone /opt/aspire.git /opt/aspire
```

A remote somewhere else — a private repository on a host you already use —
works the same way from step 2 on, and is also what lets
`.github/workflows/ci.yml` actually run; it has never had a remote to run on.

### 2. Write the two secrets

```bash
cd /opt/aspire/deploy && cp .env.example .env
```

Then fill in `.env`. Neither value has a default and compose refuses to start
without them:

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
```

```bash
shuf --random-source=/dev/urandom -i 100000000000-999999999999 -n 1   # PAIRING_CODE
```

`POSTGRES_PASSWORD` is read once, when the volume is first created. Changing
it later does not change the password the database already has.

`PAIRING_CODE` is what a device types once, in Nastavení → Párování, to be
handed a token. Digits, twelve of them, leading digit never zero. It is the
only thing between somebody and the board, so treat it as a password even
though it does not look like one. It seeds the first board on the first
start and is ignored after that; "Boards" below has the commands that
change a code or add a board.

### 2b. The notification keys, if you want the morning nudge

The morning nudge (PLAN.md §3.6) needs a VAPID key pair. Without one the API
runs perfectly well, the worker says so once at start, and the Upozornění
screen tells the person the server cannot send — so this step is optional
and can be done later.

The command makes a pair and touches nothing else, so it works before the
database exists:

```bash
cd /opt/aspire/deploy && docker compose run --rm api dotnet Aspire.Api.dll vapid
```

It prints three lines. Put them in `.env`, set `Push__Subject` to an address
a push service can reach you at — it is sent with every notification and some
services will use it if something is wrong — and then **recreate** the API,
not restart it:

```bash
cd /opt/aspire/deploy && docker compose up -d api
```

`docker compose restart api` will not do. A restart starts the same container
again, and a container keeps the environment it was created with, so the keys
in `.env` would never reach it — the screen would go on saying the server
cannot send and nothing would say why. `up -d` notices the changed
environment and makes a new container.

The API says which it is at start: one line about notifications being off
when there is no pair, and nothing at all when there is one.

```bash
cd /opt/aspire/deploy && docker compose logs api | grep -i "push notifications"
```

**Generate this once.** The public half is baked into every subscription
every browser has already made, so a new pair silently stops every
notification anybody has turned on; they have to turn them on again. The
command warns if the server already has one.

Notifications also need HTTPS, which step 5 gives you, and on an iPhone the
app has to be added to the home screen first — Safari will not offer
notifications to a tab (PLAN.md §7).

**The app tells you whether this step is done.** Nastavení · Upozornění
shows the switch when the server has a pair and this browser can be asked,
and one sentence saying why when it cannot — „Server zatím upozornění
posílat neumí“ is this step (D41).

### 3. Start the three containers

```bash
cd /opt/aspire/deploy && docker compose up -d --build
```

The first build takes a few minutes. After that:

```bash
curl -s localhost:8081/api/v1/health
```

`{"ok":true,"version":"…"}` means all three are up and the web container is
proxying to the API. The database migrated itself while that was happening;
**[The database](#the-database)** below is what happened and how to look at it.

### 4. Give the domain to nginx

```bash
sudo cp /opt/aspire/deploy/nginx/aspire.conf.example /etc/nginx/sites-available/aspire.conf
```

```bash
sudo ln -s /etc/nginx/sites-available/aspire.conf /etc/nginx/sites-enabled/ && sudo nginx -t && sudo systemctl reload nginx
```

### 5. The certificate

```bash
sudo certbot --nginx -d aspire.petrbohac.eu
```

Certbot edits the vhost in place and installs the renewal timer. Until the
certificate is there the app runs but **does not install**: a service worker
will not register over plain HTTP.

### 6. The backup

```bash
sudo crontab -e
```

```
23 3 * * *  /opt/aspire/deploy/backup.sh >> /var/log/aspire-backup.log 2>&1
```

Then run it once by hand, now, and restore from it once into a throwaway. The
commands are at the bottom of `backup.sh`. Unlike Prosper's, this backup is
the **only** other copy of the photographs, and the homelab flow that pulls
`/var/backups` to the NAS is what makes it two.

---

## The database

There is no `createdb` and no `dotnet ef database update` to run. Step 3 is
the whole of it:

1. **`db` starts first.** `api` waits on its `pg_isready` healthcheck.
2. **Postgres creates the role and the database** from the compose
   environment, on the first start of an empty `pgdata` volume, and never
   again.
3. **The API migrates.** On startup it calls `MigrateAsync()`, gated behind
   `Database:MigrateOnStart` (default `true`), which walks the migrations in
   `apps/api/src/Aspire.Infrastructure/Migrations` and applies what is
   missing. A fresh database gets `dreams`, `devices` and the EF history
   table.

Tables and columns are `snake_case`, so `psql` reads the way it looks:

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c '\dt'
```

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select id, board_id, name, paired_at, last_seen_at from devices;'
```

**Adding a migration** is a developer step, on a laptop, before the code
reaches the box: `README.md` has the command. CI refuses a model that
disagrees with its last migration, so a rebuild on the VPS never meets a
schema it cannot migrate.

### Starting over

Destructive in a way nothing else here is — the volumes *are* the board:

```bash
cd /opt/aspire/deploy && docker compose down -v
```

`-v` drops `pgdata` **and `media`**: every dream, every photograph, every
paired device. The next `up` starts from nothing. Restore from the backup
first if that is not what you meant.

---

## The photographs

`/data/media` in the API container is the `media` volume; the same volume is
mounted read-only into the web container at `/usr/share/nginx/media`, where
nginx serves it as `/media/` with a year of cache. The API writes into it
one directory per dream, one per photograph inside, and three WebP sizes
inside that, `{dreamId}/{imageId}/{thumb|screen|full}.webp` (D23); an
upload waits in the container's own `/tmp` until the worker has resized
it, so a restart mid-upload leaves nothing behind but a row the next start
drops. The directory is created in the API image owned by the app user,
which is what lets the non-root API write into a volume Docker would
otherwise create as root.

```bash
cd /opt/aspire/deploy && docker compose exec api ls -la /data/media
```

---

## Pairing a device

The shape is Prosper's, without the address: open the site, add it to the
home screen, Nastavení → Párování, type the board's code, name the device,
*Spárovat*. The phone keeps its token in `localStorage`; the server stores
only the token's hash, so a database dump does not hand anybody a working
phone. *Odpojit* on the same screen forgets the token on the phone and
nothing else; to revoke a device, delete its row (see above).

---

## Boards

A board is a tenant: its own dreams, its own devices, nothing shared with
another board, and still no account and no login (D21). A device belongs
to the board whose code it typed, for as long as it keeps the token.

The first board, *Nástěnka*, takes `PAIRING_CODE` on the first start. From
then on every code lives in the database, hashed, and the variable is
ignored. The API's own image carries the commands; each one runs against
the database and exits:

```bash
cd /opt/aspire/deploy && docker compose exec api dotnet Aspire.Api.dll board list
```

```bash
cd /opt/aspire/deploy && docker compose exec api dotnet Aspire.Api.dll board add Zuzana 483920174635
```

```bash
cd /opt/aspire/deploy && docker compose exec api dotnet Aspire.Api.dll board code Nástěnka 209384756123
```

A code is digits, twelve of them as in step 2. Changing a board's code does
not touch the devices already paired into it. Deleting a board's row in
`psql` deletes its dreams and its devices with it; there is no command for
that, on purpose.

---

## Updating

```bash
cd /opt/aspire && sudo git pull && cd deploy && docker compose build --pull && docker compose up -d
```

`--pull` fetches the base images' security patches; without it `build`
reuses whatever it pulled the first time, forever. Postgres and the media
volume are untouched by a rebuild. A migration in the new code runs on the
API's first start.

The phone picks up a new client by itself: the app asks for a new service
worker on every resume and reconnect, announces it with *Obnovit*, and
reloads on the next navigation.

---

## Troubleshooting

**`curl localhost:8081/api/v1/health` hangs or 502s.**

```bash
cd /opt/aspire/deploy && docker compose ps && docker compose logs --tail=50 api
```

The usual cause is Postgres refusing the password, which means `.env` changed
after the volume was created. The password lives in the volume, not the file.
The other one is a migration that failed: the log names it.

**Every request 502s after a rebuild.** nginx resolved the API's address once
and kept it; `app.conf` goes through a variable and Docker's resolver to avoid
exactly this. `docker compose restart web` proves it.

**Pairing 429s the wrong person.** Both fences in front of `/api/v1/pair`
count per client address, and the address is only the client's because of the
`real_ip` block in `app.conf`. `docker compose logs --tail=20 web` should show
public addresses, not `172.`.

**No install prompt on the phone.** The service worker only registers over
`https://`. Check the certificate first.

**A photograph uploads and then disappears.** The dream saves, the tile shows
the sky, and nothing on the screen says why. The media volume belongs to
somebody other than the app user, so the resize fails and the row goes with it
(D26). The API refuses to start on this now, naming the root; older
deployments fail one photograph at a time instead. What it looks like:

```bash
cd /opt/aspire/deploy && docker compose logs api --tail=400 | grep -A4 "could not be processed"
```

```bash
cd /opt/aspire/deploy && docker compose exec api sh -c 'id; ls -ld /data/media'
```

`drwxr-xr-x root root` against `uid=1654(app)` is the fault. Repair the volume
once, and `media-init` keeps it that way from then on:

```bash
cd /opt/aspire/deploy && docker compose exec -u root api chown -R 1654:1654 /data/media && docker compose restart api
```

The photographs already lost are lost: their rows were deleted, so those
dreams need the picture picked again.

---

## What this deliberately does not do

- **No account, no password, no e-mail.** One person, their own devices, and
  a code typed once.
- **No expiry on the token.** Revocation is deleting the row.
- **No object storage.** The photographs are on the VPS disk in a named
  volume, backed up nightly. MinIO or S3 is a later decision if the disk
  fills (PLAN.md §8).
- **No monitoring.** If the board is down, the phone says so.
