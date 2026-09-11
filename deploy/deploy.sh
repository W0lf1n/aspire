#!/bin/bash
# One deployment, start to finish, run **on the VPS** — by hand over SSH or by
# the Deploy button in GitHub Actions, which does nothing but call this
# (`.github/workflows/deploy.yml`, D45).
#
#     /opt/aspire/deploy/deploy.sh            # master
#     /opt/aspire/deploy/deploy.sh v1.2.0     # a tag, or any commit
#
# It is `DEPLOYMENT.md`'s Updating section with the two things a person does
# by eye added: it refuses to throw away work that is only on the box, and it
# does not call a deployment finished until the app has answered.
#
# **A failed build changes nothing.** `build` writes new images and the old
# containers go on serving from the old ones until `up -d` swaps them, so the
# app that is running is never the app that is half-built. Going back is this
# script again with the previous commit, which is why the sha is printed.

# `pipefail` because the health check is a pipeline, and curl failing into a
# grep that succeeds is exactly the lie this exists to catch.
set -euo pipefail

# Where this script lives, so neither cron nor ssh needs a working directory.
DEPLOY_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(cd "$DEPLOY_DIR/.." && pwd)"

REF="${1:-master}"
REMOTE="${ASPIRE_DEPLOY_REMOTE:-origin}"

# The ref goes to git as an argument, so a leading dash would be an option and
# not a branch. Checked here as well as in `aspire-deploy`, because this script
# is also run by hand and a fence that only exists on one path is not a fence.
case "$REF" in
'' | -* | *[!A-Za-z0-9._/-]*)
	echo "Not a ref: $REF" >&2
	exit 2
	;;
esac

# The port the host's nginx proxies to — `.env`'s, because that is the one
# compose published. Read rather than sourced: `.env` holds both secrets, and
# a script does not need to have them in its environment to curl a port.
PORT="$(sed -n 's/^ASPIRE_PORT=//p' "$DEPLOY_DIR/.env" 2>/dev/null | tr -d "\"' \r" | tail -1)"
PORT="${PORT:-8081}"

# How long the API gets to migrate and answer before this calls it a failure.
# A migration on a board of a hundred dreams is milliseconds; the minute is
# for the first start after a base image change.
HEALTH_SECONDS="${ASPIRE_DEPLOY_HEALTH_SECONDS:-90}"

compose() {
	docker compose -f "$DEPLOY_DIR/docker-compose.yml" --project-directory "$DEPLOY_DIR" "$@"
}

say() {
	echo "==> $*"
}

cd "$REPO_DIR"

# ── the code ─────────────────────────────────────────────────────────────────

# A working copy edited on the box is somebody debugging at two in the
# morning. Deploying over it without a word is how the fix nobody wrote down
# gets built into an image and forgotten, so this stops and shows what is
# there. `ASPIRE_DEPLOY_FORCE=1` goes ahead *with* those changes still in the
# tree — it does not throw them away, and git itself refuses the
# fast-forward if they are in the way of what is coming.
if [ -n "$(git status --porcelain)" ] && [ "${ASPIRE_DEPLOY_FORCE:-0}" != "1" ]; then
	echo "The working copy at $REPO_DIR has changes that are not committed:" >&2
	git status --short >&2
	echo "Commit them, or re-run with ASPIRE_DEPLOY_FORCE=1 to build them in as they are." >&2
	exit 1
fi

say "fetching $REMOTE"
git fetch --prune --tags "$REMOTE"

# A branch is moved to and fast-forwarded, so the box stays on a branch and
# `git log` reads the way anybody expects. A tag or a sha has no branch to be
# on, so it is checked out detached — which is honest about what it is.
if git rev-parse --verify --quiet "$REMOTE/$REF" >/dev/null; then
	git checkout --quiet "$REF"
	git merge --ff-only "$REMOTE/$REF"
elif git rev-parse --verify --quiet "$REF^{commit}" >/dev/null; then
	git checkout --quiet --detach "$REF"
else
	# Said here rather than by git, which answers a ref it cannot find with a
	# sentence about path arguments.
	echo "No branch, tag or commit called $REF — and $REMOTE has been fetched." >&2
	exit 1
fi

SHA="$(git rev-parse --short HEAD)"
say "deploying $SHA — $(git log -1 --format=%s)"

# ── the containers ───────────────────────────────────────────────────────────

# `--pull` fetches the base images' security patches. Without it `build`
# reuses whatever it pulled the first time, forever.
say "building"
compose build --pull

# `up -d`, never `restart`: a container keeps the environment it was created
# with, so a changed `.env` would never reach a restarted one (D41's lesson,
# and the same trap as the VAPID keys).
say "starting"
compose up -d

# ── proof ────────────────────────────────────────────────────────────────────

# The API migrates on its first start, so this is also the migration's result.
say "waiting for the app to answer on :$PORT"
HEALTH=''
for _ in $(seq 1 "$HEALTH_SECONDS"); do
	HEALTH="$(curl -fsS --max-time 3 "http://127.0.0.1:$PORT/api/v1/health" 2>/dev/null || true)"
	case "$HEALTH" in *'"ok":true'*) break ;; esac
	HEALTH=''
	sleep 1
done

if [ -z "$HEALTH" ]; then
	echo "FAILED: no healthy answer on :$PORT after ${HEALTH_SECONDS}s." >&2
	echo "The last of the API's log:" >&2
	compose logs --tail=50 api >&2
	echo >&2
	echo "Nothing was rolled back: the containers are the new build. $0 <previous-sha> puts the old one back." >&2
	exit 1
fi

# Dangling images only — the layers this rebuild just orphaned, which on a
# VPS that builds from source is the whole of why the disk fills. An image a
# container is using is not dangling, so this cannot reach what is running.
# It does reach the previous build, which is fine: going back is this script
# with an older sha, and that builds it again.
docker image prune --force >/dev/null

say "deployed $SHA — $HEALTH"
