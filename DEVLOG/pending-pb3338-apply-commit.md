## PB3338: the APPLY COMMIT clause, COMMIT and ROLLBACK are provided for the keyed organizations

Wave 1141 group A built the mechanism and returned SPLIT; wave 1151 group A resumed it (the branch continues as
`worktree-wf_331128da-8bc-1`, merged with main's d06-2 landing) and returned SPLIT again before the documentation and
inventory steps; wave 1154 group A finished them (the branch continues as `worktree-wf_e05cc4a3-4db-1`).

**Re-probe.** On 2881947e8 a 2023 program with `APPLY COMMIT ON RELF WS-COUNT` over a RELATIVE file was refused
COBOLNET1709, and COMMIT and ROLLBACK drew the accept-inert warning COBOLNET1579.

**Mechanism.** The clause moved out of the declined grammar (`CobolDeclined.g4`) into `CobolIO.g4` as
`APPLY COMMIT ON? dataReference*`: the §12.4.6.3.2 format repeats an all-optional pair, so the zero-operand spelling
parses and §12.4.6.3.3 SR4 refuses it by name (kb/Work PB1096). `DataBinder.BindApplyCommitClause` classifies each
operand (a bare word naming a file of this FILE-CONTROL paragraph is file-name-1, anything else identifier-1) and asks
SR1, SR2, SR4 and SR5; `ResolveApplyCommit`, at the end of `ResolveFiles`, resolves the identifiers and asks SR3,
SR7, SR8, SR9 (the implicit FILE STATUS, LINAGE and RECORD DEPENDING items, which join the saved items) and SR10. One
new code, COBOLNET4709, reports them all; `OoEnvironmentRules` alone reports SR6 for a factory or instance definition
(wave 1151 deleted the binder's second report of it: one violation drew two COBOLNET4709s). A
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
a keyed file of the element's clause (wave 1151: the hook proper is `EmitUseHook`'s local function, so the one method
that builds a `UseHookResult` still builds every one). The activation raises EC-FLOW-APPLY-COMMIT (Table 13's row for
§12.4.6.3.4 GR3/GR4's "EC-FLOW-APPLY": "Invocation of a runtime element with an APPLY COMMIT clause in effect by a
runtime element for which commit and rollback is not permitted"), which wave 1151 made the ACTIVATING element's: the
CALL declares the name, EcEmitter's fatal gate table carries its flag, `ProgramTable.CallProgram` latches the CALL's
checking before the callee's scope opens (`ExceptionEngine.ActivatorFlowApplyCommit`) and the activation raises under the
latch. The first build folded it at the activated element's PROCEDURE DIVISION header through an emitted set/reset pair,
which `ExceptionRaiseHelperDriftTests` refused (a raise no gate table arms). The transaction facade's calls name
`CobolFile` by its full name: a program with only a COMMIT or ROLLBACK describes no file and has no file-I/O import
(the first gate's CS0103 in `pb137_commit_inert`, `wave_h_facilities_inert` and `DeclinedFacilityTests`).

**Goldens.** `cases/14.9.36-ROLLBACK/pb3338_apply_commit_rollback` (record 2 rolled back, record 1 kept, WS-COUNT and
the FILE STATUS item restored), `cases/12.4.6-I-O-CONTROL/pb3338_apply_commit_no_operand` (SR4, COBOLNET4709),
`pb3338_apply_commit_below_2023` (COBOLNET1970 at 2002 and 2014), and wave 1151's refusals of SR2, SR3 (a level-05
item, a repeated item), SR5, SR6 (a factory definition), SR7, SR8 and SR10 (`pb3338_apply_commit_sr*`), each written
over a RELATIVE file so that the only refusal it can draw is its rule's. SR9 has no negative: a FILE STATUS item outside
the three sections is already §12.4.5.8.3 SR2's (COBOLNET2455), and a RECORD DEPENDING item may be in the FILE SECTION.
The three `declined-ec-flow-*` negatives became acceptance witnesses. The six PB371 antecedent fixtures
(`apply-commit-*`) now write a RELATIVE file and pin each rule's own refusal, COBOLNET2974, in both lanes
(`ApplyCommitSubjectRuleTests`); `apply-commit-same-area-shared` is deleted, because §12.4.6.4.4 GR3 (two
commit-controlled files of one SAME clause) is now reachable and not enforced (a lead).

**Owner test requirement (I11).** `TransactionControlTests` drives `TransactionControl` through a real run unit, real
keyed connectors and `RefusingPageMedium`: a COMMIT refused at every page operation it makes, with and without a FILE
STATUS item ('30' to the program, nothing saved, item 8's ROLLBACK restoring the items and the file; after sync 2 the
COMMIT stands), a two-file COMMIT refused at the second file (the first stays committed), a refused ROLLBACK (the
items still restored), item 8 after a refused WRITE (and nothing on a file outside the clause or after '22'),
EC-FLOW-COMMIT / -ROLLBACK / -APPLY-COMMIT under an active SORT with checking on and off, and retirement.

**Wave 1154: the rows, the documents and the register.** The derived verdict `commit-and-rollback-only` (40 rows
stamped DOCUMENTED-NON-SUPPORT on the old Annex A.4.3 decline) is retired with its two drift facts, and batch
`w1154a-pb3338-apply-commit` re-verdicts the module's rows on what the code does: 32 CONFORMS and closed (the format,
SR1-SR11, GR2/4/5/6 of §12.4.6.3.4, COMMIT GR1/GR5, ROLLBACK GR1/GR4-GR7, A.1 item 28, and the seven antecedent rows
of claimed facilities, now witnessed by their own refusal, COBOLNET2974); 19 PARTIAL, each claimed by an open note:
the rows whose rule covers every file subject to commit and rollback by kb/Work PB3340 (the sequential half), and
the EC-FLOW recursion rows by kb/Work PB3540 (an active METHOD is not seen); §12.4.6.4.4 GR3 NEEDS-OWNER-DECISION
under kb/Work PB3539. Nine rows that landed notes had closed on the decline (PB261, PB259, PB371) are re-opened
there with a paragraph; GAP moves 65 → 74, the honest count of a module that is Partial. New goldens: SR1 and SR11
(an operand looked up only in the element that writes the clause, COBOLNET1639), GR2 (an EXTERNAL file under two
programs' clauses, one transaction), GR6 (SET ADDRESS of a clause item is refused, §14.9.39.3 SR18, so the
address-change condition has no cause) and the recursion arm of EC-FLOW-COMMIT / -ROLLBACK, which wave 1151 had
no case for. The three pb259/pb261 goldens declare no clause, so their headers now pin GR1. Documents:
`docs/CONFORMANCE.md` §5 A.4.3 and §2 rows 6-7 Partial with the determinations (what a refused COMMIT tells the
program, the per-file order, the dead owner's records, EC-FLOW-APPLY-COMMIT's activator, GR6), §4 item 2 reduced
to the sequential operand, §7 DOC-A.1-28, COBOLNET1579 struck everywhere; the suite profile claims A.4.3 and
A.3 items 6-7; the README summary; VCR row 39 anchored to the new construct `apply-commit-clause-2023`; the files,
validation and data-model designs; plan §0. Register: PB3338 and PB1096 landed; PB3330 unblocked.

**Gate.** `LEG 1 ONLY (batched gating, PB2515): GREEN` at 171ef15bb (run 20261022T234659Z-e0f6c1).
