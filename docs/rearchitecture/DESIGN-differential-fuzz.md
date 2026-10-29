# DESIGN — the differential fuzz: the night, the morning's triage, and the filing of GnuCOBOL's side

Status: current design of `scripts/differential_fuzz.py`, `scripts/fuzz_night.ps1`, `scripts/fuzz_triage.py` and
`.claude/workflows/fuzz-triage.js` (kb/Work PB3463 the harness, PB3561 the pause, PB3838 the upstream register, PB3972 the
triage tool). Implemented FROM this document (CLAUDE.md rule 2); a change to the triage updates it in the same change set.
The nightly experiment records (what each night found) are `docs/rearchitecture/evidence/fleet-optimization/*differential-fuzz*.md`
and are frozen; this document describes the machinery only.

## 1. Purpose and the rules that bind it

A local model writes COBOL programs from the standard's own clauses; WiseOwl and GnuCOBOL 3.2 each compile and run every
program; only the disagreements survive to a report. The model produces LEADS (`differential_fuzz.py`'s docstring: "the ISO
spec decides"); nothing it writes enters `kb/Work/`, a golden or the conformance suite without a check against the standard
(CLAUDE.md rule 1). GnuCOBOL is a net, never authority.

Owner rules the whole pipeline keeps:

- **Fix nothing without approval.** 2026-10-24 ~17:00 PDT: "Also, run the fuzz analysis and report its findings. Do not begin
  fixing any fuzz identified issues without my explicit approval." The triage reads, adjudicates and records; a confirmed
  WiseOwl defect becomes a `kb/Work` note only when the owner has approved the batch, and nothing in this pipeline changes the
  compiler.
- **The run cadence** (2026-10-22 ~12:15 PDT): one run, its triage, every confirmed defect fixed and landed, the judge rebuilt
  from that main, then the next run. The harness refuses to start while a non-`process_only` `kb/Work` note tagged `fuzz` is open.
- **The upstream bar** (PB3838, owner 2026-10-26 ~11:45 PDT): a GnuCOBOL defect is recorded reproducibly, as a `kind: upstream`
  note carrying the eight items of the bar; every "GnuCOBOL is wrong" verdict goes to an independent refuter before it is
  filed; nothing is posted or submitted outside WiseOwlSoftware without the owner's approval of the exact text; no GnuCOBOL
  source, testsuite program or expected output is committed or quoted at length (GPL).

## 2. The night

`pwsh scripts/fuzz_night.ps1 -Cobol <judge>` starts `scripts/differential_fuzz.py` and watches the GPU, pausing the run while a
game is played (`PAUSE` file protocol, PB3561; `fuzz_ollama.py` has the protocol). The **judge** is a frozen build of the
compiler under test (`--cobol`), the build of the main the previous triage's fixes landed on. The harness writes the **run
directory** (default `conformance-suite/results/fuzz-<stamp>/`, git-ignored, local):

| File | Content |
|---|---|
| `run.json` | model, judge (`cobol_exe`), edition, seed, clause prefixes, start time |
| `results.jsonl` | one row per program: `id`, `clause`, `title`, `verdict`, `signature`, `we` and `they` (compiled, codes, `first_error`, `run_rc`, `output`, `hung`, `crashed`, `runner_error`) |
| `report.md` | survivors grouped by signature (`### \`SIGNATURE\` × N`), the class table, runner errors |
| `programs/fz*.cob` | the programs (free format, edition 2014 by default) |

The verdict classes (`differential_fuzz.py#classify`): `BOTH_REJECT`, `NO_PROGRAM`, `BOTH_HANG` and `UNDEFINED_RESULT` are the
model's failures; `AGREE` and `OUTPUT_LATITUDE` are dropped; `THEY_REJECT`, `WE_REJECT`, `OUTPUT_DIFFERS`, `WE_CRASH`, `WE_FAIL_RUN`,
`EXIT_DIFFERS`, `WE_HANG`, `THEY_HANG`, `THEY_FAIL_RUN` are leads; `RUNNER_ERROR` is never a compiler verdict.

**`UNDEFINED_RESULT`** (kb/Work PB3862) is an output difference in a program whose result the standard leaves undefined, and the
program's text proves it (`scripts/fuzz_undefined.py#undefined_results`, one function; `classify` takes its yes/no). Three
findings, each a PROOF and never a guess, so every shape the analysis cannot follow reads as defined and the row stays a lead:

| Finding | The program | Authority |
|---|---|---|
| `UNWRITTEN_STORAGE` | DISPLAYs storage with no VALUE clause that no statement writes. Definedness is the least fixed point of the data flow: a VALUE, any statement naming the item (other than as a DISPLAY operand or a MOVE sender), or a MOVE / MOVE CORRESPONDING (ISO 14.7.6 1, 2) from a defined item or a non-item makes an item defined; a MOVE from storage that is itself unwritten does not. | ISO A.2 19) |
| `SUBSCRIPT_RANGE` | an integer-literal subscript outside 1 to the OCCURS maximum of its dimension | ISO 8.4.2.3.4 2), 14.6.13.1.1 NOTE 3 |
| `INCOMPATIBLE_DATA` | an unprefixed alphanumeric literal with a letter MOVEd to a numeric item | ISO 14.9.25.4 6) |

