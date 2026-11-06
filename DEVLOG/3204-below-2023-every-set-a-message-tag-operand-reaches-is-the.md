## Entry 3204 — 2026-11-06 11:17 PST — Below 2023 every SET a message-tag operand reaches is the COBOL0900 gate alone, and an imported operand is told from a same-file one by its origin

PB4836 and PB4837, one root: the below-2023 no-op that PB4771 gave `SetBinder.BindMessageTagSet` was Format 17's alone, and its
"is the gate already refusing this?" test (`HasDeclaredMessageTagOperand`) read the SHAPE of the operand (a compiler temporary or an
object property is not declared), not where its description came from.

**Re-probe (Debug build, `--std 2014`).** `SET P TO T`, `SET O TO T`, `SET T UP BY 1`, `SET T DOWN BY 1` each drew COBOL0869 / COBOL0867 /
COBOL2112 beside the gate; `SET T TO P`, `SET T TO O` bound to Format 17's SR35 and drew COBOL0869 (Format 17 was already a no-op for
declared items, so those two were already clean once the receiver was declared; the theory covers them anyway). A same-file function's
returned temporary, a same-file class's property and a same-file inline method result each drew COBOL0869 beside COBOL0900.

**Fix.** The question moved to the one dispatch every SET format passes through: `SetBinder.BindSet` binds the statement to a no-op when
the edition lacks `usage-message-tag-2023` and some data reference of the statement names a message-tag item whose entry this
compilation walks (`NamesGatedMessageTag`, a pure probe over the statement's own references, a SET holding no nested statement).
`BindSetTo` asks the same of a function-identifier or inline-invocation sender after the classifier binds it (`sender.TemporaryItem`).
"Whose entry this compilation walks" is the origin: `DataItem.IsImported`, set by the repository decoder's one `Item` constructor and
carried to a compiler temporary by `CompilerTemps.Create` from its model. A same-file function, class property and method are the
compilation's own items, so their gate has fired; an imported one has no entry and stays bound and checked at every edition
(the PB4771 review R1-1 and R2-1 behaviour, unchanged: `SetFormat17_ImportedFunctionSender_IsRefusedAtEveryEdition`,
`SetFormat17_ImportedPropertyOperand_IsBoundAtEveryEdition`). `HasDeclaredMessageTagOperand` is deleted; `BindMessageTagSet` keeps only
the SR35 refusals.

**Goldens.** No ISO rule is introduced, so the witnesses are theories next to PB4771's in `MessageTagIntroductionGateTests`:
`EverySetFormatAMessageTagOperandReaches_IsTheGateAloneBelowTheIntroduction` (13 cases) and
`SameFileFunctionPropertyAndMethodOperands_AreTheGateAloneBelowTheIntroduction` (10), each asserting that below 2023 every error is
COBOL0900 and at 2023 the format's own refusal speaks with no gate.

**Left (registered in the report, not fixed).** MOVE, DISPLAY and every other verb still draw a class diagnostic beside the gate at
2014 for a tag operand (DESIGN-message-control.md §7.1's "no second one may appear" holds for SET only). The structural answer is the
same question asked once in `StatementBinder.BindStatementCore` over a statement's own operands, outside this group.
`SET <capacity> TO FUNCTION <same-file function returning a tag>` binds its amount itself (F14/F16 arms) and is not reached by the
sender hook.

**Rows.** None claimed or closed.

