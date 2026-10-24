## Entry 2441 — 2026-10-23 23:01 PDT — A landing's Linux reds are tier 0a of the worktree's next gate (PB3155, PB3184); PB3190: the gate's leg 1 runs the test methods a change edits, not only the ones it adds

### A landing's Linux reds are tier 0a of the worktree's next gate (PB3155, PB3184)

**The defect.** Tier 0a, "red at the previous gate", took its reds only from build-local's own previous run
(`gate_plan.read_previous_run`, fed by `run_gate_legs.Host.plan`). The landing's Linux gate (`scripts/linux-gate.sh`,
the row `enqueue.py land` runs after the Windows gate) leaves its reds in `TestResults/linux-gate/*.trx`, which nothing
read. So when a landing stopped on a Linux red, the finisher's Windows re-gate planned the failed cases by the ordinary
tier rules, left them in leg 2 and read `LEG 1 ONLY (batched gating, PB2515): GREEN`. w1076a (PB2921) ran
`NulByteDriftTests` and `StagedLoudResidue` by hand on the gate's build, and PB3165's finisher re-ran its Linux red
under WSL by hand. It is a CI-invariant leak path (PB1957): the one signal that names the red was dropped between the
two gates.

**Re-probe** (this worktree, before the fix). A `Failed` result planted in
`<worktree>/TestResults/linux-gate/unit.trx` and a listing holding it with 50 other cases: `gate_plan.py` planned it
tier 1, legs `[2]`. After the fix the same probe plans it tier 0a, in leg 1, and prints
`gate_plan: red keys (tier 0a) — linux gate …: 1; previous run: none: 0`.