It abstains (no finding) for a program with a COPY, REPLACE, OPTIONS paragraph, compiler directive, RENAMES or a second program.
The analysis is flow-insensitive, so a DISPLAY before the first write is not found; a variable subscript or reference-modification
position and the content a MOVE moves at run time need the program run and stay leads. The row records the findings
(`undefined`); a row recorded before PB3862 is read again from its source under the run directory, so `--report` shows an old night
under today's classes. Only an OUTPUT difference becomes `UNDEFINED_RESULT`: an exit-status difference with the same output stays
a lead (`WE_FAIL_RUN`, `EXIT_DIFFERS`), because no undefined value can end the run.

## 3. The morning: `fuzz_triage.py prepare`

### 3.1 The tool

ONE tool, `scripts/fuzz_triage.py`, takes the run directory (`--run`) and the judge (`--judge`, default run.json's
`cobol_exe`; `--fresh-judge` is the build to re-run probes on, default the judge) as ARGUMENTS and keeps everything in a
triage directory. Nothing is retargeted by hand: no run id, date, judge path, TEMP path or session scratchpad is written in
the tool, the Workflow script or the briefs (the self-test greps for them). Subcommands, in the order the morning runs them:

| Subcommand | Job |
|---|---|
| `prepare` | rescore the output differences, slice the survivors, render the briefs (§3.3-§3.5) |
| `collect` | tally and VALIDATE the adjudicators' verdict files (§4.1); exit 1 on a bad line or an undecided signature |
| `cites` | check every clause the verdicts cite exists, and every `upstream.citations` and refutation-line `citations` quote with `cite.py`'s own check |
| `verify` | re-run each `WISEOWL-DEFECT` repro on the fresh judge, one directory per program; exit 1 when a repro file is missing or its compile outlives the timeout (the loop goes on to the next) |
| `refute-plan` | slice the `GNUCOBOL-SIDE` records for the independent refuters and render their briefs (§4.3) |
| `file` | file what the refuters upheld as `upstream` notes (§5); a dry run unless `--write` |
| `render` | re-render the briefs of a prepared triage (after a template edit) |

### 3.2 The triage directory

`<triage>` (`--triage`, default `<coord>/scratch/fuzz-triage/<run id>`; `triage_directory` refuses, with
`coord.scratch_problem`, any directory inside a git checkout). The default is NOT beside the run: the run directory is
`<repo>/conformance-suite/results/fuzz-<stamp>`, and the adjudicators and refuters are read-only roles whose PreToolUse hook
(`scripts/hooks/readonly_repo.py`) refuses every Write inside a git working tree, so a triage directory there would refuse every
verdict file and record they write (PB3972 review R1-1). The roles READ the run directory and WRITE only under `<triage>`:

```
triage.json  slices.json  refute-slices.json  filing.md
batch/residue.jsonl                      the rescored output differences
briefs/common.md  adj-<S>.md  refute-<R>.md
adj/<S>/input-<S>.md|residue-<S>.md      the slice's input
adj/<S>/verdicts-<S>.jsonl  report-<S>.md  probes/          written by the adjudicator
records/<family>.md|.cob                 the GNUCOBOL-SIDE records
refute/<R>/input-<R>.jsonl  refute-<R>.jsonl                the refuter's slice and outcome lines
refuted/<family>.md|.cob                 the refuter's corrected copies
verify/NN-<signature>/                   `verify`'s compiles
```

`prepare` rewrites inputs and briefs and never touches a verdict, record, refutation or report file, so a re-run after a template
fix resumes. The briefs' file names are constants of the tool (`ADJ_BRIEF`, `REFUTE_BRIEF`); the Workflow script spells the
same names and the self-test holds the two equal.

