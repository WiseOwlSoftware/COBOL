## Entry 2463 — 2026-10-24 11:05 PDT — PB3112: the drift-rule index reads every path a drift test spells, so a grammar edit runs the five grammar drift tests at the Windows gate

**Re-probe.** The note said the tier-0c cost cap keeps about 270 Sweep cases out of Windows leg 1, so a tree-wide rule
goes red first at the landing's Linux gate. Two landings had already closed that for the cases it named: tier 0h runs the
trait guard at every gate, and tier 0d (PB3531) runs every drift test the drift-rule index says governs a changed file,
whatever it costs. PB3247 then taught the index to follow helper classes (`SuiteManifest`), which is what the previous
implementer's report named as the remaining mechanism (w1129g's `ConstantNameLiteralPositionDriftTests`). I replayed the
35 Linux-red landing heads the reports record against today's index (`replay.py`): every drift-rule red is governed except
a lock race, a harness timeout, an apphost rebuild (PB3529) and `SpecTraceabilityInventoryDriftTests` (PB3131).

**What was still open.** The same shape one level down. The index read a call's path with a regex over arguments that held
no parenthesis, so `TestRepo.Src(Path.Combine("Cobol.Net.Frontend", "Grammar"))`, `Path.Combine(TestRepo.Root, GrammarDir)`
(a class constant), `Path.Combine("src", ..)` and a wrapper whose callers spell the path (`Read("src", "Cobol.Net.Compiler",
"X.cs")`) named nothing. 71 of the 391 drift tests had no read the index could place; 62 gained at least one when the
reading was fixed. Five scan the grammar (`ArithmeticOperandRoleDriftTests`, `SendingOperandDriftTests`,
`ReceivingOperandFormDriftTests`, `LevelNumberArmDriftTests`, `ClassConditionTableDriftTests`) and governed no grammar
file, so a grammar edit's red could first appear at the landing's Linux gate. `SourceEncodingDriftTests` read
`TestRepo.At(tree)` from a theory's parameter and governed nothing.

**The fix, at the root.** `scripts/spec/drift_rules.py` now reads a path with ONE evaluator (`path_expr`, `path_join`):
string literals, `TestRepo.Root`, class constants (`const string`, `static readonly string`; a local or a parameter is
another method's name and is never read, which a first draft got wrong: `string script = TestRepo.Scripts("arch", ..)` in
one method doubled the prefix of `TestRepo.Scripts("arch", script)` in another), collection expressions, nested
`Path.Combine` and `TestRepo.X` calls, with balanced parentheses, up to the first argument it cannot spell. A method that
wraps one read is read at its callers (`Sources.reads`), and its own directory read is replaced by theirs unless some use
of the wrapper (a method group, `nameof`) spells none; a method that only passes arguments on (`Go("--check")`) is no
wrapper. `drift_rules.py --check` now refuses a drift test that reads the repo and spells no path (`blind`), so the index
cannot silently miss a whole class again; `SourceEncodingDriftTests` and `GateLegDriftTests` spell their trees with
literals. No hand table: a new reader is followed the day it is written.

**Sweep traits.** The exact reading moves the `Sweep` trait for four classes, and `HostWitnessTraitDriftTests` holds it
both ways. `LevelNumberArmDriftTests` (it enumerates `src/Cobol.Net.Compiler`) and `SourceEncodingDriftTests` (`src`,
`tests`) carry it; `CitationAuditSelfTestDriftTests`, `FixedFormRecordDriftTests` and `Gr4GroupDecisionDriftTests` read
only the files they name and lose it.

**Cost.** Tier-0d recorded seconds, before to after, from the shared timings store (contention-inflated): a grammar edit
74 to 78 classes, 476 to 581 s (`SendingOperandDriftTests` 76 s and `ArithmeticOperandRoleDriftTests` 24 s join); a compiler
emitter edit 130 to 128 classes, 357 to 346 s; a binder edit 404 to 404 s; a golden-adding, test-editing,
script-editing or `docs/CONFORMANCE.md` diff is unchanged. Recorded in DESIGN-test-build-ci.md section 3.14.2.

**The timing instances.** The note's third to fifth instances (a load-skewed sample, a leg-2 case never re-timed) decided
whether a sweep the diff reaches ran at the Windows gate. With tier 0d they decide only the order of a sweep the diff cannot
reach, so the timings store needs no change.

**Witnesses.** `drift_rules.py --self-test` fires four planted classes (a nested call, a constant, a bare tree-root
`Path.Combine`, a wrapper and its method-group use, and a blind test); `impacted_tests.py --self-test` names the grammar
tests for `CobolData.g4`, `ClassConditionTableDriftTests` for `ClassConditionModel.cs` and `FixedFormRecordDriftTests` for an
emitter file against the real index. No golden: this is test-gate tooling.

**Review round 1.** The reviewer found that a wrapper's caller contributed EVERY argument to the wrapper's path, including
the ones bound to parameters the body's path never carries: `CitationAuditSelfTestDriftTests.AssertDeclares(string script,
params string[] cases)` was indexed as reading `scripts/spec/cite.py/fires EMPTY-NEEDLE (check)/..`, so it lost its
governance of `cite.py`. `Sources.reads` now records which parameters the read's path carries and takes only those
arguments at a caller (a `params` parameter takes the rest); `cite.py` governs the class again, and the probe for index
paths that exist nowhere finds none from a wrapper. A constant that names a later one no longer reads as ambiguous when a
pass refines its own earlier partial value (the reviewer's lead): `Sources.constants` repeats passes to a fixed point and
flags only a name declared twice to different values in one pass. Both have planted arms in `drift_rules.py --self-test`,
and `impacted_tests.py --self-test` holds `cite.py -> CitationAuditSelfTestDriftTests` against the real index.

**Landing note.** This branch merges the PB3155 follow-up (`worktree-wf_0e51b14b-ced-6`, entry 2434), which was reviewed
CLEAN and has not landed; it lands first or with this one.

