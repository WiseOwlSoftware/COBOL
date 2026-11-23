# PB3227, 2026-11-22: the refused argument reports once, and the count rule is written once (wave 20262139 group D)

**Re-probe (base d87e47fa1d).** A two-formal FUNCTION-ID R35TWO, `MOVE FUNCTION R35TWO(NOSUCH 1) TO X` and
`MOVE FUNCTION R35TWO(LEADING 1) TO X`: each drew its own error (COBOL1639, COBOL1638) and a second
`COBOL1506: FUNCTION R35TWO argument 1: ... is not yet supported for user-defined function activation`.
After: COBOL1639 and COBOL1638 alone.

**Fixed (the finding `procedure-binding-5--code--f2#1`).** `UdfBinder.UdfActivate` carries a `BoundOperandError` argument as
`BoundExprError.Carry` (already on the ledger, reported by its own rule or announced as unbuilt) and reports nothing; an operand
shape `UdfArg` has no arm for is `BoundExprError.Unbuilt` (COBOL1756 through the statement funnel), never a source diagnostic.
COBOL1506's descriptor now names the §2023:14.8.2.1 count rule alone.

**Fixed (the merged finding `procedure-binding-5--duplication--f1#1`).** The §2023:14.8.2.1 count rule is one predicate,
`ParameterConformance.ArgumentCountConforms`, asked by the CALL (`CallBinder`, COBOL code of `CallArgumentCount`), INVOKE
(`OoBinder.OoBindResolvedInvoke`, `InvokeArgumentNotConforming`) and function (`UdfBinder.UdfActivate`, COBOL1506) binders. Each
statement keeps its own code: the three are different activating statements, and their messages name the statement.

**Still open (why the note is `half`).**
- `data-binding-2--duplication--f1#5`: the data pass derives each CALL/INVOKE argument's pass mode
  (`StorageClaimScan.ByReferenceOperands`) apart from the CALL binder that decides it. Untouched.
- `procedure-binding-5--modern-csharp--f2#1`: an ISO category or class word rendered by `ToString().ToLowerInvariant()` of a
  `PicCategory`/`CobolClass` enum name (wrong for every multi-word member, e.g. `numericedited`). A sweep found about 12 such sites
  in `src/WiseOwl.Cobol.Compiler/Binding` (`MoveBinder.cs:449`, `SetBinder.cs:964`, `CallBinder.cs:1147`, `ConditionBinder.cs:1454`,
  `OperandClassScreen.cs:196`, `ReportSectionBinder.cs:4249`/`4352`, `DataBinder.SignClause.cs:101`, `ExternalizedName.cs:97`/`144`,
  `AssignTargetRule.cs:169`, `ResolveRecordFormatPass.cs:247`); one spelling function for the ISO category word, asked by each, is
  the fix. Untouched.
