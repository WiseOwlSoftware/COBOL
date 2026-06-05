# CobolSharp version-conformance corpus (post-1985 standards)

NIST CCVS covers **COBOL-85 only**. This corpus is the equivalent for **COBOL-2002 / 2014 / 2023**: a growing set
of small, focused programs that each exercise a specific ISO spec feature, compiled under the matching
`--standard` dialect and checked against expected output. It is executed by `ConformanceTests` in the integration
test project and runs as part of `scripts/guard.sh`, so it provides **both conformance evidence and regression
protection**.

## Layout

```
tests/conformance/2002/   COBOL-2002 programs  (compiled --standard cobol2002)
tests/conformance/2014/   COBOL-2014 programs  (--standard cobol2014)
tests/conformance/2023/   COBOL-2023 programs  (--standard cobol2023)
```

## Adding a test — PART AND PARCEL OF EVERY POST-1985 FEATURE

Drop **two files** in the version directory; they are auto-discovered (no test code to write):

- `<name>.cob` — a self-contained COBOL program (one compilation group; multiple program/function units in one
  file are fine) that exercises the feature and `DISPLAY`s a deterministic result. Begin it with a comment naming
  the feature and ISO section, e.g. `*> ISO 8.4.3 — user-defined function inline invocation`.
- `<name>.out` — the exact expected stdout. Trailing whitespace/newlines are ignored and CRLF is treated as LF.

The program's **version directory selects the `--standard`**. Keep each program minimal and deterministic (avoid
date/time/random unless that is the feature under test).

## The rule

**Every feature added for 2002/2014/2023 MUST ship with at least one conformance test here, in the same commit as
the feature.** This corpus is the per-version "% to spec" evidence and the regression net as features accrue.

## Roadmap

Backfill conformance tests for already-landed M2 features over time; grow 2014/2023 as those milestones begin.
See `docs/ISO2023_CONFORMANCE_PLAN.md`.
