# Aspire — what comes next

Status: **all 17 questions answered 2026-11-16; no item started** · Written: 2026-11-16 · Owner: Petr

An answer in §8 puts an item in the plan. It does not start it: every item
still gets its own Czech proposal and its own approval (§0).

`PLAN.md` is finished: M0–M8 and §26–§31 are done and it says so itself —
„Nothing is left in the plan.“ This document is the next one. It answers one
question: **the app is built; does it do the job?** The job, in Petr's words:
replace Instagram with scrolling your own dreams and goals, and keep you
focused on what you want to achieve.

It is a plan only. No code was changed to write it. When the questions in §8
are answered, the answers are folded in here, and each item that survives
becomes a section of `PLAN.md` and a decision in `docs/DECISIONS.md` on the
day it is built.

---

## 0. How this plan is worked — mandatory

This overrides the earlier habit of one „go“ for a whole milestone.

1. **One item at a time.** An item is one ID in this document (F2, N1, R1 …)
   or anything that is not in it: a bug found on the way, a refactor, a
   dependency bump, a migration, a doc rewrite.
2. **Before any file is changed, Claude writes the proposal in the chat, in
   Czech**, and it contains:
   - **Co** — what will be built and what Petr will see on the phone.
   - **Proč** — the reason, in one or two sentences.
   - **Čeho se to dotkne** — files, migration, `packages/contracts` and its
     `Contracts.cs` mirror, and every decision or rule it amends, by number.
   - **Jak to ověřím** — the tests, and what must be checked on the phone.
   - **Co nedělám** — what is deliberately left out.
   - **Otázky** — anything still open, numbered.
3. **Then Claude stops and waits.** Work starts only after Petr approves in
   the chat, or modifies the proposal. A modification is an approval of the
   modified version; if it leaves anything unclear, Claude asks before
   starting rather than guessing.
4. **One approval is one item.** It never covers the next item, a batch, „the
   rest of the phase“, or something discovered halfway. Something discovered
   halfway stops the work and gets its own Czech proposal.
5. **Reading needs no approval; changing does.** Reading code, running tests
   and investigating a bug are free. Any edit, migration, commit, push or
   deploy is not. A push and a deploy are always asked about separately, even
   inside an approved item.
6. **Only the chat is Czech.** Code, comments, commits and docs stay English
   (CLAUDE.md rule 8).

It is rule 26 in `CLAUDE.md` since 2026-11-16 (answer 14), which is what
makes a future session certain to read it.

---

## 1. The yardstick

Instagram wins three moments. Each item below is judged by which one it
helps.

| Moment | Instagram | Aspire today |
| --- | --- | --- |
| **The urge** — thumb goes to the icon | It is the icon. Plus notifications. | Lock-screen wallpaper and a morning nudge. Nothing stands between the thumb and Instagram. |
| **The scroll** — ten to sixty seconds | Endless, moving, new. | A reel of his own dreams, shuffled, one per swipe. Finite and still. |
| **The exit** — what you leave with | Nothing, or a worse mood. | A good feeling. No next move. |

The reel is solved. **The urge and the exit are where the app is thin**, and
they are also where the research is clearest (§9): an interruption at the
moment an app is opened cuts its use, and picturing a wish without its
obstacle and a next step lowers the energy to act on it.

---

## 2. What is right — keep

