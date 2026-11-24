# PB3231: not done in wave 20262164 group A; the sites have moved

Wave 20262164 group A (branch `worktree-wf_38508f28-551-1`) was dispatched with PB3231 bundled again with PB2622,
PB2627, PB2724 and PB3227. This wave carried the finished PB2627 and PB2724 work onto current main and returned it
for landing. It did not start PB3231 (one mechanism per implementer). The note says PB3231 "lands as part of PB2372
or immediately before it". PB2372 has since landed, so PB3231 is no longer blocked and can be its own dispatch.

**Sites (re-measured 2026-11-24, merge of origin/main 32bcb7b79a):** `ReferenceResolver.ObjectProperty.cs` no
longer exists. Its forgers are in `ObjectPropertyResolver.cs`.
- `src/WiseOwl.Cobol.Compiler/Binding/ReferenceResolver.cs:104` (`ReferenceResolver.WithoutTrailingSuffix`)
- `src/WiseOwl.Cobol.Compiler/Binding/ReferenceResolver.cs:560` (`ReferenceResolver.ReadWritten`), `:1003` (`WrittenReference`)
- `src/WiseOwl.Cobol.Compiler/Binding/ObjectPropertyResolver.cs:114` (`ObjectPropertyResolver.ViewSubjectChain`)
- `src/WiseOwl.Cobol.Compiler/Binding/ObjectPropertyResolver.cs:329` (`ObjectPropertyResolver.PropertyObjectReference`), `:356` (`WithoutRefMods`)
- `src/WiseOwl.Cobol.Compiler/Binding/ObjectPropertyResolver.cs:377` (`ObjectPropertyResolver.WithoutResultRefMods`), `:399` (`Suffix`)
- `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs:3378` (alphabet peel write), `:4913` (read), `:4928` (`DataBinder._alphabetPeeledOperands`)
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/EvaluateBinder.cs:858` (THROUGH IN alphabet operand)
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/OoBinder.PropertyObject.cs:65` (`OoBinder.OoBindPropertyObject`)

`grep -rn "new Core\.[A-Za-z]*Context(" src --include=*.cs` finds the eight forged constructions in the two
resolver files, plus the two `ProgramUnitContext` reparents (`BinderDriver.cs:565`, `OoDriver.cs:224`), which are
not this note's. The input change reaches far. `DataReferenceContext` is named 278 times outside the generated
parser. The funnel's readers name `dref` 51 times (ReferenceResolver.cs), 47 (SubscriptBinder.cs), 35
(ObjectPropertyResolver.cs), 29 (NameResolution.cs) and 15 (RefModBinder.cs). A forged slice also reaches
`FieldOperand`, `Probe`, `Resolve`, `WrittenSource.Of`, `WrittenQualifiedName.Of` and `IStatementSite.ApplyObjectView`.
A slice of a reference can start at a qualification's word (identifier-3 of Format 7, the subject of a view), so the
model's head is either the base word or a qualifier, and each has its own subscript and reference-modifier tail.
