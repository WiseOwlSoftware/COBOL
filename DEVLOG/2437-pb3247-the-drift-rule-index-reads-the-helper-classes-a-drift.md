## Entry 2437 — 2026-10-23 21:28 PDT — PB3247: the drift-rule index reads the helper classes a drift test reads the tree through

The gate's tier 0d plans every drift test that `scripts/spec/drift_rules.py` says governs a changed file. A test that
read a file only through a shared helper class was invisible to the index, so a branch changing that file gated GREEN
and only the landing's whole-population Linux leg found the red (the CI invariant, cluster PB1957).

**Re-probe (base 24e337272).** Two of the note's three witnesses no longer reproduced. PB3531's tier 0d already takes
`drift_rules.governed` from the diff with or without an impact map. `traceability-inventory.json` names
`SpecTraceabilityInventoryDriftTests`, and `docs/CONFORMANCE.md` names `AnnexA1RegisterDriftTests`. The third still
reproduced: for a conformance golden, `drift_rules.py` reported "0 specific drift rule(s)" and named none of the
classes that scan the corpus. `ConstantNameLiteralPositionDriftTests` (28 s recorded) and nine others read the corpus
through `SuiteManifest` and name no path.

**Sibling sweep.** The same mechanism had three more arms:
- **kb/Work notes.** `ClosesRowsBackLinkDriftTests` and `DefectiveRowCoverageDriftTests` read the register through
  `WorkRegister`, and the index's hand-copied table of TestRepo's helpers lacked `Kb`. So a note, which nearly every
  landing edits, governed nothing but the conflict-marker sweep.
- **Non-literal arguments.** `TestRepo.Kb("Work", note + ".md")` was read as the file `kb/Work/.md`.
- **`Path.Combine(TestRepo.Root, ...)`.** 24 such reads in 16 drift tests were never read at all.

**Fix, in `scripts/spec/drift_rules.py`:**
- `Sources` reads every top-level type a drift test uses by name: a member access, `new`, `typeof`, a base type or
  `using static`. Comments do not count, and only types in the test's own project or `tests/_shared` do. The test then
  governs, transitively, every path those helpers name (`Sources.inherited`), and the per-file query prints
  `— through <helper>`.
- Inherited paths govern a changed file but never decide `is_sweep`. The closure is file-grained, so a literal added to
  one helper must not move the `Sweep` trait of every class that uses it. The sweep set is unchanged by inheritance.
- TestRepo's path helpers are read from `tests/_shared/TestRepo.cs` (`test_repo`); the copied table is deleted.
  `--check` fails on a helper in a shape the index cannot read.
- `TestRepo.SuiteCase("id")` and `SuiteFile` resolve a literal id to its program, which they then govern specifically.
- A helper call's path is its leading string literals, and a collection expression counts as its elements.
- `Path.Combine(TestRepo.Root, ...)` counts as the root helper. Seven classes that scan a whole tree that way are
  sweeps and now carry `[Trait("Sweep", "tree")]`: EndMarkerRule, EvaluateOperandCombinations, OdoDependingAdmission,
  ProgramNameScope, RenamesPlaceBuilder, SubscriptAdmission and Table12StatementName. OdoDependingAdmission had its
  trait on a method, where the planner never sees it.

**Measured effect** (governed classes before → after): a golden 8 → 18, `conformance-suite/wiseowl/diagnostics.json`
10 → 19, a kb/Work note 1 → 4, `docs/CONFORMANCE.md` 25 → 29, `specs/ISO_COBOL.md` 29 → 31, the inventory 19 → 21.
No file lost a governing class. Building the index takes about 0.7 s instead of 0.2 s.

**Witnesses.** `drift_rules.py --self-test` is new. It plants a tree and fires each arm: Kb derived, a two-step helper
chain, a helper from another project ignored, a comment ignored, a literal suite id resolved, leading literals and
`Path.Combine`, inherited paths never making a sweep, and an unreadable helper reported. `impacted_tests.py
--self-test` gains a check that, with no map, a change to each of the note's three files and to a kb/Work note names
the drift test that went red on it. Docs: DESIGN-test-build-ci.md tier 0d, MANDATORY-PRACTICES P11, the gate skill and
gate_plan.py's tier docstring.

**Self-review.** Architecture: one index, and the gate still asks only `governed`. Duplication: the copied helper table
was a second copy of TestRepo, and it is gone. Performance: each source is read once and each use scan is memoized.

**Gate** (run 000001-20261024T015348Z, head 84c6d1b09; leg 1 ran 205 Unit and 24 Conformance tier-0d cases, among them
HostWitnessTraitDriftTests and TrackedTreeScopeDriftTests): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 8,336 · Unit 1,256 · Characterization 31`.

