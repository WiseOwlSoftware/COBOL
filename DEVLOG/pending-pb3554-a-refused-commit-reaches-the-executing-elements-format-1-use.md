## PB3554: a refused COMMIT or ROLLBACK of another element's file reaches the executing element's format 1 USE procedures with or without checking

A COMMIT or ROLLBACK the medium refuses for a file of another element's APPLY COMMIT clause ran the executing element's
open-mode USE procedure (`USE AFTER ERROR ON I-O`) when EC-I-O checking was on, and no USE procedure at all when it was
off. The adjudication (2026-10-25) read the standard: format 1 USE selection follows the I-O status, never EC checking
(§14.6.13.1.1 and its NOTE 1), the candidates are the declaratives of the element containing the statement and then its
containers' GLOBAL ones (§14.9.49.4 GR4 a), b)), and GR6 a)-e) run a file-name USE when its condition occurs and an
open-mode USE for any file open in that mode, naming no element. The checking-on arm was right; the checking-off arm was
the defect.

Re-probe: the adjudication's four witnesses, written first as `ForeignCover` cases of
`CommitRollbackRefusalWitnessTests` (both keyed organizations, COMMIT and ROLLBACK), failed 20 of 20 on a fresh build:
the contained program's declarative (`ON I-O`, or `ON KF` for the containing element's GLOBAL file) never ran, and with
no FILE STATUS item anywhere the run unit ended under DOC-A.1-103 although a declarative covered the status.

Fix shape: the plain format 1 hook (the item 8 question, `__IoCheck`, item 8's ROLLBACK, the RESUME transfer under the
EC model) was inline in `SequentialIoEmitter.EmitUseHook`. It is now one method, `EmitPlainIoCheck`, used by the
own-file arm and by `EmitForeignFileIoCheck`'s checking-off branch, so the two arms cannot drift; the foreign branch
returns the "no declarative ran" test and `TransactionEmitter` terminates only when none ran and no element gives the
file a FILE STATUS item. The checking-on arm is unchanged. The emitted text of every existing hook is identical
(same lines, same name allocation order).

Witnesses: the open-mode and file-name tiers with and without a FILE STATUS item, the open-mode tier with checking on
(the declarative runs, then rule 5's termination), and a declarative ending in RESUME AT, whose trace shows item 8's
ROLLBACK ran before the transfer. 67 of 67 pass in the class. No golden: a refused COMMIT needs the fault-injecting
medium, which the conformance suite cannot drive (the same holds for PB3338 and PB3542-PB3544). Docs: DESIGN-runtime-library
2.2.1.10 and CONFORMANCE A.4.3 determination (a) state the rule. Inventory rows: none claimed, none closed.
