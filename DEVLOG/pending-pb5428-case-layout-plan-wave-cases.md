## PB5428: the case layout lands, and plan_wave's 3,742-line self-test becomes 233 cases under `scripts/selftests/`

PB3726's second step (DESIGN-orchestrator-loop.md section 16.2). A script's tests now live in
`<root>/selftests/<path>/<name>/`, as a `fixtures.py` and `test_<world>.py` files of `case_<name>(expect)` functions that end
in `selftest_cases.run(...)`.

**The mechanism.** `scripts/selftest_cases.py` is the collector (`run`, `Expect`, `subject`): cases run in name order, an
exception or a case with no expectation is a FAIL, a module with no case is refused, and `subject(__file__)` imports the
script the file's place names. `scripts/selftest_layout.py` holds the one path rule (`subject_of`, `tests_dir_of`), the
refusals (a duplicate `case_` name, a `test_x` function, a module-level `expect(`, an out-of-order case, a file that is
neither `fixtures.py` nor `test_*.py`, a directory whose subject is missing or still handles `--self-test`, a module outside
`selftests/` importing one) and the `REMAINING` ratchet: the measured set is 162 Python files (127 that handle
`--self-test`, 27 standalone `test_*.py` and the 8 `docchecks` delegates), one path per line. `self_tests.py` calls the rule
before it runs anything (a refusal is `=== SELF-TESTS: RED — layout: … ===`), starts every file under a `selftests/` tree
with `scripts/` on `PYTHONPATH` in `Runner.run_one`, and takes `--only <path> ...`.

**The readers.** `inert_lane._imports` takes two edges for a file under `selftests/`: `scripts/` is a directory it
resolves against, and `selftest_cases.subject(__file__)` is an import of the subject, so a case file's closure holds the
subject's own imports (planted case in `inert_lane.py`'s self-test). `scratch_repo._audit_tree` reads every file under a
`selftests/` tree as whole self-test scope, so a `fixtures.py` that runs `git init` itself is RED (planted case in
`scratch_repo.py`'s self-test). `self_test_arms.findings` reads a converted subject's arms as `case_` names of its tests
directory. `script_layering.py` needed no edge: it reads declared modules only and a case file declares none; section 16.2
now says so.

**The conversion.** `scripts/orchestrator/test_plan_wave.py` is deleted. An AST-driven one-shot (kept out of the tree) split
it into `fixtures.py` (the prologue: imports, helpers, `plant()` building the scratch world) and `test_planner.py` (233
`case_NNN_<slug>` functions, one per comment-led block, in the file's original order; module-level names that later blocks
read are `global` in the case that sets them). Oracle: the first argument of every `check(` call of the old file and of every
`expect(`/`expect.equal(` call of the new one, by a syntax-tree walk, are equal as multisets (507 each, 502 distinct; the note's
"416" was stale); the old run printed `552/552 checks OK` and the new `PASS (233 cases, 552 expectations)`; the golden plan
`testdata/plan_wave_default.json` is untouched; the headers (`SELF-TEST-POPULATION: tree`, `SELF-TEST-CLOCK: real`)
travelled to `test_planner.py`, which the runner passes at both the real and the shifted clock.

**What this did not do.** The blocks of the old script share one world (the notes in `WORK`, `UN`, `DR_*`, `cl`, `gjson` and
others are read hundreds of lines after they are built), so the cases are one world file in order, not the several world files
section 16.2 describes; splitting it needs each block's reads of earlier state made explicit, which is a rewrite of the
fixtures, not a move. The case order is therefore load-bearing for this one file.

**Case dirs for the new modules:** `scripts/selftests/selftest_layout/test_refusals.py` (every planted breach RED, the clean
tree not, `REMAINING` sorted one per line and equal to the measured set) and `scripts/selftests/selftest_cases/test_collector.py`
(the collector's arms, `subject`'s refusals and a case file run bare failing at its import). `self_tests.py` gained arms for
the layout RED verdict, `--only` and the `PYTHONPATH` of a case file.