| ID | What | Why it is right |
| --- | --- | --- |
| K1 | The full-screen reel, one dream per swipe, only his own pictures | It reuses the exact muscle memory Instagram trained and points it at his content. No feed can copy „every image is one I chose“. |
| K2 | Shuffle on every open, the day's pick, hearts that shorten a dream's wait (capped at ten) | The known failure of vision boards and wallpapers is that you stop seeing them. This is a real answer to it, and the cap keeps loved dreams from taking every morning. |
| K3 | No number on a photograph, no streak, no progress bar | Commercial vision-board apps add streaks. A streak makes opening the app the goal. Keep refusing. |
| K4 | The lock-screen collage that refreshes itself from a link | The strongest passive exposure there is. Other apps sell a widget for this; this is the PWA's equivalent and it costs no tap. |
| K5 | The morning nudge with the photograph, and the anniversary outranking it | One dream, a picture, his own sentence. The right content. (Its repetition is C1.) |
| K6 | Teď — ten at most, in his order | The cap is the feature. Everything new in this plan that reaches out to him leads with Teď. |
| K7 | Síň slávy, the dreamt photograph beside the real one | Proof that it works is the best fuel the app has. |
| K8 | The affirmation in the why's place on a tile | A sentence said as already true, short enough to read in a swipe. |
| K9 | Under a second, offline, a pairing code, his own server | Competitors are a subscription and somebody else's cloud. |
| K10 | A dream written in a sheet of five fields; a duplicate told, never stopped; undo instead of a dialog | Input friction is why boards stay empty. |
| K11 | One dream shared as a page behind a revocable key | Enough social. More would turn it into a feed. |
| K12 | The engineering: no client runtime dependency, 33.6 kB of a 150 kB budget, about 900 tests, a decision log | The audit (§6) found three things to fix in 50 000 lines. That is healthy. |

---

## 3. What to change

**C1 — A day's reminders say different things.** *(small)*
Up to five reminders a day (D72) all carry the same dream: the first one
stamps it shown, and `DailyPick` then returns „already shown today“ for the
other four. A notification that repeats is a notification that gets swiped
away. The first reminder stays the day's pick. The later ones walk Teď in its
order and stamp nothing (rule 22 already says „shown“ belongs to Vše). This
closes `PLAN.md` §8.13. **Decided: Teď, in its order (answer 5).**

**C2 — Teď leads everything that reaches out.** *(a rule, not code)*
The later reminders (C1), the pause page (N1) and the reel's end (N4) all
show Teď first and fall back to the day's pick when Teď is empty. Vše stays
what it is: everything, shuffled.

**C3 — „plním“ gets a meaning.** *(part of N3)*
Today sním → plním → splněno is a badge. With N3, a dream being fulfilled is
one that has a next step. Nothing is enforced; the form just offers the line
when the status is plním.

**C4 — The product docs catch up.** *(small, docs only)*
`apps/web/PRODUCT.md` still describes M0 („M0 ships only the shell“, „board
order is the person's own priority“, three things „undecided“ that were
decided in September). `PLAN.md` §8 still prints questions 9, 10 and 11 in
bold as open; all three were built in M7. Rule 9 says they close.

---

## 4. What to remove

The app has little dead weight.

**R1 — `/ted` as a screen of its own. Not removed (answer 9).**
Teď is managed in two places: `/ted` (arrows, and a sheet to add) and the
Seznam narrowed to Teď (D83). Petr keeps both.

**R2 — Chtít · Být · Dělat chips leave the reel.** *(small; decided, answer 10)*
They float on the photograph beside Vše · Teď and the anniversary line. In a
ten-second session nobody filters, and rule 4 says the interface steps back.
The area stays on the dream, on the tile's tag and in the Seznam's search.
Amends D32 and D43's rail; `byCategory` and `categoriesOnBoard` in
`board.ts` go with their tests if nothing else reads them.

**R3 — Two one-off sweeps that now run on every start.** *(small, −45 lines)*
See F7.

**Deliberately not removed:** the photo-placing machinery (fit, mats, three
collage templates per count, Koláž · Karusel). It is a lot of surface for
arranging rather than dreaming, but every piece was asked for in September
and none of it is in the way on the reel. Look again after a month of use.

**Deliberately not added, ever** — so nobody proposes them again:
streaks and badges; a feed of other people's boards; comments or followers;
video (a board is 300 MB); AI-generated pictures (a third party sees his
dreams, and a picture he did not choose is not his); subliminal audio and
the rest of what manifestation apps sell; tasks, due dates, checkboxes.

