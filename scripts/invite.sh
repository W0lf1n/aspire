#!/bin/bash
# Make a board for somebody and print its pairing code — here, in this
# terminal, and nowhere else (D46).
#
#     scripts/invite.sh Zuzana
#
# It is one SSH session that runs `board invite` in the API's own container,
# which is where the database and the hashing are. The code is printed once
# and then only its PBKDF2 hash exists, so what scrolls past here is the only
# copy: send it the way you would send a password, and if it is lost make a
# new one with `board code` rather than looking for this one.
#
# **Deliberately not a GitHub Action.** A workflow that prints a code leaves
# it in a run log for ninety days, readable by anybody who can read the
# repository. A board's code is the whole of what stands between a stranger
# and somebody's dreams (D21), so it does not go anywhere it can be scrolled
# back to.
#
# The box: override either for a different VPS or a different checkout.
#
#     ASPIRE_SSH=petr@aspire.petrbohac.eu scripts/invite.sh Zuzana
set -euo pipefail

SSH_TARGET="${ASPIRE_SSH:-root@aspire.petrbohac.eu}"
REMOTE_DIR="${ASPIRE_DIR:-/opt/aspire}"
SITE="${ASPIRE_SITE:-https://aspire.petrbohac.eu}"

NAME="${1:-}"
if [ -z "$NAME" ] || [ "$#" -gt 1 ]; then
	echo "usage: scripts/invite.sh <name>    # scripts/invite.sh Zuzana" >&2
	echo "A name with a space in it is one argument: scripts/invite.sh 'Zuzana K.'" >&2
	exit 2
fi

# The name reaches a remote shell, and a name is whatever a person typed. One
# pair of single quotes around it, with any single quote inside it closed and
# re-opened, is the only quoting that is actually safe there.
quote() {
	printf "'%s'" "$(printf '%s' "$1" | sed "s/'/'\\\\''/g")"
}

# `-T` because there is no TTY worth allocating and `exec` allocates one by
# default, which turns the output into something with escape codes in it.
ssh -- "$SSH_TARGET" "cd $(quote "$REMOTE_DIR/deploy") && docker compose exec -T api dotnet Aspire.Api.dll board invite $(quote "$NAME")"

cat <<EOF

What to send them, with the code above:

  1. Open $SITE on the phone.
  2. Add it to the home screen — Safari's Share → Přidat na plochu.
  3. Nastavení → Párování, type the code, name the device, Spárovat.

They get their own board: their own dreams, their own photographs, nothing of
yours and nothing of theirs on your board. Their device keeps the token; the
code is only for joining, so it is worth nothing to them afterwards.
EOF
