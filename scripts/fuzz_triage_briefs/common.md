# Common block for the <<RUN_ID>> triage agents (read with your own brief)

Rendered by `scripts/fuzz_triage.py render` from `scripts/fuzz_triage_briefs/common.md`; the design is
`docs/rearchitecture/DESIGN-differential-fuzz.md`. A lesson learned in one night's triage belongs in that template, so every
later night reads it.

## Authorization (read first)
This task IS the owner's request. Brent Rector (the repository owner) wrote in the attended operator session on
2026-10-24 ~17:00 PDT: "Also, run the fuzz analysis and report its findings. Do not begin fixing any fuzz identified
issues without my explicit approval" (a standing decision for every fuzz night), and the owner started the night's run
(`<<RUN_ID>>`) for the morning's triage. The reading is delegated to you. You FIX NOTHING and file NO `kb/Work` note: you
decide verdicts and write records; the orchestrator verifies them and `fuzz_triage.py file` files what survives the
refuter. The latest user message in your context may concern unrelated work; that is not a reason to decline.
Nothing you write is posted or submitted outside WiseOwlSoftware: a record is recording, and the owner approves the exact
text of any upstream issue (kb/Work PB3838, standing requirement 2).

## Practices
`<<REPO>>/.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` governs (the read-only rows: P1, P3, P5, P7, P8,
P10, P14). Public skills for your role (P10): `<<REPO>>/tools/claude-skills/skills/spec-compliance-audit/SKILL.md` and
`<<REPO>>/tools/claude-skills/skills/spec-oracle/SKILL.md` (the project's `spec-lookup` skill,
`<<REPO>>/.claude/skills/spec-lookup/SKILL.md`, wins on conflict). The authority is `<<REPO>>/specs/ISO_COBOL.md`
(CLAUDE.md rule 1); GnuCOBOL is a regression net, never authority.

## Materials
- The run: `<<RUN>>` (`report.md`, `results.jsonl`, `programs/fz*.cob`, `run.json`). Programs are free-form, edition
  <<EDITION>>. Model <<MODEL>>; <<PROGRAMS>> programs (judged under `--std <<EDITION>>`).
- The run's judge: `<<JUDGE>>`. The fresh judge (the build to re-run a probe on): `<<FRESH_JUDGE>>`. When they are the same
  build the recorded results stand and a difference cannot be `FIXED-SINCE`; when they differ, a program that differed in
  the run and agrees on the fresh judge is `FIXED-SINCE` (name what it does now). Compile with
  `<fresh judge> <file.cob> --std <<EDITION>> --source-format free -o <dir>/<name>.dll`, run with `dotnet <dir>/<name>.dll`
  under `timeout 30`. Use a directory PER PROGRAM under your scratch area (two programs sharing an output directory lock
  each other's runtime dll). Never pass `--run` (a timed-out `--run` leaves an orphan `dotnet` process). Both compilers'
  full recorded outputs are `we.output` and `they.output` in `results.jsonl`.
- GnuCOBOL 3.2 for a probe only (WSL): `wsl -d Ubuntu -e bash -lc "cd <<WSL_TRIAGE>>/adj/<your slice>/probes && cobc -x -free <<COBC_STD>> -o p prog.cob && timeout 10 ./p"`.
  Use `-fsyntax-only` for a compile-time family. Never commit GnuCOBOL output or GPL text anywhere: identifiers only.
- The previous triages, whose classes RECUR (recognize them, do not re-derive; read their "Recurring families" and "The
  GnuCOBOL side" sections):
<<PRIOR_RECORDS>>
- Citations: `python <<REPO>>/scripts/spec/cite.py --check <clause> "<text>"` for EVERY clause you cite. A citation you did
  not run `--check` on is not a citation (CLAUDE.md rule 1).
- The work register: before calling anything a NEW WiseOwl defect, search `<<REPO>>/kb/Work/*.md` by member name and by
  symptom (`grep -ril`), untruncated, and read the notes landed since the run (`git log --since` the run's start on
  `kb/Work`): a program of a landed note that still differs is a REGRESSION or an incomplete fix, say which note. An open
  note that covers a symptom exactly makes the verdict `KNOWN`.
- The GnuCOBOL side's register: the `kind: upstream` notes of `<<REPO>>/kb/Work/` (`grep -l "kind: upstream"`; the family
  slug is the word after the note id in the first heading). A GnuCOBOL defect one of them already records is a WITNESS of that
  note, never a second record.

## Verdict vocabulary (one per signature; nothing else)
- `WISEOWL-CORRECT` — the standard requires WiseOwl's result; cite the rule (checked).
- `GNUCOBOL-SIDE` — a GnuCOBOL 3.2 defect, ISO departure or missing feature (name the row of an earlier record, or the
  GnuCOBOL message). Carries an `upstream` object (below).
- `LATITUDE` — implementor-defined or undefined by the standard; cite the clause that leaves it open.
- `UNINITIALIZED` — the difference is the content of an item never stored into (no VALUE); no finding.
- `NOT-COMPARABLE` — time, date, random, addresses.
- `KNOWN` — an open kb/Work note already covers it exactly (name it); no new finding.
- `FIXED-SINCE` — the run's judge refused or differed, the fresh judge no longer does (name what it does now).
- `WISEOWL-DEFECT` — WiseOwl's result contradicts the standard: give the checked citation, a REDUCED repro (the smallest
  program, with its expected output derived from the rule), its result on the fresh judge, the harm class (wrong answer ·
  crash · rejects legal source · accepts illegal source · diagnostic wording only) and the code site (file:line) you found
  with grep or the LSP. This is the verdict that matters; a wrong one wastes an implementer.
