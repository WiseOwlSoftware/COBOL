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

The verdict classes (`differential_fuzz.py#classify`): `BOTH_REJECT`, `NO_PROGRAM` and `BOTH_HANG` are the model's failures;
`AGREE` and `OUTPUT_LATITUDE` are dropped; `THEY_REJECT`, `WE_REJECT`, `OUTPUT_DIFFERS`, `WE_CRASH`, `WE_FAIL_RUN`,
`EXIT_DIFFERS`, `WE_HANG`, `THEY_HANG`, `THEY_FAIL_RUN` are leads; `RUNNER_ERROR` is never a compiler verdict.

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
| `cites` | check every clause the verdicts cite exists, and every `upstream.citations` quote with `cite.py`'s own check |
| `verify` | re-run each `WISEOWL-DEFECT` repro on the fresh judge, one directory per program |
| `refute-plan` | slice the `GNUCOBOL-SIDE` records for the independent refuters and render their briefs (§4.3) |
| `file` | file what the refuters upheld as `upstream` notes (§5); a dry run unless `--write` |
| `render` | re-render the briefs of a prepared triage (after a template edit) |

### 3.2 The triage directory

`<triage>` (default `<run>/triage`, inside the git-ignored run directory):

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
the rule exist in the claimed edition?), re-checks every citation, and appends `{"family", "outcome", "class", "edition", "why"}`
to `refute/<R>/refute-<R>.jsonl` with outcome `UPHELD`, `RECLASSIFY` (to another GnuCOBOL-side class or edition) or
`OVERTURNED`. For the first two it writes the FINAL record to `refuted/<family>.md`, every correction applied in place in every
section it touches. Tier 3 of PB3838 measured why the step exists: 12 RECLASSIFY and 1 OVERTURNED of 113.

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
command; the Draft issue opens with its `**Title:**` line; every `citations` entry passes `cite.py`'s own check (the words are
inside that clause); and the resulting note passes the register's own `work.upstream_problems` (the structured fields, the eight
sections non-empty, the 60-line fence cap that keeps GPL testsuite text out, no unapplied "this paragraph governs" correction,
an edition-2023 behaviour only as `feature-missing`). A record that fails any of these is HELD in `<triage>/filing.md` with
the reason and is never filed half (PB3838 standing requirement 1); an `OVERTURNED` or unrefuted record is held likewise.

### 5.3 Deduplication

A family an existing `upstream` note already records (the slug after the note id in its first heading), and every `witness`
verdict, adds the run's program ids to that note's `## Witnesses` section (`- <run id>: fz…`, ids only, idempotent per run id) and
files nothing.

### 5.4 The note

`render_note` writes the register's frontmatter of an `upstream` record (`process_only: true`, no cluster, tier or claimed row,
`related: [PB3838]`, the harm flag of the class, `spec_refs` from the citations, `upstream_status: unreported`) and the record's
sections in the bar's order, then `## Witnesses`. Ids come from `scripts/orchestrator/alloc.py pb <n>` only, called once for the
count of notes to write, and only under `--write`. Every note is rendered and checked before the first file is written, so a
refusal leaves the register as it was. The operator then runs `python scripts/spec/work.py check`, commits the notes and
lands the branch; filing the notes upstream (tier 6 of PB3838) stays owner-gated.

## 6. Tests and drift

`scripts/test_fuzz_triage.py` (discovered by `scripts/self_tests.py`; it needs no build or git) drives the tool on a fabricated
run and proves the arms listed in its docstring: rescoring, slicing, the briefs (no unfilled placeholder, no night-specific
literal, the vocabulary, dispositions and bar sections held equal to the tool's, the Workflow script's brief names held equal to
the tool's), the verdict rules, the refute plan, and every reason `file` holds a record, the witness path, the no-op re-file and
the write-nothing-on-refusal rule.

## 7. Alternatives rejected

- **One script per job (the earlier nights' shape).** Each night copied the previous night's scratch set and retargeted it by
  hand; a lesson survived only if the next session found it, and a scratchpad's end lost the tooling.
- **The Workflow deciding.** A Workflow script is untested JavaScript with no file access; every decision lives in the tool.
- **Filing straight from the adjudicator.** The tier-3 measurement (113 verdicts, 13 corrected) is the reason for the refuter.
- **A `GNUCOBOL-SIDE` summary table in the evidence document only.** That is the prose form PB3838 replaced: nothing in it can be
  filed upstream.
