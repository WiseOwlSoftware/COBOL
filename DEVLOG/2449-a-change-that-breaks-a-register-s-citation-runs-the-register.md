## Entry 2449 — 2026-10-24 01:29 PDT — A change that breaks a register's citation runs the register's drift tests in leg 1 (PB3131)

kb/Work PB3131 (wave 20261026 group D, part of the PB1957 gate-overhead campaign). Four landings between 2026-10-14
and 2026-10-17 stopped in CI's Linux unit leg on `SpecTraceabilityInventoryDriftTests` after a Windows leg 1 that
never ran it. Each branch deleted a member that an inventory code-location names, or renamed a test that a test-ref
names, without touching the inventory. The class costs 4-6 s a case, over `SWEEP_CHEAP_SECONDS`, so tier 0c left it
in leg 2, and nothing tied it to the C# file the diff changed.

**Re-probe.** The note's first form, a diff that edits the register, was already closed by PB3531's tier 0d:
`drift_rules.py` lists the inventory's and `docs/CONFORMANCE.md`'s checking classes as specific rules of those files,
and a replay of 00a3735e5 (an inventory edit) puts them in leg 1. The second form still reproduced. A no-map replay of
611bdb3a2 (the PB2740 test rename) put 124 classes in tier 0d, and `SpecTraceabilityInventoryDriftTests` was not one
of them.

**Fix.** The drift-rule index answers a second question for the planner: which drift classes govern a register that
cites what the diff removes. `impacted_tests.removals` reads one `git diff -U0` and builds a `drift_rules.Removal` for
each changed file: a deleted file, the identifiers on removed lines that the new text lacks, and the types the file
declares. `drift_rules.citing` indexes each non-C# file a drift test names (91 files, 18 MB) once per plan, by the
three shapes a register uses to cite a symbol: a path, a `path#Member` chain, and a dotted `Class.Member` name. It
returns the classes that govern each citing register specifically, and `reach_of` adds them to tier 0d with a note
naming the broken citation. Nothing is listed by hand, so a new register is covered as soon as a drift test names it.
In no-map replays, all four landing stops now name the class and the citation they broke (611bdb3a2, 684d01b4b,
1a473341f, c3c6879ab). The citation step costs about 2-5 s per plan; a first version ran one regex per removed
identifier and cost 12-49 s.

**Tests.** `impacted_tests.py --self-test` gains nine arms: the three citation shapes, a deleted suite test, a
removal no register cites, a change that removes nothing, the diff reader (a removed `-- a/x` line is not a file header), the `reach_of` wiring with no map, and the real inventory.
Each arm that expects a citation fails when `Citations.broken_by` is stubbed out. `ImpactedTestsDriftTests` now pins these arms and PB3531's
two tier-0d arms, which it had not pinned. Docs: `DESIGN-test-build-ci.md` (tier 0d), the gate skill,
MANDATORY-PRACTICES I1, and the gate scripts' headers.

**Not covered.** The NIST-differential instance (a diff to `HostEntryEmitter.cs` with no impact map) is a code-reach
gap, not a register citation. It belongs to the PB2696 family and goes back as a lead. A removed identifier that still
appears elsewhere in its file, for example in a comment, is not seen.

**Branch review, round 1.** The reviewer replayed the six most recent file-deleting commits on main and found the
deletion arm far too loose (R1-1). Two causes. `removal_of` took a "type" from any `record`/`class` word in the
removed text, comments and strings included, so a deleted file whose doc comment said "a record is" made `is` a
citation. And a deleted file's stem matched any whole word, so deleting a `test.py` or `README.md` matched prose. One
deletion promoted up to 102 classes into tier 0d, which sits outside the leg-1 budgets. The finisher fixed both:
- A type is now a C# declaration at a line start (`TYPE_DECL`). Deleting one file of a `partial` type deletes no type.
- A deleted type breaks only a `Type.Member` dotted name.
- A deleted file breaks a path citation only when the citation, resolved against the tree before the change
  (`TreeFiles`), names no file the change keeps.

Five of the six commits now promote nothing beyond their governed classes. The sixth, cf9c85914, promotes the
inventory's classes on a real code-location of the file it deleted. The four landing stops still name their class.
The lead R1-2 is fixed too: a patch header whose path holds a space ends in a TAB, and a quoted one is C-unquoted.
Seven new self-test arms are pinned in `ImpactedTestsDriftTests`.

The PB3603 form, which main folded into this note during the review, was closed on main by PB3247 while this branch
waited to land: the drift-rule index now reads the helper classes a drift test reads the tree through, so a
kb/Work-only diff puts `ClosesRowsBackLinkDriftTests` and `DefectiveRowCoverageDriftTests` in tier 0d (measured on
the merged tree). The landing's merge of main kept both mechanisms. The branch's hand-extended `HELPERS` table gave
way to main's `test_repo`, which reads TestRepo's helpers from its source. The two type-declaration regexes became
one `TYPE_DECL`, and `data_files` reads registers reached through a helper, as `governing` does. All three forms
are closed, so the note is `landed`.

Gate: see the report (`w20261026d-PB3131-report.md`).

