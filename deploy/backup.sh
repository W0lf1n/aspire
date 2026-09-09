#!/bin/bash
# Nightly dump of the database and the media volume — PLAN.md §4.
#
#     sudo crontab -e
#     23 3 * * *  /srv/aspire/deploy/backup.sh >> /var/log/aspire-backup.log 2>&1
#
# Unlike Prosper, the server here is the **first** copy: the photographs live
# on the VPS and nowhere else once the phone has uploaded them. Losing the box
# is losing the board. So this dumps both halves — Postgres for the rows,
# the media volume for the images — and the existing homelab flow pulls
# `/var/backups/aspire` to the NAS.
#
# It is deliberately dull: pg_dump, tar, gzip, delete what is older than
# KEEP_DAYS. No incremental anything.

# `pipefail` is why this is bash and not sh: without it `pg_dump | gzip`
# exits on gzip's status, and gzip succeeds at compressing an error.
set -euo pipefail

# The dump is the whole board in plain text and the images beside it.
# Every file this writes is created 0600 and the directory 0700.
umask 077

# Where this script lives, so cron does not need a working directory.
DEPLOY_DIR="$(cd "$(dirname "$0")" && pwd)"

DEST="${ASPIRE_BACKUP_DIR:-/var/backups/aspire}"
KEEP_DAYS="${ASPIRE_BACKUP_KEEP_DAYS:-30}"

STAMP="$(date +%Y-%m-%d)"
DB_FILE="$DEST/aspire-db-$STAMP.sql.gz"
MEDIA_FILE="$DEST/aspire-media-$STAMP.tar.gz"

mkdir -p "$DEST"
# A directory that already existed was created under the old umask.
chmod 700 "$DEST"

compose() {
	docker compose -f "$DEPLOY_DIR/docker-compose.yml" --project-directory "$DEPLOY_DIR" "$@"
}

# ── the rows ─────────────────────────────────────────────────────────────────
# `-T` because cron has no TTY and `exec` allocates one by default — which is
# how this works by hand and fails at 03:23 every night.
compose exec -T db pg_dump --username aspire --clean --if-exists aspire |
	gzip -9 >"$DB_FILE.partial"

# Renamed only once it is whole. A truncated dump that looks like a backup is
# worse than no backup, because it is the one you reach for.
mv "$DB_FILE.partial" "$DB_FILE"

# An empty dump is a failure that exited 0 — an unreadable database still
# produces a valid, tiny gzip stream.
SIZE="$(wc -c <"$DB_FILE")"
if [ "$SIZE" -lt 1024 ]; then
	echo "$(date -Iseconds) FAILED: $DB_FILE is $SIZE bytes" >&2
	exit 1
fi

# ── the photographs ──────────────────────────────────────────────────────────
# The volume is read through a throwaway container rather than by guessing
# where Docker keeps it on the host. WebP does not compress further; the gzip
# is for the tar's own overhead and costs nothing.
compose run --rm -T --no-deps --entrypoint tar api -C /data/media -cf - . |
	gzip -1 >"$MEDIA_FILE.partial"
mv "$MEDIA_FILE.partial" "$MEDIA_FILE"

find "$DEST" -name 'aspire-*.gz' -mtime "+$KEEP_DAYS" -delete

echo "$(date -Iseconds) ok: $DB_FILE ($SIZE bytes), $MEDIA_FILE ($(wc -c <"$MEDIA_FILE") bytes)"

# Restoring, for when it is needed and nobody remembers:
#
#     gunzip -c /var/backups/aspire/aspire-db-2026-09-09.sql.gz \
#       | docker compose exec -T db psql --username aspire aspire
#     gunzip -c /var/backups/aspire/aspire-media-2026-09-09.tar.gz \
#       | docker compose run --rm -T --no-deps --entrypoint tar api -C /data/media -xf -
#
# Do it once, on purpose, before you need it. A backup nobody has restored from
# is a hypothesis.