### 3.3 Rescoring the output differences

`OUTPUT_DIFFERS`, `EXIT_DIFFERS` and `THEY_FAIL_RUN` rows are rescored from the RECORDED outputs (no re-run): NUL bytes read as
spaces and trailing blanks are stripped per line (the harness's normalization); an equal pair with equal exit codes is `AGREE`
and dropped, an equal pair with different exit codes `EXIT_DIFFERS`, otherwise the first four differing lines are kept. This is
sound exactly when the judge is the build the run used, which is why a morning whose main has moved passes `--fresh-judge` and
the adjudicators re-run the candidates on it (`FIXED-SINCE`).

### 3.4 Slices

An adjudicator reads one slice (a signature is the unit; the first program of each is read in full):

| Slice | Input | Method |
|---|---|---|
| `A<n>` `B<n>` | rescored output differences grouped by (class, clause), most rows first; clauses `8.x` to A, the rest to B; 25 signatures a slice | `method-AB.md` |
| `C<n>` | `report.md`'s `WE_REJECT` sections: every signature of 3 rows or more, and a smaller one carrying a diagnostic code no larger signature carries | `method-C.md` |
| `D<n>` | `THEY_REJECT`, `THEY_FAIL_RUN`, `THEY_HANG`, `BOTH_HANG` sections, plus `BOTH_HANG` rows no section lists | `method-D.md` |
| `N1` | `WE_HANG`, `WE_FAIL_RUN`, `WE_CRASH`, `EXIT_DIFFERS`, `RUNNER_ERROR`, `NO_PROGRAM` sections, rows no section lists, the runner-error tail | `method-N.md` |

`report.md` prints a signature's first dozen programs and an ellipsis; each slice states ALL of them, taken from `results.jsonl`.
`slices.json` records every slice's signature list, which `collect` compares with the verdict files to name the undecided ones.

### 3.5 Briefs

`scripts/fuzz_triage_briefs/` holds the templates (`common.md`, `slice.md`, `method-*.md`, `refute.md`) with `<<NAME>>`
placeholders; `render_template` fills them and an unknown name is an error. Facts come from the run (`run.json`, the program
count), the tool (REPO, the WSL spelling of the triage path, the roles' turn caps read from `.claude/agents/`, the
coordination directory's stop files: the owner's global `STOP` and the fleet's `STOP-<run id>`) and the evidence folder (the
earlier nights' records, listed by glob). **A lesson a night's triage learns is written into a template**, so every later night
reads it. Kept from the earlier nights verbatim in spirit: look for the MORE SPECIFIC rule than the one cited and refute the
compiler's own diagnostic argument before calling a refusal wrong (night 5); one directory per probe program, never `--run`;
`cite.py --check` for every clause; read a signature's first program in full and sample the rest only for a candidate defect.

## 4. Adjudication and refutation

### 4.1 The verdict line

Each adjudicator appends one JSON line per decided signature to `adj/<S>/verdicts-<S>.jsonl`:
`signature`, `count`, `verdict`, `harm`, `rule`, `note`, `repro`, `code_site`, and for `GNUCOBOL-SIDE` an `upstream` object
(§5.1). The vocabulary is `WISEOWL-CORRECT`, `GNUCOBOL-SIDE`, `LATITUDE`, `UNINITIALIZED`, `NOT-COMPARABLE`, `KNOWN`,
`FIXED-SINCE`, `WISEOWL-DEFECT`, `MODEL-ERROR`, `UNDECIDED` (`common.md` and `fuzz_triage.VERDICTS` are held equal by the
self-test). `collect` refuses a verdict outside it, a `WISEOWL-DEFECT` without `repro`, `harm`, `rule` and `code_site`, a
`GNUCOBOL-SIDE` without a valid `upstream`, and a family claimed by two verdicts.

### 4.2 The Workflow

`.claude/workflows/fuzz-triage.js` is thin: it runs `cobol-adjudicator` agents (args `adjudicate`) and `cobol-refuter` agents
(args `refute`) a few at a time, each from its brief, and returns their reports. It reads no file and makes no decision: which
slices exist, what the briefs say and which records go to a refuter are the tool's, tested in `scripts/test_fuzz_triage.py`.
It cannot run a command, so the operator runs the tool between the two phases:
`prepare` → Workflow (adjudicate) → `collect` → `refute-plan` → Workflow (refute) → `file` on a branch.

### 4.3 The independent refuter

`refute-plan` slices the `GNUCOBOL-SIDE` verdicts whose disposition is `record` and that have no refutation line yet (six
to a slice), and renders `briefs/refute-<R>.md`. The refuter (`cobol-refuter`, read-only, default refuted) re-runs each record on
cobc, tries to overturn it (legal under the edition the `-std` claims? a more specific rule? latitude? a harness artifact? does
the rule exist in the claimed edition?), re-checks every citation, and appends `{"family", "outcome", "class", "edition", "citations", "why"}`
to `refute/<R>/refute-<R>.jsonl` with outcome `UPHELD`, `RECLASSIFY` (to another GnuCOBOL-side class or edition) or
`OVERTURNED`. For the first two it writes the FINAL record to `refuted/<family>.md`, every correction applied in place in every
section it touches, and the line's `citations` (`{clause, text}`, required for these two) are the clauses of THAT corrected copy:
the refuter may cite a more specific rule than the adjudicator did, and the filed note is the corrected copy. Tier 3 of PB3838 measured why the step exists: 12 RECLASSIFY and 1 OVERTURNED of 113.

## 5. Filing GnuCOBOL's side

### 5.1 The `upstream` object of a `GNUCOBOL-SIDE` verdict

`disposition` is one of:

- `record`: the adjudicator wrote `records/<family>.md` and its minimal program `records/<family>.cob`. Fields: `family` (a
  lowercase slug; a second mechanism is `<family>--<suffix>`), `class`, `edition`, `version`, `citations` (a list of
  `{clause, text}`), `record`, `repro`, `draft_title`, `programs` (the run's ids). The record's `##` sections are Summary and the
  bar's eight items (PB3838): Minimal test case, Environment, Observed result, Expected result (per ISO), Our compiler, Fix
  site, Upstream state, Draft issue.
- `witness`: an existing `upstream` note already records the mechanism: `note` and `programs`.
- `not-filed`: GnuCOBOL's side but the bar cannot yet be met: `reason`. It stays in the triage report.

### 5.2 What `file` requires

For every `record`: a refutation line (§4.3) whose outcome is `UPHELD` or `RECLASSIFY` (the refuter's class and edition then
replace the adjudicator's) and the refuter's corrected copy; the Minimal test case section carries a fenced block EQUAL to the
repro file (the program is copied OUT of the git-ignored run directory into the note); the Environment names the `cobc`
command; the Draft issue opens with its `**Title:**` line; every entry of the REFUTATION LINE's `citations` passes `cite.py`'s own
check (the words are inside that clause), the `§<clause>` citations written in the corrected copy's Expected result and Draft
issue are exactly that list, and each listed clause is named in the Expected result (so `spec_refs`, derived from the refutation
line, never contradicts the body); and the resulting note passes the register's own `work.upstream_problems` (the structured fields, the eight
sections non-empty, the 60-line fence cap that keeps GPL testsuite text out, no unapplied "this paragraph governs" correction,
an edition-2023 behaviour only as `feature-missing`). A record that fails any of these is HELD in `<triage>/filing.md` with
the reason and is never filed half (PB3838 standing requirement 1); an `OVERTURNED` or unrefuted record is held likewise.

### 5.3 Deduplication

A family an existing `upstream` note already records (the slug after the note id in its first heading), and every `witness`
verdict, adds the run's program ids to that note's `## Witnesses` section (`- <run id>: fz…`, ids only) and files nothing. There is
one bullet per run: a second signature of the same note in the same run appends the ids the bullet lacks, and ids already there
are not repeated, so a second `file` is a no-op.

### 5.4 The note

`render_note` writes the register's frontmatter of an `upstream` record (`process_only: true`, no cluster, tier or claimed row,
`related: [PB3838]`, the harm flag of the class, `spec_refs` from the refutation line's checked citations, `upstream_status: unreported`) and the record's
sections in the bar's order, then `## Witnesses`. Ids come from `scripts/orchestrator/alloc.py pb <n>` only, called once for the
count of notes to write, and only under `--write`. Every note is rendered and checked before the first file is written, so a
refusal leaves the register as it was. The operator then runs `python scripts/spec/work.py check`, commits the notes and
lands the branch; filing the notes upstream (tier 6 of PB3838) stays owner-gated.

## 6. The cumulative history and its pages

The owner asked on 2026-10-28 for a running summary of every night, published as an Artifact, written for a reader new to the
project and "maintained automatically as an artifact of each fuzz testing run", then for the same summary at the repository root
"so I can easily link to it and track readers" (kb/Work PB4006). Three pieces make it:

- **The history,** `conformance-suite/fuzz-history.json`, one record per night, tracked because the run directories are
  git-ignored and the triage directories are coordination scratch. It is not under `docs/.../evidence/`, whose JSON files are
  frozen records (the EVIDENCE SUPERSESSION audit holds each to a `_frozen` banner): this file grows every night. A record holds
  the run's own numbers (run.json, including its hours and its count of seed sections; report.md's headline and survivor line;
  the harness classes of results.jsonl), the adjudicators' verdict tally (signatures and programs per verdict) and the ids of
  the notes the night filed on the WiseOwl side. It never stores a status or a classification. The file also carries
  `artifact`, the one Artifact URL the HTML page is published to (https://claude.ai/artifact/UBCptoMp1GohHrvpUFRS7g).
- **The notes of a night,** by ONE rule (`fuzz_history.defects_of`): every defect or adjudication note whose `source` names the
  run id. What each note IS is read from the register when a page is drawn (`finding_kind`): a settled question (`kind:
  adjudication`), a retired finding, or a defect, which is a compiler bug when its declared sites (`work.note_sites`, the
  register's one reader of them) reach the product's code under `src/` and a test-harness bug when they do not. Neither the
  area nor `process_only` decides it: harness defects are filed under `tests/`, `process/`, `orchestrator/`, `scripts/` and
  `ci/` areas as well as `tooling/`, and a wrong diagnostic is `process_only` yet a compiler bug (kb/Work PB4010). A site
  block counts only when it names a path, a glob or `none (why)` (`none` reads as harness), the bar `work.sites_problems`
  holds open notes to; a block naming only types or members classifies nothing. The 16 fuzz defects filed before site blocks
  were required are a closed record in the code (`LEGACY_KINDS`), never extended. Any other defect nothing classifies stops
  the render and `history` refuses it, by name: no note is guessed into either count. A note whose `source` names a fuzz run the history holds no night for (a run that
  stopped early) is counted too and listed as "Run of <date>" (`unrecorded_runs`). Only compiler bugs reach the totals; the
  per-night chart holds the nights; the table lists all four kinds. A GnuCOBOL record counts when the fuzzing produced it (its
  `source` names the fuzz triage or the harvest plan PB3838) and it is not retired (`from_fuzzing`).
- **Two renderings of one text, written together,** by `scripts/fuzz_history.py`'s one writer `write_ledger` (`render`, and
  `history --write`): the HTML page for the Artifact (`<coord>/scratch/fuzz-summary.html`, or `--out`) and `FUZZ-LEDGER.md` at
  the repository root with its chart files in `docs/fuzz-ledger/*.svg`, which GitHub renders in place. There is no way to
  write one without the other. GitHub never renders a committed `.html` file (its viewer shows the source and its raw host serves
  `text/plain`), so the root copy is Markdown; the HTML is served by the Artifact. The prose (INTRO, STEPS, CAPTIONS, HEADINGS),
  the tables and the SVG charts are each written once and drawn by both, so the two cannot drift. Each states that it is
  machine-generated and names the script and the history file. Every fact either states comes from the records: the hours and
  section count from run.json, the nights without a tally from the records, every status from kb/Work.
- **The Artifact and FUZZ-LEDGER.md stay in sync** (owner 2026-10-28: "The fuzz ledger repo file must be kept in sync with the
  artifact"; kb/Work PB4010). Both carry one LEDGER VERSION in their stamp: a digest of the content alone (the Markdown and its
  charts drawn with an empty stamp). Neither carries a build time, so equal content is equal bytes and a render that changes
  nothing leaves FUZZ-LEDGER.md untouched. The Artifact is the history's `artifact` (the one FUZZ-LEDGER.md links) and is
  published from MAIN's FUZZ-LEDGER.md only: after a publish, `python scripts/fuzz_history.py mark-published [--page <html>]`
  fetches main, refuses a page whose version is not the version FUZZ-LEDGER.md carries there, and records the URL and version in
  `<coord>/fuzz-ledger-published.json` (one record: the Artifact belongs to the account that published it, which is the only one
  that can update it). `fuzz_history.py status`, printed by session-probe every session, says `current`, `PUBLISH OWED` (the
  record shows another version than main's file, or another Artifact) or `REGENERATE OWED` (main's file carries no version, or
  differs from a render now: a fuzz note's status moved, or the file was edited by hand; a render lands on a branch, then the
  publish). That last answer is MAIN's own: `main_verdict` extracts origin/main's committed `scripts/`, `kb/Work/`, history and
  FUZZ-LEDGER.md with `git archive` into a temporary directory and runs that copy's `fuzz_history.py verify`, which renders its
  own tree and compares it with its own FUZZ-LEDGER.md. No file of the checkout running `status` reaches it: not an edit, not an
  untracked note, not a stale script, and a checkout behind main reports main's state (the PB2987 lesson). Four review rounds
  found a working-tree check one input short each time (a dirty input, work.py, coord.py and model_rules.json, an untracked
  note); reading main's committed tree has no list to keep. The extract costs seconds, so it runs only when the Artifact already
  shows main's version.

**The step.** The last step of every morning, after `file --write` and on the same branch: `python scripts/fuzz_triage.py history
--triage <dir> --judge-commit <sha> --write`. It refuses a triage that `collect` does not pass (a problem or an undecided
signature) and a report.md whose headline it cannot read, finds the night's notes by the rule above (`--defect` adds one whose
source does not name the run, and every id must be a note), and records the night. A re-run keeps the night's number, its stored
notes, its judge and its evidence path, and replaces the rest. It then writes the HTML page to `<coord>/scratch/fuzz-summary.html`
and `FUZZ-LEDGER.md` with `docs/fuzz-ledger/` into the worktree, one ledger version. The session commits the history, the
ledger and its charts with the night's filing and lands them; once they are on main it publishes the HTML file to the
`artifact` URL with the Artifact tool, the only publisher (a script cannot publish), and runs `mark-published`.

**Backfill.** Nights 1 to 7 were recorded on 2026-10-28 from their run directories and, for nights 3 to 7, the verdict files that
survived in the triaging sessions' scratch. Every night's notes were then rebuilt by the one rule (the first backfill's
hand-copied lists missed PB3523, PB3569 and PB3570; branch review R1-1). Nights 1 and 2 were triaged before verdict files existed,
so their records carry no tally (`verdicts: null`) and the pages mark them "not itemized". Night 1's report.md was regenerated
after PB3490 changed the classes, so its survivor counts are that file's, not the first-night record's.

## 7. Tests and drift

`scripts/test_fuzz_undefined.py` proves `undefined_results` shape by shape, each program that must be found beside the nearest one that must not (the three storage shapes of PB3468 class 2, the statements that may have written an item, REDEFINES and EXTERNAL, the subscript and incompatible-data shapes, and the abstentions); `differential_fuzz.py --self-test` proves the class (`classify`, the row's `undefined`, `rederived`, the report heading).
`scripts/test_fuzz_triage.py` (discovered by `scripts/self_tests.py`; it needs no build or git) drives the tool on a fabricated
run and proves the arms listed in its docstring: rescoring, slicing, the briefs (no unfilled placeholder, no night-specific
literal, the vocabulary, dispositions and bar sections held equal to the tool's, the Workflow script's brief names held equal to
the tool's), the verdict rules, the refute plan, and every reason `file` holds a record, the witness path, the no-op re-file and
the write-nothing-on-refusal rule. `scripts/test_fuzz_history.py` proves the history and both pages, arm by arm as its docstring lists them: a run's numbers (and the refusal of an unreadable report), the tally, the one notes rule and the four kinds, which GnuCOBOL records count, the upsert, the HTML page and FUZZ-LEDGER.md with standalone chart files, the sync of the two (one writer and one ledger version, equal content as equal bytes, `mark-published`'s refusal and record, every `status` state), the `history` subcommand's refusals (an unclassifiable note anywhere on the page writes nothing), numbering, re-run and dry run, and the verdict colours held equal to the verdict vocabulary.

## 8. Alternatives rejected

- **One script per job (the earlier nights' shape).** Each night copied the previous night's scratch set and retargeted it by
  hand; a lesson survived only if the next session found it, and a scratchpad's end lost the tooling.
- **The Workflow deciding.** A Workflow script is untested JavaScript with no file access; every decision lives in the tool.
- **Filing straight from the adjudicator.** The tier-3 measurement (113 verdicts, 13 corrected) is the reason for the refuter.
- **A `GNUCOBOL-SIDE` summary table in the evidence document only.** That is the prose form PB3838 replaced: nothing in it can be
  filed upstream.
