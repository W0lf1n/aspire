# Product

<!-- impeccable:product-schema 1 -->

Written from PLAN.md and the M0 brief without an interview: the plan answers
every question init asks. Facts marked _(inferred)_ were not stated outright.

## Platform

web

## Users

Petr, alone, on a phone held vertically: on a train, in a queue, in a bad
meeting. The job is to look at his own dreams for ten seconds and remember
why he is fighting through today. Desktop is secondary. _(inferred)_ Zuzana
may use it later on her own device; the pairing model makes that one board,
many devices, unless a second board is ever wanted.

## Product Purpose

A personal dreamboard: each dream is a full-screen image swiped through like
a reel, so attention goes to his own dreams instead of other people's
content. Pure fuel, not a task app and not a goal tracker. Success is the
board being opened daily and a dream image on screen in under a second.

## Positioning

The Yager "dream building" wall, in the pocket, private, on his own VPS. The
mechanism no feed can copy: every image is one he chose, about his own life,
with one line saying why.

## Operating Context

Third app in a trio after Prosper (finance) and Planner. Same stack and
deployment shape as Prosper: SvelteKit PWA, ASP.NET Core API, Postgres,
Docker Compose behind nginx on a Contabo VPS at aspire.petrbohac.eu.
Installed to the home screen; offline on repeat visits. Photos come from the
phone camera, the library, or a pasted URL.

## Capabilities and Constraints

- A dream is a title, one to five images, a one-to-three-sentence why, a
  category, an optional target year, and a status: dreaming, in progress,
  achieved.
- Board order is the person's own priority. On open, the first card is a
  random dream weighted toward the least recently seen.
- Auth is Prosper's: a pairing code typed once, a device-bound token. No
  accounts, no passwords.
- M0 ships only the shell: an empty board, the four navigation slots,
  Settings with the theme choice, and a styleguide route. No upload, no
  swipe, no image processing yet.
- UI language is Czech; code, comments and docs are English.
- Undecided: whether categories stay the fixed Yager set or become free
  form; whether a voice memo is worth building; whether a second board
  exists for a second person.

## Brand Commitments

- The name is Aspire.
- Sibling of Prosper: its spacing, type scale, radii, pills, circles and the
  floating glass bar, so the two feel like one family.
- Warmer and more colourful than Prosper, but restrained: one primary
  accent, one secondary, warm neutrals. At most two accent hues. Colour lives
  in accents and gradients, never in walls of colour.
- Photos are the hero. The interface steps back.
- Light and dark from the start, following the system, with a manual choice
  in Settings. Dark is where photos pop and must look best.
- Inter, self-hosted, at 400 / 500 / 600. No uppercase labels.

## Evidence on Hand

- PLAN.md at the repository root: vision, features, data model, milestones.
- Prosper's design system at ../financni prosperita/docs/DESIGN.md and its
  tokens, the visual starting point.
- No dreams, photos or copy exist yet. Nothing on the board may be invented
  as if it were his; demonstration material is labelled as such.

## Product Principles

1. Instant: cold open to a dream image in under a second, offline.
2. Visual: the image fills the screen; words are a title and one line.
3. Emotional, not analytical: no progress bars on the board, achievement
   celebrated rather than measured.
4. Frictionless input: a dream from the phone in three taps.
5. Private: his dreams, his server.

## Accessibility & Inclusion

Touch targets of 44 px or more, AA contrast in both themes, text on photos
always over a scrim, motion off under `prefers-reduced-motion`.