**The fix: one reader of a worktree's previous gates, on both hosts.** `gate_plan.previous_gates(worktree, previous)`
returns the timings of build-local's previous run and the reds of every source by its label: that run's, and the Linux
gate's newest run (`<worktree>/TestResults/linux-gate/`, which linux-gate.sh empties before each run). The Linux run
gives reds only: another host's durations would mis-time this host's plan. The driver's `Host.plan` and the CLI
(`--worktree`, default the script's repository) both call it, with no shim over `--previous-run`. `build_plan` takes
the reds by source, and writes each source's count into the plan (`"reds"`) and prints it: its red keys, and how many
of them name a case discovered on this host. A red is matched by its name key alone, so one whose display name differs
between hosts (a theory argument that names a host path) is visible there and is not lost silently.

**The first gate was RED** (run `000001-20261024T015633Z`, self-tests): the `run_gate_legs.py` self-test's planted plan
had no `"reds"` field, which `summary_lines` now reads. The planted plan now carries it.

**The rule that keeps it true.** Every live tool that writes a trx file into a worktree is a row of
`gate_plan.TRX_WRITERS`: the gate (its previous run: timings and reds), the Linux gate (its newest run: reds) and the
battery (nothing: it runs in its own detached worktree at a batch head). The new
`GateLegDriftTests.EveryTrxWriter_IsARedSourceOfThePlan_OrSaysWhyNot` runs `gate_plan.py --audit-trx-writers`. It is
red when a live tool (`live_tools.ROOTS`) writes a trx file with no row, when a row's tool no longer writes one, or when
a row names a results directory its tool never writes. It also asserts that the audit still sees all three writers.

**Tests.** `gate_plan.py --self-test` has six new arms: no previous gate on either host; a passing Linux run adds no red;
both hosts' reds under their own labels; no timings from the Linux run; a Linux-only red, renumbered by a partition
change, is tier 0a in leg 1 and counted; and the writers audit's three findings on a planted repository.

**Docs.** DESIGN-test-build-ci.md §3.14.2 (tier 0a, and where reds come from), the gate skill, MANDATORY-PRACTICES I1
and plan §9 now say the reds come from either host. A comment at linux-gate.sh's results directory says the planner
reads it.

**Register.** PB3155 is landed, and PB3184 (the same mechanism, filed separately from PB3165's finisher) is landed with
it. Neither closes an inventory row (test-gate tooling).

**Siblings left open, by design.** A merge-queue bounce's reds live in GitHub's CI run, not the worktree. The Linux
gate's `selftests` and `guard` legs write no trx, and a self-test whose inputs changed re-runs at the next gate anyway.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 8,360 · Unit 1,369 · Characterization 31 … — Conformance 9,983/18,343 · Unit 34,455/35,824 · Characterization 5/36 cases ran (skipped 0) in 1 of 2 leg(s) … run 000002-20261024T021714Z) ===`; the new `GateLegDriftTests.EveryTrxWriter_IsARedSourceOfThePlan_OrSaysWhyNot` ran in
leg 1 and passed.

### PB3190: the gate's leg 1 runs the test methods a change edits, not only the ones it adds

**The defect.** Tier 0a of the gate's order plan holds the change's own tests, and under batched gating (leg 1 only)
it is the only promise that they run locally. It held only test methods the diff ADDED: `impacted_tests.reach_test_file`
compared the set of `[Fact]`/`[Theory]` method names before and after. A test that already existed and whose body the
diff changed was tier 1, budgeted like any other case (2 % of the assembly's recorded seconds and a 15 s collection
cap; every case tier 1 with no map), so a dear one ran only in leg 2 and its red surfaced first at the landing's Linux
gate or in CI. The PB3570 finisher hit it on 2026-10-23.

**Re-probe.** One character changed inside the existing `ImpactedTestsDriftTests.GatePlanSelfTest_DrivesEveryArm`, then
`impacted_tests.py --base HEAD --plan`: `tier0a` was `{'added_tests': {'Unit': []}}`. After the fix the same run names
the edited method in tier 0a.

**The fix.** `impacted_tests.tests_changed(text, hunks)` takes every test method whose lines (its attribute block,
declaration and body) a `-U0` hunk's new side touches, and every theory whose `[MemberData]` source a hunk touches,
since that member's values are its rows. A hunk of blank and comment lines touches nothing. A new file, or one named
without a diff, gives all its test methods. The member spans come from a new module, `scripts/spec/cs_members.py`.
Its lexer blanks comments, preprocessor lines, character literals and every string form (verbatim, interpolated with
its holes followed, raw), so the braces left are code. A member-level reader then splits containers from members and
handles block, expression and initializer bodies. A file whose braces do not balance once lexed falls back to all its
test methods. `impacted_tests.py --audit-members` holds the lexer to every source of the gated test projects, and a new
`ImpactedTestsDriftTests` fact runs it: CLEAN over 6,078 test methods, with no overlapping spans. `added_tests` is
renamed `changed_tests` in `Reach`, `gate_plan.Context`, the `--plan` JSON and every self-test, with no alias. Matching
stays by method name, so a partition class that inherits its tests from a family base still matches. The hunk tuple
became a `Hunk` named tuple carrying both sides, and `method_ids` reads it by field.

**Found on the way, fixed here.** The old regex missed every same-line test (`[Fact] public void X() => …;`: 11
methods in `CobolBoolTests.cs` and `TierCRejectionTests.cs`), so even an added one of that shape was never tier 0a.
It also counted test methods inside planted C# strings (`VacuousTestDriftTests.cs`). The comment in `reach_of` said a
changed theory data source left its classes "already tier 0a", which the code did not do; now it does. A replay
(`--head`) of a commit that deletes a test file crashed the analysis, because it read the file at the head where it
no longer exists (`19c9289af` replayed). A deleted file now names no test, and a self-test arm pins it.

**Branch review R1-1, fixed.** The first version decided "a hunk of blank and comment lines" with a line regex, so a
hunk inside a test's raw string, its embedded COBOL program, counted as a comment edit when its lines started `*` or
`//` or were blank, and the test stayed out of tier 0a. Moving the `*>` indicator line of
`ExceptionPerformSourceFormatScopeTests.AutoDetectedFixedForm_IsRestoredAtEndPerform` one column gave no edited test.
Now `cs_members.code_lines` answers which lines carry code: every line a string literal touches, a blank one included,
and any line with a character outside a comment. `parse_hunks` takes both sides' texts and flags each hunk line, and
`Hunk.trivial`, `method_ids` and `inserted_members` read the flags; the line regex is deleted. The method level had the
same leak: a comment-looking line deleted from a class-level raw string was not a declaration change. `parse_hunks` also
dropped a content line starting `--` or `++` as a file header, which shifted the line numbers of the rest of the hunk;
it no longer does.

**Tests.** `cs_members.py --self-test` (11 arms: every string form, spans, data sources, nested types, a tuple return
type, hunk touch rules, the audit firing on an unbalanced file and on an orphan attribute). `impacted_tests.py
--self-test` gains seven arms over a real `git diff --no-index -U0`: an edited body line, a deleted body line, an added
`[InlineData]` row, a changed `[MemberData]` source, a changed helper (not tier 0a), a comment line (not tier 0a) and
an unbalanced source (every test). `ImpactedTestsDriftTests` pins them all by name. The review fix adds a `code_lines` arm to `cs_members.py` and five arms to `impacted_tests.py` (a `*>` raw-string line, a blank line in a raw string, a deleted `--` verbatim line, and the method level's string-line and comment-line deletions), each red against the old code.

**Design.** `DESIGN-test-build-ci.md` sections 3.14.2 (tier 0a) and 3.13 now say an edited test method is tier 0a by its own
span, never its file's; the M11 correction (a whole changed test file is not tier 0a) still stands. An edited harness,
such as a helper, a constructor or a data file a test reads, is still tier 1 through the map. That residue is reported
as a lead.

**Rows.** None: test-gate tooling, no ISO rule.

