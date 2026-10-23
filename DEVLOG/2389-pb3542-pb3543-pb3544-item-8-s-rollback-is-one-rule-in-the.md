## Entry 2389 — 2026-10-22 21:24 PDT — PB3542, PB3543, PB3544: item 8's ROLLBACK is one rule, in the standard's order

Wave 1156 group A. ISO §14.6.13.1.3 item 8 says that "if there is a fatal exception condition for a file subject to an
active APPLY COMMIT clause, then a ROLLBACK statement is executed unconditionally", but only in its own arm: "If checking for
the exception condition is not enabled". PB3338 emitted the ROLLBACK as the last line after every I-O hook. Three
adjudicated defects followed from that placement, and the re-probe on this branch's build reproduced each one:

- PB3542: with checking off, a USE procedure that ended in RESUME AT P2 jumped past the ROLLBACK. WS-COUNT stayed 0002 and
  record 2 stayed in the file.
- PB3543: with EC-I-O checking on, a USE procedure or a WHEN phrase that ended in RESUME AT NEXT STATEMENT still rolled
  back, although item 5 NOTE 2 (and item 4 NOTE 1) says the program continues and item 8 is never reached.
- PB3544: a '35' on a RELATIVE USING file rolled back under the SORT, although item 2 hands SORT and MERGE to their own
  rules. A '47' from a READ in the SORT's input procedure also rolled back, with no EC-FLOW-ROLLBACK, although
  §14.9.36.4 GR2 forbids a ROLLBACK under SORT control.

**The fix.** `SequentialIoEmitter` now holds the rule once. `Item8Applies` decides where item 8 can apply: never on a
SORT/MERGE transfer (`verbDisposes`), never where the statement's ON EXCEPTION phrase processes the condition (item 1), and
never on a success arm. `EmitItem8Question` asks, at the raise and before any declarative runs, whether the ROLLBACK is
due: `CobolFile.RollbackDueOnFatal(name, checkingEnabled)`, where the emitter passes the raised condition's bit in the
statement's checking mask. `EmitItem8Rollback` runs the ROLLBACK after the declarative and before a RESUME transfers
control or DOC-A.1-103 ends the run unit. Every arm of the hook consumes the rule: the `__IoCheckEc` arm, the EC-model
`__IoCheck` arm, the plain arm, the no-declaratives arm, and the foreign-file arm of a refused COMMIT or ROLLBACK.
`TransactionControl.RollbackOnFatal()` executes the ROLLBACK statement's general rules through `Execute`, which it shares
with `Rollback`, so GR1 (no active clause: CONTINUE) and GR2 (EC-FLOW-ROLLBACK under SORT, MERGE or a recursive element)
hold. Every input-output statement on a file under commit control declares EC-FLOW-ROLLBACK (`EcBinder.NodeKindRaises`).
`FileModel.IsCommitControlled` now states the keyed-file-of-a-clause fact once; `ApplyCommitModel.CommitControlledFiles`,
the rule and the EC declaration all read it.

**Siblings found on the way and fixed.** (1) A declarative that CLOSEd the file replaced the fatal status before the old
post-hook question was asked, so nothing was rolled back. The question is now asked at the raise. (2) A DELETE FILE whose
ON EXCEPTION phrase processed '41' still rolled back, which item 1 forbids. (3) `TransactionControl.HasStatusItem`
stored the refusal's status while it answered. Asked after item 8's ROLLBACK, it would have put '30' back over the FILE
STATUS item the ROLLBACK had just restored. It now reads what `Refused` recorded.

**Goldens** (accept 2023, the edition of APPLY COMMIT; `pb3338_apply_commit_below_2023` is the refusal below it):
`14.6.13.1.3-Fatal-exception-conditions/pb3542_item8_before_resume_at_procedure`, `pb3542_item8_asked_at_the_raise`,
`pb3542_item8_not_after_an_on_exception_phrase`, `pb3543_item8_not_with_checking_enabled`,
`pb3543_item8_not_after_a_when_phrase`, `pb3544_item8_not_on_a_sort_transfer`, and
`14.9.36-ROLLBACK/pb3544_item8_rollback_in_sort_procedure_ec_flow` and `_unchecked`, and for the other two controls
§14.9.36.4 GR2 names, `pb3544_item8_rollback_in_merge_output_procedure_ec_flow` (a MERGE output procedure) and
`pb3544_item8_rollback_under_recursive_element_unchecked` (a recursive runtime element in the call chain). Unit:
`TransactionControlTests.Item_8_is_not_due_when_checking_for_the_condition_is_enabled` and
`Item_8_under_a_forbidding_control_raises_ec_flow_rollback_or_rolls_nothing_back` (an input procedure, an output
procedure and a recursive element, checking on and off); the existing item 8 cases now drive the
question and the ROLLBACK as the emitted code does. `Item8RollbackSiteDriftTests` pins the one site: the question and the
ROLLBACK are written only by `SequentialIoEmitter`, their runtime entry points are named only by `RuntimeApi`, and every
SORT/MERGE transfer hook passes `verbDisposes: true`.

**Rows.** None claimed; GR-14.6.13.1.3-8 stays CONFORMS. Docs: DESIGN-runtime-library.md 2.2.1.10 ("The compiler half")
and CONFORMANCE.md A.4.3's witnesses.