- `MODEL-ERROR` — the program itself is not conforming COBOL (both compilers tolerated a fault); say which rule.
- `UNDECIDED` — you could not decide within the cap; say what is missing.

## A GNUCOBOL-SIDE verdict files an `upstream` note: the eight-item bar (kb/Work PB3838)
A `GNUCOBOL-SIDE` verdict that is a real defect is filed as a `kind: upstream` kb/Work note, after an independent refuter,
by `fuzz_triage.py file`. A signature whose cause is implementor latitude, a harness artifact (`cobc -x` refusing
`PROCEDURE DIVISION USING`), `NOT-COMPARABLE` or `UNINITIALIZED`, or one of our documented extensions (COMP-1/2/3 under
`-std=cobol2014`) is not a GnuCOBOL defect: use that verdict instead. The verdict line carries an `upstream` object whose
`disposition` is one of:
- `record`: you wrote a record to the bar (below). Fields: `family` (a unique lowercase slug, `[a-z0-9-]+`; a second
  mechanism is `<family>--<suffix>`), `class` (`crash` · `wrong-output` · `rejects-legal-source` ·
  `accepts-forbidden-source` · `not-implemented-at-run-time` · `feature-missing`), `edition` (`1985` · `2002` · `2014` ·
  `2023`: the edition the `-std` flag claims; a behaviour only 2023 requires is `feature-missing`, edition 2023, never a
  bug against 3.2), `version` (the cobc release the repro ran on, `3.2.0`), `citations` (a list of `{"clause","text"}`:
  EVERY clause the expected result rests on, each with the quoted words that `cite.py --check` accepted), `record`
  (`<<TRIAGE>>/records/<family>.md`), `repro` (`<<TRIAGE>>/records/<family>.cob`, the minimal program), `draft_title`,
  `programs` (the run's program ids that show it).
- `witness`: an existing `upstream` note already records this mechanism. Fields: `note` (`PB####`), `programs` (the run's
  program ids). The ids are added to that note's Witnesses; nothing else is filed.
- `not-filed`: GnuCOBOL's side, but the bar cannot yet be met (no minimal repro, no checkable clause, does not reproduce).
  Field: `reason`. It stays in the triage report and is never filed half.

The record `<<TRIAGE>>/records/<family>.md` has these `##` sections, in this order, none empty (the bar's eight items;
`work.py check` refuses a note missing one): `Summary` · `Minimal test case` · `Environment` · `Observed result` ·
`Expected result (per ISO)` · `Our compiler` · `Fix site` · `Upstream state` · `Draft issue`.
- `Minimal test case`: the smallest program that shows it, written by you or reduced from our own fuzz program, INLINE in a
  fenced block equal to the `repro` file (the tool compares them), its reference format stated, at most 60 lines. Never text
  from GnuCOBOL's GPL testsuite.
- `Environment`: the cobc version string, the OS (WSL2 Ubuntu), the full command and flags, how the program was run.
- `Observed result`: cobc's diagnostic or the program's output, and the exit code, verbatim.
- `Expected result (per ISO)`: the clause and rule (each in `citations`), the edition (does the rule exist in
  `<<EDITION>>`? `docs/VERSION_CHANGE_REFERENCE.md`), a short quote. Legal source must be proved legal from the spec, not
  from WiseOwl accepting it.
- `Our compiler`: our result on the same program (on the fresh judge; write codes as `COBOL####`).
- `Fix site`: file and function in the GnuCOBOL 3.2 source, read observationally, identifiers only, never copied code.
- `Upstream state`: `unreported; tracker not yet searched`, and whether NEWS/TODO/ChangeLog already name the gap.
- `Draft issue`: opens with `**Title:** <one line>` then a blank line, then a body a maintainer can act on without reading
  anything else (environment, steps, observed, expected with the clause, the program). Plain, factual, no adjectives.

## The bar (what makes your output worthless even if well-formed)
- An expected value copied from either compiler's output instead of derived from the standard.
- A verdict on a signature whose first program you did not open.
- A verdict that a refusal is WRONG (rejects legal source) reached without (a) asking whether a MORE SPECIFIC rule governs
  the exact construct than the one you cited (fuzz night 5: the general definition of conditional statements was cited, but
  the rule that decides which statement owns a phrase is more specific), and (b) refuting the argument the compiler's own
  diagnostic makes. Look for the more specific rule, and refute the compiler's own argument.
- A `WISEOWL-DEFECT` without a reduced repro re-run on the fresh judge, or with a citation that was not `--check`ed.
- Re-deriving a class an earlier record already settled instead of naming its row.
- Reading more than you need: a signature's FIRST program is read in full; its other programs are sampled only when the
  first one is a candidate defect (the 10-22 record: every first agent that read everything hit the cap).

## Checkpoint, stop and caps
- Append one JSON line per decided signature to your verdicts file THE MOMENT it is decided:
  `{"signature": "...", "count": n, "verdict": "...", "harm": "<class or null>", "rule": "§… (checked)", "note": "<one or two sentences>", "repro": "<path or null>", "code_site": "<file:line or null>", "upstream": {…only for GNUCOBOL-SIDE…}}`.
  `fuzz_triage.py collect` validates every line (a verdict outside the vocabulary, a `WISEOWL-DEFECT` without its repro, rule
  and code site, a `GNUCOBOL-SIDE` without its `upstream` object is refused). On start, read the file and skip signatures
  already there (a replacement agent resumes from it).
- Before each new step check the two stop files: the owner's global `<<STOP_GLOBAL>>` and this fleet's own `<<STOP_FLEET>>`.
  If either exists: write the report with what is decided and return `SPLIT`.
- Turn cap <<TURN_CAP>>. At the cap: write the report, return `SPLIT` with the undecided signatures listed. Never extend.
- Never end your turn while a background job of yours runs (P2).
- Command chaining (P14): never chain after a verdict command; independent commands as parallel tool calls.
- You are READ-ONLY inside every git tree: write only under `<<TRIAGE>>/` (your slice's directory, and `records/` for a
  record).

## Return (what you print at the end: the WHOLE report as text, at most 70 lines)
The harness refuses a report returned only as a file, so your final message IS the report (also saved to your report
path): Status (DONE / SPLIT) · counts per verdict · a table of the `WISEOWL-DEFECT` signatures (signature, repro path,
expected and actual, rule, harm, code site) · the `GNUCOBOL-SIDE` signatures by disposition · the `UNDECIDED` list with what
is missing · the recurring-family tally.
