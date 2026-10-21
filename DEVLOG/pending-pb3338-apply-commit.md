## PB3338: the APPLY COMMIT clause, COMMIT and ROLLBACK are provided for the keyed organizations

Wave 1141 group A, first half (the implementer returned SPLIT; the finisher completes this entry).

**Re-probe.** On 2881947e8 a 2023 program with `APPLY COMMIT ON RELF WS-COUNT` over a RELATIVE file was refused
COBOLNET1709, and COMMIT and ROLLBACK drew the accept-inert warning COBOLNET1579.

**Mechanism.** The clause moved out of the declined grammar (`CobolDeclined.g4`) into `CobolIO.g4` as
`APPLY COMMIT ON? dataReference*`: the §12.4.6.3.2 format repeats an all-optional pair, so the zero-operand spelling
parses and §12.4.6.3.3 SR4 refuses it by name (kb/Work PB1096). `DataBinder.BindApplyCommitClause` classifies each
operand (a bare word naming a file of this FILE-CONTROL paragraph is file-name-1, anything else identifier-1) and asks
SR1, SR2, SR4, SR5 and SR6; `ResolveApplyCommit`, at the end of `ResolveFiles`, resolves the identifiers and asks SR3,
SR7, SR8, SR9 (the implicit FILE STATUS, LINAGE and RECORD DEPENDING items, which join the saved items) and SR10. One
new code, COBOLNET4709, reports them all; `OoEnvironmentRules` reports SR6 for a factory or instance definition. A
SEQUENTIAL file in the clause stays refused by name, COBOLNET1709, narrowed to that operand (the sequential
transaction journal is kb/Work PB3340). COBOLNET1579 is retired.

The run unit's half is `TransactionControl` (`RunUnit.Transactions`, reached through `CobolFile`): every activated
clause is a participant whose emitted `__CommitSave` / `__CommitRestore` copy its items' storage images
(`OperandText.RecordAreaImage` and `SequentialIoEmitter.EmitImageInto`), saved at activation and at every COMMIT and
restored at ROLLBACK; a CANCEL or an INITIAL program's return retires the participant (no restore; deactivated by the
next COMMIT or ROLLBACK). COMMIT and ROLLBACK answer GR1's CONTINUE when no clause is active, raise EC-FLOW-COMMIT /
EC-FLOW-ROLLBACK under a recursive element or an active SORT/MERGE procedure, and otherwise call the keyed engine's
`FileRegistry.Commit` / `Rollback` (kb/Work PB3337). A refused file is told by '30' in its FILE STATUS item and its
exception processing; `SequentialIoEmitter.EmitUseHook` now ends with §14.6.13.1.3 item 8's unconditional ROLLBACK for
a keyed file of the element's clause. The activation raises EC-FLOW-APPLY-COMMIT (Table 13's row for §12.4.6.3.4
GR3/GR4's "EC-FLOW-APPLY") as of the element's PROCEDURE DIVISION header.

**Goldens.** `cases/14.9.36-ROLLBACK/pb3338_apply_commit_rollback` (record 2 rolled back, record 1 kept, WS-COUNT and
the FILE STATUS item restored), `cases/12.4.6-I-O-CONTROL/pb3338_apply_commit_no_operand` (SR4, COBOLNET4709),
`pb3338_apply_commit_below_2023` (COBOLNET1970 at 2002 and 2014). The three `declined-ec-flow-*` negatives became
acceptance witnesses.
