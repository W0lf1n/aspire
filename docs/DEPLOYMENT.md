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

A remote somewhere else works the same way from step 2 on, and is what CI
and the Deploy button are: the repository is on GitHub as
`W0lf1n/aspire`, `.github/workflows/ci.yml` runs there on every push, and
**[The Deploy button](#the-deploy-button)** below is the same deployment as
**[Updating](#updating)**, pressed from a phone. A box set up against the
bare repository needs one line changed to use it; that section says which.

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
cd /opt/aspire/deploy && docker compose run --rm -T api vapid
```

`vapid` on its own, and nothing before it: the image's entrypoint is already
`dotnet Aspire.Api.dll`, and `compose run` appends what you type to it. Repeat
the entrypoint and the app gets three arguments it does not recognise, ignores
the command, and **starts a web server instead** — which looks like a hang.
`-T` is for the pipe below; without a terminal it would otherwise wrap the
output.

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

## The lock screen that refreshes itself

Nastavení → Tapeta → *Každé ráno sama* makes a link, and the link is its own
permission (D60): whoever holds it gets one collage of that board's six
dreams for the day, and nothing else — no dreams to read, no writes, no
token. It is the only thing in this app besides `pair` that answers without
`Authorization`.

What that means for the box:

- **The key is in the path**, so `location ^~ /api/v1/w/` in
  `deploy/nginx/app.conf` turns `access_log` off for it. A capability written
  into a log file is a capability handed to whoever reads, rotates or copies
  that file. If you add logging in front of nginx — Cloudflare, a reverse
  proxy on another host — it needs the same treatment or the link is in
  somebody else's log.
- **It renders on every hit**: six full-size photographs decoded, cropped and
  drawn. Ten a minute per address in the API and the same in nginx.
- **Revoking is one tap.** *Nový* replaces the key, *Zrušit* removes it; the
  old link 404s immediately either way. There is nothing to clean up.

To see whether a board has one, and to take it away from the box rather than
from the phone:

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select id, name, link_key is not null as has_link from boards;'
```

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c "update boards set link_key = null where name = 'petr';"
```

The phone's half is Zkratky → Automatizace → Denně v 6:55 → *Získat obsah
URL* → *Nastavit tapetu* (zamčená obrazovka, bez náhledu), with *Zeptat se
před spuštěním* off. The wallpaper has to be a plain photo wallpaper rather
than a shuffle, or the Shortcut has nothing to overwrite. The screen says all
of this in Czech; it is here because it is the half that is not in the app.

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

### Somebody wants to try it

From the laptop, one command — the whole of handing a stranger their own
board (D46):

```bash
scripts/invite.sh Zuzana
```

It opens one SSH session, runs `board invite` in the API's container, and
prints the board's twelve digits in **your** terminal. The API makes the code
itself, from the operating system's randomness rather than from whatever a
person would type twice, and prints it **once**: what is stored is the PBKDF2
hash, so there is no command that reads it back. Lost means `board code` with
a new one, not a lookup.

The script says what to send with it. What they get is a tenant: their own
dreams, their own photographs, their own devices, and nothing of yours — a
board is the whole of what a device can see (D21). Their own „Vše", their own
daily pick, their own Síň slávy.

This is not a GitHub Action on purpose. A workflow that prints a code leaves
it in a run log for ninety days, readable by anybody who can read the
repository, and a board's code is the only thing standing between a stranger
and somebody's dreams.

Over SSH by hand it is the same command the script runs:

```bash
cd /opt/aspire/deploy && docker compose exec api dotnet Aspire.Api.dll board invite Zuzana
```

---

## Updating

One command, on the box or from GitHub's Deploy button — the same command
either way, because the button runs this script over SSH and does nothing
else (D45):

```bash
/opt/aspire/deploy/deploy.sh
```

It fetches, fast-forwards, rebuilds with `--pull`, swaps the containers and
then **waits for the app to answer** before calling it a deployment; one that
does not answer prints the API's last fifty lines and exits non-zero. It
refuses to run over a working copy with uncommitted changes, which is the one
thing `git pull` would quietly destroy at two in the morning. A tag or a sha
deploys as well as a branch:

```bash
/opt/aspire/deploy/deploy.sh v1.2.0
```

`--pull` fetches the base images' security patches; without it `build`
reuses whatever it pulled the first time, forever. Postgres and the media
volume are untouched by a rebuild. A migration in the new code runs on the
API's first start.

**Going back** is the same script with the sha it printed last time. A build
that fails changes nothing at all — the old containers serve from the old
images until `up -d` swaps them — so the only deployment worth undoing is one
that built and then misbehaved.

The phone picks up a new client by itself: the app asks for a new service
worker on every resume and reconnect, announces it with *Obnovit*, and
reloads on the next navigation.

### The Deploy button

`.github/workflows/deploy.yml` is manual only: **Actions → Deploy → Run
workflow**, a ref (`master` unless you mean otherwise), and it opens an SSH
session and runs the script above. The run's summary page shows what the box
said.

It needs five things once, and then never again. The second is the longest
and has a section of its own.

**1. A key for the runner, without a passphrase.** Yours has one, which is
right for yours and impossible for a machine. Make a second one, on the
laptop:

```bash
ssh-keygen -t ed25519 -N '' -C 'github-actions aspire deploy' -f ~/.ssh/aspire-deploy
```

**2. A user for it, that can do nothing else.** Its own section, below:
**[A user that can only deploy](#a-user-that-can-only-deploy)**. Do that now
and come back; the rest of this is GitHub's side.

**3. The box pulls from GitHub.** The script fast-forwards from `origin`, so
`origin` has to be the repository the button deploys — not the bare
repository on the box, if that is what step 1 set up:

```bash
cd /opt/aspire && git remote -v
```

```bash
cd /opt/aspire && sudo git remote set-url origin https://github.com/W0lf1n/aspire.git
```

A **private** repository also has to let the box read it: add a second key
pair as a *deploy key* on GitHub (Settings → Deploy keys, read-only), keep
its private half on the box, and use the SSH remote
`git@github.com:W0lf1n/aspire.git` rather than `https://`. A public
repository needs neither.

**4. The four secrets**, in Settings → Secrets and variables → Actions:

| Secret            | What                                                               |
| ----------------- | ------------------------------------------------------------------ |
| `VPS_HOST`        | `aspire.petrbohac.eu`                                              |
| `VPS_USER`        | `aspire-deploy`, the user made below                               |
| `VPS_SSH_KEY`     | the **private** half of step 1, the whole file, both `-----` lines |
| `VPS_KNOWN_HOSTS` | the box's host key, so the runner knows what it is talking to      |

The last one comes from the laptop, where the box is already known:

```bash
ssh-keyscan -t ed25519 aspire.petrbohac.eu
```

Compare it against the line already in your own `~/.ssh/known_hosts` before
pasting it in — that comparison is the whole point of the secret. Keyscanning
inside the workflow on every run would instead trust whatever answered that
night, which is the attack a known_hosts file exists to stop.

The workflow sends one command — `aspire-deploy <ref>` — and nothing else,
because that is the only thing the key on the box is allowed to ask for. The
checkout path lives in the wrapper on the box rather than in a repository
variable.

**5. Press it once while watching.** The first run proves the key, the host
key, the remote and the path in one go, and failing any of them costs a
minute rather than a deployment.

The button does not check that CI passed — it deploys what you point it at.
What it deployed is in the run summary, and `git log -1` on the box says the
same thing afterwards.

### A user that can only deploy

The key in GitHub's secrets is a key to the box, and a key to the box is
worth exactly what it can be used for. This gives it a user of its own, and
then gives that user one thing to do (D47).

**What cannot be fenced off, said plainly.** The deployment writes
`/opt/aspire` and talks to the Docker socket, and socket access *is* root —
a member of the `docker` group can start a container with `/` mounted in it.
So the deployment runs as root, through one `sudo` rule, and what is fenced
is the key: no shell, no file, no port forwarding, no `scp`, and one command
whose only argument has to look like a ref. A leaked secret can then ask for
a deployment and nothing else.

It also means anybody who can push to `master` can run code as root on this
box, because the box builds what it fetches. That is true of every continuous
deployment; it is worth saying once. Keep push rights to this repository as
tight as the SSH key.

All of this is on the box, as root.

**1. The user.** No password, so nothing can log in as it but a key:

```bash
sudo useradd --create-home --shell /bin/bash --comment 'GitHub Actions deploy' aspire-deploy && sudo passwd --lock aspire-deploy
```

It is deliberately **not** in the `docker` group and owns nothing: the whole
of what it can do arrives in step 3.

**2. The one command it may run.** `deploy/aspire-deploy` in this repository
is the fence; it goes somewhere the deploy user cannot write:

```bash
sudo install -m 755 -o root -g root /opt/aspire/deploy/aspire-deploy /usr/local/bin/aspire-deploy
```

**3. The sudo rule.** One line, one script, no password — and `visudo -c`
rather than an editor, so a typo cannot lock the box's sudo:

```bash
printf 'aspire-deploy ALL=(root) NOPASSWD: /opt/aspire/deploy/deploy.sh, /opt/aspire/deploy/deploy.sh *\n' | sudo tee /etc/sudoers.d/aspire-deploy >/dev/null && sudo chmod 440 /etc/sudoers.d/aspire-deploy && sudo visudo -c
```

The script it names must not be writable by the user that may run it as root,
or the rule is a root shell with extra steps. `/opt/aspire` is root-owned
from `git clone`; this makes sure of it:

```bash
sudo chown -R root:root /opt/aspire && sudo find /opt/aspire -perm -o+w -not -type l
```

That `find` should print nothing at all.

**4. The key, pinned to the command.** Everything before the key type is the
fence: sshd runs `/usr/local/bin/aspire-deploy` whatever the client asks for,
and puts the ask in `SSH_ORIGINAL_COMMAND` for the wrapper to check.

On the laptop, in the same shell where the key was made:

```bash
printf 'command="/usr/local/bin/aspire-deploy",no-agent-forwarding,no-port-forwarding,no-pty,no-user-rc,no-X11-forwarding %s' "$(cat ~/.ssh/aspire-deploy.pub)" | ssh root@aspire.petrbohac.eu "install -d -m 700 -o aspire-deploy -g aspire-deploy /home/aspire-deploy/.ssh && cat >>/home/aspire-deploy/.ssh/authorized_keys && chown aspire-deploy:aspire-deploy /home/aspire-deploy/.ssh/authorized_keys && chmod 600 /home/aspire-deploy/.ssh/authorized_keys"
```

**5. Prove both halves.** The first says a deployment works; the second says
nothing else does. Both from the laptop:

```bash
ssh -i ~/.ssh/aspire-deploy -o IdentitiesOnly=yes -o BatchMode=yes aspire-deploy@aspire.petrbohac.eu "aspire-deploy 'master'"
```

```bash
ssh -i ~/.ssh/aspire-deploy -o IdentitiesOnly=yes -o BatchMode=yes aspire-deploy@aspire.petrbohac.eu "cat /etc/shadow"
```

The first prints the deployment. The second must print `This key may only
run: aspire-deploy <ref>` and exit 126 — and so must `ssh … aspire-deploy@…`
with no command at all, which is somebody asking for a shell.

`VPS_USER` in the four secrets above is then `aspire-deploy`, not `root`.

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

**The morning nudge arrived late.** Find out whether the server was late or
the phone was. A sent nudge stamps the dream it named, so the stamp is the
server's own record of when it went out:

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select title, last_shown_at from dreams order by last_shown_at desc nulls last limit 3;'
```

```bash
cd /opt/aspire/deploy && docker compose exec db psql -U aspire -d aspire -c 'select mode, at_minutes, utc_offset_minutes, last_sent_on from push_subscriptions;'
```

The stamp is UTC and `utc_offset_minutes` converts it: 120 in summer here, so
`05:00:02Z` is two minutes past seven and the server was on time. Then the
delay was the push service holding the message, which is what `Urgency: high`
is for (D51) — before that fix every nudge went out at `normal`, and one sat
in Apple's queue for 77 minutes. Since D51 each send also logs a line:

```bash
cd /opt/aspire/deploy && docker compose logs api --tail=400 | grep Nudged
```

A stamp *later* than the chosen hour means the worker itself was late, which
it is allowed to be for two hours (`NudgeSchedule.GraceMinutes`) — a deploy
over breakfast still sends, at the first tick after it comes back:

```bash
cd /opt/aspire/deploy && docker inspect -f '{{.State.StartedAt}}' $(docker compose ps -q api)
```

Nothing arriving at all, with a stamp that moved, is the phone: a Focus, a
scheduled summary, or notifications off for the installed app.

---

## What this deliberately does not do

- **No account, no password, no e-mail.** One person, their own devices, and
  a code typed once.
- **No expiry on the token.** Revocation is deleting the row.
- **No object storage.** The photographs are on the VPS disk in a named
  volume, backed up nightly. MinIO or S3 is a later decision if the disk
  fills (PLAN.md §8).
- **No monitoring.** If the board is down, the phone says so.