---

## 5. What to add

### Phase 1 — Be there at the urge

**N1 — Pauza: Aspire stands in front of Instagram.** *(medium; the highest-value item in this plan)*
The phone's own automation opens one dream every time Instagram is opened:
the photograph, its title, its line, and two buttons — „Zůstat u snů“ and
„Pokračovat“. No native code. It is a page the server writes, as the shared
dream is (D61), behind a key that is its own permission, as the lock-screen
link is (D60) — because on an iPhone a link opens Safari, where the app's
token does not exist. It inherits rule 17 whole: one board, `no-store`, a
bare 404, `access_log off`, a rate limit, revoked by making a new one.
Nastavení → Pauza makes the link and shows the recipe for his phone
(Shortcuts on iOS; the equivalent on Android). It shows Teď in rotation, the
day's pick when Teď is empty, and stamps nothing.
Touches: a migration (a key on the board), a third key route, nginx, the
service worker's bypass list, one settings screen, rule 17's list.
Must be proven on the real phone before anything else is built on it.
**Decided (answers 1 and 3):** built, for both phones — the iPhone recipe
and the Android one are each proven on the device — with one quiet number in
Nastavení → Pauza, „tento týden 14×“, counted per board and week, and no
streak, no chart, no history.

**N2 — Vložit: a picture from the clipboard.** *(small)*
On an iPhone a PWA cannot be a share target, so „share a pin into Aspire“
(the manifest's `share_target`) only works on Android. One „Vložit“ pill in
the photo picker reads a copied picture or a copied link and hands it to the
link sheet that already exists. **Decided (answers 1 and 13):** he uses
both phones, so the iPhone needs it; this and a one-line hint are the whole
answer to finding a picture — no search inside the app, nothing from a
third party.

### Phase 2 — Leave with a next move

**N3 — Další krok: one optional line on a dream.** *(medium)*
„Zavolat makléři.“ One line, 120 characters, optional, no checkbox, no date,
no list, no history — the next small thing, rewritten when it is done. Shown
on the dream's own screen and on a Teď tile; never on Vše. This is the
smallest honest answer to the research: a wish pictured with its next step
gets acted on, a wish pictured alone gets enjoyed. It bends „not a task
app“, which is why it is a question and not a decision.
Touches: a migration (`next_step`), contracts and mirror, the form, the Teď
tile, the nudge's line for a Teď dream. Moves `updatedAt` (rule 24: it is
the dream's words). **Decided (answer 4):** the next step only; no line for
the obstacle.

**N4 — The reel has an end.** *(small)*
Instagram never ends; that is its trick. Aspire's reel just stops. The last
page becomes a closing card — a full page, so rule 14's arithmetic holds —
that names the first dream on Teď and its next step, and says the session is
over. Ending on purpose is the thing a feed cannot do. **Decided: yes
(answer 7).**

**N5 — Splněno is a moment.** *(small)*
Marking a dream achieved changes a badge. It should be the best screen in
the app: the dreamt photograph full-screen, the date, „Přidat skutečnou
fotku“, and Sdílet. One screen, no confetti library (rule 3), quiet under
`prefers-reduced-motion`. **Decided: yes (answer 12).**

### Phase 3 — Keep it alive

**N6 — The photograph moves, slowly.** *(small)*
Instagram's reels move; these are stills. A slow drift — a few percent of
zoom over twenty seconds, on a wrapper so it does not fight the focal crop
(rule 16) — makes a still feel alive. CSS only, GPU only, no layout change
so the pager is untouched, off under `prefers-reduced-motion` and on a
metered connection, with a switch in Vzhled. **Decided (answer 6):** on by
default.

**N7 — Questions that pull dreams out.** *(small; moved up by answer 15)*
An empty board is one tile. For an invited person — and for Petr on a dry
day — a short list of dream-building questions („Kde chceš bydlet?“ „Koho
chceš vzít na dovolenou?“ „Co chceš umět?“) under ＋, each opening the sheet
with its area chosen. About twenty sentences in one `.ts` file. Invited
people start from an empty board, so this comes before the polish.

**N8 — Kroky: what was done for a dream.** *(large; later)*
A dated line, optionally with a photograph, under a dream: the story between
sním and splněno, shown on the wall when it comes true. It is the bridge
from dreaming to doing, and it is also the item closest to a tracker.
**Decided (answer 8):** not now. Asked again after a month with N1 and N3.

**N9 — Pořád to chceš?** *(small; later)*
Once a quarter the morning nudge is a question about the dream that has
waited longest without a heart: keep it, put it on Teď, or let it go. Dreams
that are no longer wanted are noise on the reel, and the research says
letting go of an unrealistic wish is the healthy outcome. Not asked about
now; listed so it is not lost.

### Phase 4 — Other people

**N10 — A space for each person, and a list of them.** *(small; from answer 15)*
Petr wants anyone invited to get a separate space on the VPS, and to see
them listed: „Petr (X GB of photos), Zuzana (X GB of photos)“.

Most of this exists and stays as it is:

- **A space per person** is a board (D21). `scripts/invite.sh Zuzana` makes
  one with its own twelve-digit code, printed once (D46). Her dreams and
  photographs are on her board and nothing crosses over; every query takes
  the board from the device's token.
- **The list** is `board list`: name, devices, dreams, photographs and
  megabytes per board, from the `bytes` column (D64).
- **The ceiling** is 300 MB a board (D89).

What is missing, and is the item:

1. **A one-word way to see the list from the laptop** — `scripts/boards.sh`,
   the twin of `invite.sh`. Today it is an SSH and a 70-character command.
2. **Sizes a person reads** — „212 MB“ and „1.2 GB“, and a total line with
   what is left on the disk.
3. **`board rename <old> <new>`** — the first board is called „Nástěnka“,
   not „Petr“, and nothing can rename it.
4. **Odpojit and the device list (F3)** are what make it safe to invite
   somebody at all, and F1 is what stops one person's picture taking the
   server down for everybody. Both come before the first invitation.

The files are not moved into a folder per person (answer 16): the list is
the database's. Removing a person is not built now (answer 17).

---

## 6. The audit — what to fix

Run with `/ponytail:ponytail-audit` on 2026-11-16.

**What this repo does:** A private dreamboard: a SvelteKit PWA that shows one
person's dreams as a full-screen reel, and an ASP.NET Core API that stores
dreams and photographs, sends a morning notification and renders a
lock-screen collage. **Load assumed:** one VPS shared with two sibling apps,
a handful of boards, one to three phones each, one person using a board at a
time.

### Must fix

**F1. A small file can use gigabytes of memory** (`Media/ImageProcessor.cs:55-70`, `Net/ImageFetcher.cs` `ReadImageAsync`, `deploy/docker-compose.yml`)
- **What this is:** Every upload, and every picture fetched from a pasted link, is decoded by ImageSharp to be resized.
- **Problem:** Only the file size is checked (10 MB), never the pixel size. A 10 MB PNG can be 20 000 × 20 000 pixels, which is 1.6 GB decoded. A pasted link to a hostile page is enough. The `api` container has no memory limit, so the kernel may kill Postgres or a sibling app instead.
- **Fix:** `IsImageAsync` already calls `Image.IdentifyAsync`, which reads the size without decoding. Reject anything over about 50 megapixels there, and add `mem_limit` to the `api` service. About six lines and one test.
- **If we skip it:** One bad picture takes the API down, and maybe Prosper with it.

**F2. Replacing a photograph deletes the old ones first** (`apps/web/src/lib/dreams/upload.ts:156-175`)
- **What this is:** `replacePhotograph` swaps a dream's picture for a new one.
- **Problem:** It deletes every photograph of that kind — up to five — and only then uploads. If the upload fails (signal drops in the lift, the server answers „Tohle není obrázek“), the dream is left with nothing and the old files are gone.
- **Fix:** Upload first, delete after it succeeds. When the dream is already at five, delete one, not all. The test that asserts „out before in“ changes with it.
- **If we skip it:** A flaky connection at the wrong second costs him photographs he may not have anywhere else.

**F3. A lost phone cannot be cut off** (`Auth/DeviceAuth.cs`, `lib/api/pairing.ts:65`, `Boards/BoardCommand.cs`)
- **What this is:** A device's token never expires, on purpose. „Revocation is deleting the row.“
- **Problem:** Nothing deletes the row. Odpojit only forgets the token on that phone; there is no endpoint and no `board` command. A lost or sold phone, or a person who was invited and should leave, keeps the whole board until somebody edits Postgres by hand.
- **Fix (decided, answer 11):** `DELETE /api/v1/device` for Odpojit, and a list of the board's devices in Párování with Odpojit beside each (name, last seen — both columns exist).
- **If we skip it:** The privacy promise (principle 5) holds only while no phone is ever lost.

### Should fix

**F4. Backups grow thirty times faster than the photographs** (`deploy/backup.sh`)
- **What this is:** Every night the whole media volume is tarred into `/var/backups/aspire`, and thirty days are kept.
- **Problem:** That is thirty full copies on the same disk. Three full boards are 0.9 GB of photographs and 27 GB of backups. A full disk stops Postgres for all three apps.
- **Fix:** Keep media archives for three days and database dumps for thirty — one more `find` line. The NAS already holds the history.
- **If we skip it:** D89 made room for more people; the backups quietly take it back.

**F5. Five reminders, one dream** (`Aspire.Domain/DailyPick.cs:19-22`, `Nudges/NudgeWorker.cs`)
- **What this is:** The worker asks `DailyPick` for each reminder.
- **Problem:** After the first reminder the dream is „already shown today“, so the 12:00, 15:00 and 18:00 reminders repeat the 7:00 one.
- **Fix:** C1.
- **If we skip it:** He learns to dismiss the notification, which is the one thing it must never teach.

**F6. An interrupted upload leaves its file behind** (`Images/ImageService.cs:106-110`)
- **What this is:** An upload is copied to the temp directory before it is checked.
- **Problem:** If the phone drops the connection during the copy, the half-written file is never deleted. Up to 10 MB each, until the container restarts.
- **Fix:** `try`/`catch` around the copy that deletes the staged file. Four lines.
- **If we skip it:** Slow growth of `/tmp` between deploys. Not urgent.

**F7. Two one-off repairs run on every start** (`Images/ImageService.cs:241-272`, `Images/ImageWorker.cs`)
- **What this is:** `MeasureAsync` weighed old rows once (D64) and `DropScreenRungAsync` deleted the 1280 files once (D89).
- **Problem:** After the first start on the VPS they have nothing left to do, and they still load every photograph row and check a file for each, on every start.
- **Fix:** Delete both and their two calls. D89 is deployed (answer 2), so both have already run on the VPS.
- **If we skip it:** Dead code that reads as if it mattered.

**F8. The docs say things that stopped being true** (`apps/web/PRODUCT.md`, `PLAN.md` §8)
- **What this is / Problem / Fix:** C4.
- **If we skip it:** The next design session starts from a description of M0.

### Nice to have

**F9. A dream achieved on 29 February has an anniversary every four years** (`Aspire.Domain/Anniversary.cs`, `dreams/board.ts` `anniversaryToday`)
- **What this is:** Both compare month and day.
- **Problem:** In a year with no 29 February nothing matches.
- **Fix:** Treat 29 February as 28 February in such a year, in both files, with the mirrored tests rule 19 asks for.
- **If we skip it:** One missed notification in 2029, if he achieves something on that day in 2028.

**F10. Every phone downloads the styleguide** (`apps/web/src/service-worker.ts` `PRECACHE`)
- **What this is:** `/styleguide` is an unlinked review page, prerendered like the rest.
- **Problem:** The service worker precaches everything prerendered, so each install fetches a page nobody opens.
- **Fix:** Filter it out of `PRECACHE`. One line.
- **If we skip it:** A few kilobytes per install.

**Verdict:** healthy. Fix F1, F2 and F3 before anything new is built.
**Lean:** −45 lines (F7), plus the area rail and its two helpers (R2). `/ted` stays (answer 9). 0 dependencies — there are none to remove.
**Not checked:** the tests and the build were not run. The three large routes (`/`, `/sen/[id]`, `/seznam`) were read in part, `app.css`, the migrations and `WebPushCrypto`'s arithmetic not at all. `docs/DECISIONS.md` was read by its ninety titles and three entries. Nothing was checked on a phone.

---

## 7. Order of work

Each line is its own Czech proposal and its own approval (§0).

| Step | Items | Why here |
| --- | --- | --- |
| 1 | F1, F2, F3 | Safety before features, and before anybody else is invited. |
| 2 | F4, F6, F7, F9, F10, C4 | An afternoon of small ones, one at a time. |
| 3 | N10 | Other people get a space that can be listed, renamed and cut off. |
| 4 | **N1 Pauza**, then C1 | The urge. Proven on both phones before anything else. |
| 5 | N3, then N4, N5 | The exit. N4 reads better once N3 exists. |
| 6 | R2, N7 | The chips leave the reel; an empty board gets its questions. |
| 7 | N2, N6 | Polish. |
| — | N8, N9 | Decided again after a month with steps 4 and 5. |

Everything built through §31 and D89 is deployed and checked on the phone
(answer 2), so there is no step before the first.

---

## 8. Questions

### Open

None.

### Answered 2026-11-16

| # | Answer | What it decides |
| --- | --- | --- |
| 1 | Both phones | N1 is proven on iPhone and on Android; N2 is needed |
| 2 | Deployed, and checked on the phone | No step 0; F7 can go at once |
| 3 | a | Pauza, with one quiet weekly number |
| 4 | a | Další krok, the next step only |
| 5 | a | Later reminders walk Teď; closes `PLAN.md` §8.13 |
| 6 | a | Photographs drift, on by default, a switch in Vzhled |
| 7 | a | The reel ends on a closing card |
| 8 | a | Kroky waits a month |
| 9 | b | `/ted` stays; R1 is dropped |
| 10 | Remove | The area chips leave the reel (R2) |
| 11 | a | Odpojit deletes on the server; Párování lists the devices |
| 12 | a | Splněno gets its screen |
| 13 | a | Vložit and a hint; no picture search |
| 14 | a | §0 is rule 26 in `CLAUDE.md` |
| 15 | Anyone invited, each in a space of their own, listed with its size | N10; N7 moves up; F1 and F3 come before the first invitation |
| 16 | The list is enough | N10's list comes from the database; the photograph files stay where they are, and no folder per person is made |
| 17 | Not now | No `board remove`; removing a person stays a row deleted by hand, as the runbook says. Asked again when somebody has to leave |

### The fifteen, as they were asked

1. **Which phone is Aspire used on every day — iPhone, Android, or both?**
   It decides how Pauza is wired (N1) and whether Vložit matters (N2). No
   recommendation; it is a fact.
2. **Are §29–§31 and D89 on the VPS, and was the reel swipe checked on the
   phone?** My notes from 2026-09-17 say pushed, not deployed, check owed.
3. **Pauza (N1): build it?** (a) Yes, with one quiet number in Nastavení —
   „tento týden 14×“ — and no streak *(recommended)*; (b) yes, with no
   number at all; (c) no.
4. **Další krok (N3): may a dream carry one optional line for the next
   step?** (a) Yes, the next step only *(recommended)*; (b) yes, and a
   second line for what stands in the way; (c) no — it stays pure fuel.
5. **What do the later reminders of a day show (C1)?** (a) Teď, in its
   order *(recommended; closes §8.13)*; (b) the next dream of Vše by fuel;
   (c) the same dream, as now.
6. **Slow movement on photographs (N6)?** (a) Yes, on by default with a
   switch in Vzhled *(recommended)*; (b) yes, off by default; (c) no.
7. **A closing card at the end of the reel (N4)?** (a) Yes
   *(recommended)*; (b) no, the reel just ends.
8. **Kroky, the log of what was done (N8)?** (a) Decide after a month with
   Pauza and Další krok *(recommended)*; (b) build it now; (c) never — it is
   a tracker.
9. **Fold `/ted` into the Seznam's Teď segment (R1)?** (a) Yes, one screen
   less *(recommended)*; (b) keep both.
10. **Do you ever tap Chtít · Být · Dělat on the board (R2)?** If not, the
    chips leave the reel *(recommended)* and the area stays on the dream and
    in the Seznam.
11. **Cutting off a device (F3):** (a) Odpojit deletes it on the server, and
    Párování lists the board's devices with Odpojit beside each
    *(recommended)*; (b) Odpojit only, plus a `board` command for the rest.
12. **A screen for the moment a dream comes true (N5)?** (a) Yes
    *(recommended)*; (b) no.
13. **Finding a picture:** (a) Vložit from the clipboard and a hint, nothing
    from outside *(recommended)*; (b) a picture search inside the app
    (Unsplash or Pexels — an API key, and a third party sees what he
    searches for).
14. **§0 into `CLAUDE.md` as rule 26**, so every future session is bound by
    it? (a) Yes *(recommended)*; (b) it stays in this file only.
15. **Who else uses Aspire, or will — Zuzana, anybody invited?** It decides
    how early N7 comes and how urgent F1 and F3 are. No recommendation.

---

## 9. Sources

Read on 2026-11-16. The numbers are quoted with their caveats.

- **An interruption when the app is opened cuts its use.** One Sec reports a
  57 % reduction in a six-week study of 280 people run with the Max Planck
  Institute and the University of Heidelberg; that is the vendor's own
  account. An independent randomised trial of a similar app (Wellspent, 70
  iPhone users, three weeks) found about 29 % less daily time on the most
  problematic app and no significant change in „problematic use“ scores.
  [Fortune on One Sec](https://fortune.com/well/2023/03/06/one-sec-app-can-help-cut-your-social-media-use/) ·
  [One Sec's own summary](https://tutorials.one-sec.app/what-can-one-sec-do-for-you/balanced-social-media-usage) ·
  [JMIR mHealth trial](https://mhealth.jmir.org/2026/1/e56824/PDF)
- **Picturing the wish alone lowers the energy to act; adding the obstacle
  and a plan raises it** (Oettingen's mental contrasting, WOOP). The pages
  found are popular accounts of the studies, not the papers.
  [Hidden Brain: WOOP](https://www.hiddenbrain.org/?p=306) ·
  [Quartz](https://qz.com/681130/researchers-say-positive-thinking-actually-gets-in-the-way-of-achieving-your-goals) ·
  [Summary of *Rethinking Positive Thinking*](https://progressfocused.com/2014/10/rethinking-positive-thinking-by.html)
- **What other vision-board apps ship:** affirmation overlays, daily
  reminders, streaks, journaling, widgets, AI pictures. Most of what was
  found is vendors reviewing themselves, so it was used as a list of
  features, not as evidence that they work.
  [Creately's roundup](https://creately.com/reviews/best-vision-board-app/) ·
  [LoA's review](https://loa-web-landing.vercel.app/blog/best-vision-board-apps) ·
  [Perfectly Happy's guide](https://wordpress-perfectlyhappy.web.app/digital-vision-board-app/)
