## PB4321: a method local is referenced by its identity on the neutral tree, and the Roslyn backend spells it in one place

**The mechanism (leak L8 of DESIGN-backend-abstraction.md §1.3).** The binders formatted the C# names of the method
locals the generated code declares and froze them into the model and the bound tree: `PositionLocal.CsName`,
`LocalVector.CsName` (`__allN`, `__holdN`), `BoundReportVaryingRef.CsName` with `ReportVaryingModel.CsName`
(`__kv{Uid}`). A local's name is no ABI: no other module binds to it, and a CIL method addresses its locals by slot, so
the name is a backend decision.

**Re-probe (base 2e48863d5).** The fields were as the note found them. A probe program (INITIALIZE of a table,
`FUNCTION SUM(T(ALL))`, `MOVE CORRESPONDING GA(I) TO G2`) emits `for (long __ini0 …)`, `__all0 => …` and
`ref var __corr0s = ref …`, each name formatted in a binder. The sibling sweep (`$"__` interpolations and string
fields named for a local under `Binding/`) found four more of the same mechanism: `InitializeLoop.Var` (`__iniN`),
`TableElementMove.Var` (`__vlmN`), `CorrespondingHoist.Local` (`__corrNs`/`__corrNt`), and the CORRESPONDING anchor's
member paths, whose root was a `RootFieldSegment` holding the anchor local's name.

**The fix.** A local is referenced by its identity, a `MethodLocal` (`Binding/Model/MethodLocal.cs`). The binder's
kind is `StatementLocal(StatementLocalRole Role, int Ordinal)`: the construct that declares it and an ordinal, the
binders' existing serials. Every node above carries it, and a CORRESPONDING anchor's member path starts at the new
`LocalRootSegment`. The Roslyn backend spells every local in one function, `LocalNames.Of`
(`CodeGen/Roslyn/LocalNames.cs`), with today's stems, and the locals it declares itself (a hoisted identification, a
leaf loop counter, a sort index) are `CSharpLocal`s it makes. The design doc's L8 row is DONE, and its §1.2, §2.4, §4
and §5 text says where a local's name now lives; DESIGN-binder-bound-tree.md §3.9.1, INTRINSICS_DESIGN.md and
REPORT_WRITER_DESIGN.md name the identity too.

**Neutrality.** `python scripts/arch/compare_oracle.py --base 2e48863d5`:
`=== ARCH-ORACLE: IDENTICAL — 15712 cases (baseline 2e48863d5802, new 2e48863d5802-dirty) ===`. The probe's emitted
C# is byte-identical too. `python scripts/semgrep/verify.py`: PASS, no rule's count moved.

**The witness.** `MethodLocalIdentityDriftTests` (a drift rule over `src/WiseOwl.Cobol.Compiler/Binding`) holds three facts: no `WiseOwl.Cobol.Binding` type has
a `string` property named for a local, no binder source spells a role's stem, and every role has its own spelling. The
first two failed on a planted offender (`record ProbeL8Temp(string Var)` returning `$"__kv{n}"`) before it was
removed. `PositionCarrierDriftTests` dropped its `PositionLocal.CsName` adjudication. The first gate was RED on one
case, `HostWitnessTraitDriftTests.TheSweepTrait_IsExactlyDriftRulesTreeWideSweeps…`: the new test carried
`[Trait("Sweep", "tree")]`, but it reads `src/WiseOwl.Cobol.Compiler/Binding`, a SPECIFIC path in `drift_rules.py`'s
reckoning, so the trait came off.

**Left as it is.** The Roslyn backend still roots two of its own locals in a `RootFieldSegment`
(`PlaceIdentification.FreezeView`, `PlaceCursor.AtElementCell`). That is §1.3 L7's `CsField` shape, and moving them
changes the emitted C# (`PlaceRenderer.Received`'s BASED-area match), so it is a lead in the wave report.

**Self-review.** Architecture: no C# spelling of a local is left on a neutral node; the backend extends the identity
only for locals it declares itself. Code: each emitter reads the name once per declaration, and the declaration and
its reads go through the same `LocalNames.Of`. Performance: one small record per local where a string stood, off any
hot path. Duplication: the stems have one home. No golden: the change is behavior-neutral and no rule's behavior
moved, and the oracle is its evidence.
