## PB3190: the gate's leg 1 runs the test methods a change edits, not only the ones it adds

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
changed theory data source left its classes "already tier 0a", which the code did not do; now it does.

**Tests.** `cs_members.py --self-test` (11 arms: every string form, spans, data sources, nested types, a tuple return
type, hunk touch rules, the audit firing on an unbalanced file and on an orphan attribute). `impacted_tests.py
--self-test` gains seven arms over a real `git diff --no-index -U0`: an edited body line, a deleted body line, an added
`[InlineData]` row, a changed `[MemberData]` source, a changed helper (not tier 0a), a comment line (not tier 0a) and
an unbalanced source (every test). `ImpactedTestsDriftTests` pins them all by name.

**Design.** `DESIGN-test-build-ci.md` §3.14.2 (tier 0a) and §3.13 now say an edited test method is tier 0a by its own
span, never its file's; the M11 correction (a whole changed test file is not tier 0a) still stands. An edited harness,
such as a helper, a constructor or a data file a test reads, is still tier 1 through the map. That residue is reported
as a lead.

**Rows.** None: test-gate tooling, no ISO rule.
