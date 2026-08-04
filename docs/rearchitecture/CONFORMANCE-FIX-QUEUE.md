# Verified Conformance Fix-Queue — MOVED

> ⛔ **THIS FILE IS RETIRED. The work register is `kb/Work/` — one note per item — and `kb/Work.base` is the
> view over it.** Nothing was lost: all 39 PB entries were migrated with their bodies **verbatim** (verified —
> zero lines dropped across 343,466 characters), and the entries that were never itemised finally are.

## Why it moved

"What is left to do" was authored in FIVE places, in five formats, and three of them claimed to be canonical:

| register | claim | problem |
|---|---|---|
| plan §0 NEXT | "the ONLY live-state SSOT" | buried in 3,614 lines |
| this file | owned its own LANDED tally | 2,484 lines of prose |
| `kb/Remaining Work Tracker.md` | "the canonical remaining-work tracker" | 5 days stale |
| plan §11 | the analysis backlog | inside the plan |
| this file's RESIDUE block | ~10 findings | prose, never itemised |

When three artifacts each claim to be the source of truth, none is — and it cost real work: the residue block
contained a **wrong-answer** defect (`EXCEPTION-STATEMENT` returns `GO` where Table 12 requires `GO TO`) that no
work list could see, because it lived inside a paragraph.

## Where things are now

- **`kb/Work/`** — the register. One note per item, tracked in git, frontmatter carrying `kind`, `status`,
  `severity`, `area` and the harm flags. The forensic prose — repro, citation, why a previous summary was wrong —
  is in each note's body, unchanged.
- **`kb/Work.base`** — every view: *Fix next*, *Blocked*, *Open but nobody gets a wrong answer*, the §11
  analyses, and everything.
- **`scripts/spec/work.py`** — `check` validates the register, `next` prints the ranked list (session-probe
  shows it every session), `stats` counts it.

## The ranking rule, because it is the point

`Fix next` = **not landed AND (wrong-answer OR crashes) AND not blocked** — ranked by what a defect does to a
user's program, never by its severity label. PB24 (`FUNCTION LENGTH` silently wrong) and PB39 (rule-id numbering,
zero wrong answers) are **both `[MAJOR]`**, and on 2026-08-04 the session picked PB39. This filter drops PB39 off
the list entirely.

The full pre-migration history of this file remains in git.
