## PB3972: the fuzz triage is one tool in the tree, and an upheld GnuCOBOL-side verdict files an `upstream` note

Group A of wave 20261731, PB3838's tier 5. Seven nights of morning triage had run from the writing session's TEMP
scratchpad (`residue.py`, `slice.py`, `render.py`, `collect.py`, `verify.py`, `retarget.py`, `save_reports.py`,
`reports_from_journal.py`, `cites.sh`, the briefs and `wf.js`), copied and retargeted by hand each night, and a
GNUCOBOL-SIDE verdict filed nothing.

**Design first.** No document covered the differential fuzz. `docs/rearchitecture/DESIGN-differential-fuzz.md` now does:
the night (wrapper, harness, judge, run directory), the morning (§3: rescore, slice, briefs), adjudication and the
independent refuter (§4) and the filing (§5). It is registered in `docs/DOC_INDEX.md` and `docs/DESIGN.md` §0.5.

**The tool.** `scripts/fuzz_triage.py` takes `--run` and `--judge` (default run.json's `cobol_exe`) and writes everything
under `--triage` (default `<coord>/scratch/fuzz-triage/<run id>`, outside every git checkout); no run id, date, judge path or TEMP path is written in it, the templates or the
Workflow (the self-test greps for them). Subcommands: `prepare` (rescore from the recorded outputs, slice, render),
`collect` (validate every verdict line, name the undecided signatures), `cites`, `verify`, `refute-plan`, `file`, `render`.
The briefs are templates in `scripts/fuzz_triage_briefs/` with `<<NAME>>` placeholders and an unknown name is an error;
night 5's lesson (look for the more specific rule, refute the compiler's own argument), the per-program directory, never
`--run` and `cite.py --check` for every clause are kept. The roles' turn caps are read from `.claude/agents/`, the stop
files from `coord.py`. Four changes against the hand-run set: `report.md` lists only a signature's first dozen programs, so
every slice now states ALL of them from `results.jsonl`; rows no report section lists (BOTH_HANG, RUNNER_ERROR, NO_PROGRAM)
are synthesized only when no section lists them; the data/expression split (clauses 8.x) is a named constant; `save_reports`
and `reports_from_journal` are gone because the briefs have each agent write its own report file.

**Re-probe.** `prepare` on the sixth night's run reproduces the hand-run slices exactly: A1, B1-B4, C1-C4, D1-D2, N1 with
85, 292/114/39/13, 832/105/43/20, 190/22 rows. `existing_families` reads the 65 filed notes (PB3873-PB3937).

**Filing GNUCOBOL-SIDE.** A `GNUCOBOL-SIDE` verdict carries an `upstream` object: `record` (a record to the bar, its repro,
checked `citations`), `witness` (an existing note) or `not-filed` (the bar cannot be met yet). `refute-plan` slices the records
for `cobol-refuter` agents, who write a corrected copy of every upheld record with the correction applied in place.
`file` writes a note only for an UPHELD or RECLASSIFY record (the refuter's class and edition replace the adjudicator's)
that meets the register's own `work.upstream_problems` bar, whose minimal program is a fenced block equal to the repro
file (copied out of the git-ignored run directory), whose Environment names the cobc command, whose Draft issue opens with
its title, and whose every citation passes `cite.py`'s own check. Everything else stays in `<triage>/filing.md`, never filed
half. A family an existing note records, and a `witness` verdict, add the run's program ids to that note's Witnesses
(idempotent per run). Ids come from `alloc.py pb <n>` only, and only under `--write`; every note is rendered and checked
before the first file is written. Nothing is posted anywhere.

**The Workflow** `.claude/workflows/fuzz-triage.js` is thin: it runs the adjudicators and the refuters four wide from the
tool's briefs and decides nothing. The self-test holds its brief names equal to the tool's constants.

**Tests.** `scripts/test_fuzz_triage.py` (discovered by name, no `--self-test` flag on the tool, which `self_tests.py` would
run twice) drives a fabricated run through rescoring, slicing, the briefs, each verdict rule, the refute plan and each reason
`file` holds a record, the RECLASSIFY path, the witness path, the no-op re-file and the write-nothing-on-refusal rule. It is
green (`SELF-TEST: PASS`).

**Branch review round 1.** The reviewer found four blocking defects and a lead; each is fixed with its own self-test arm.
(R1-1) The default triage directory sat beside the run, inside the git working tree, where `readonly_repo.py` refuses every
Write of the read-only adjudicators and refuters: the default is now `<coord>/scratch/fuzz-triage/<run id>` and
`triage_directory` refuses any directory inside a checkout with `coord.scratch_problem` (the self-test pipes a write to the
real hook). (R1-2) The filed note is the refuter's corrected copy, but only the adjudicator's citations were checked: the
refutation line now carries `citations`, `file` and `cites` check those, `spec_refs` come from them, and the `§` clauses the
corrected copy writes must equal them. (R1-3) `add_witness` dropped the second signature's ids of the same note in one run: a
run has one bullet and a later call appends the ids it lacks. (R1-4) The tool kept a second fence tracker, section lookup
and draft-title regex: it now asks `work.py` (`fenced_block_texts` over the shared `_scan_fences`, `section_named`,
`draft_title`), so the backtick-info-string rule and any later CommonMark fix apply to it. (R1-5, the lead, in the same file)
`verify` exits 1 for a missing repro or a compile past 120 s and goes on to the next repro. Self-review found the docstring
and design section 3.2 still naming the old default; both corrected.

**Rows.** PB3972 claims none and closes none (process tooling); its status is `landed` with the Sites paragraph naming the
briefs' templates and the docstring edit in `scripts/differential_fuzz.py`.
