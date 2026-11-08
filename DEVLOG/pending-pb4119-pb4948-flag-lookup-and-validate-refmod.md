## PB4119 and PB4948: one silent declaration lookup, one reference-modification reader

**PB4119.** `FlagConformancePass.ResolveItem` was a second copy of `ReferenceResolver.ProbeDeclaration` and it did not
probe, so under `--permissive` with a `>>FLAG-02` option an ambiguous name drew COBOL1639 a second time at the MOVE verb
(re-probe: `MOVE X TO X` with two `X` gave three COBOL1639; now two, one per operand). It also read only the flat
qualifier list, missing a qualifier that absorbed a subscript. `ResolveItem` now asks `ProbeDeclaration`, the one lookup.
Pinned by `FlagDirectiveTests.Compile_MoveToSameNameOn_AmbiguousOperand_ReportsCobol1639OncePerOperand_NotAgainAtTheVerb`.

**PB4948.** `VALIDATE X OF G (1:3)` compiled while `VALIDATE X (1:3)` was refused with COBOL4777 (§2023:14.9.50.3 SR1),
because the grammar's `qualification` absorbs the reference modifier and `ScreenValidateOperand` read the flat suffix
only. The test lives once, in `ReferenceResolver.ReadWritten` (`WrittenReference.IsReferenceModified`, which also counts a function-object property result reference modifier, PB1425); `ScreenValidateOperand`,
`VersionConformancePass.VisitUnstringStatement` (which carried a third copy) and `FlagConformancePass` (whose private
`IsRefModified` is deleted) all call it. Golden: `conformance-suite/cases/14.9.50-VALIDATE/pb4948-validate-qualified-ref-modified-operand`
(refused at 2002, 2014, 2023 with COBOL4777). No inventory rows claimed or closed.

Self-review: review round 1 (R1-1) found a first draft had added a second reader, DataReferenceCst.IsReferenceModified,
that disagreed with ReadWritten on a property suffix; it is deleted and the three callers use the resolver decomposition.
The USE FOR DEBUGGING check at ProcedureTableBuilder 809 also tests subscripts and was left.
