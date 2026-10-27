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

Branch review round 1, R1-1 (blocking): the foreign arm chose checking on or off from the directives that name NO file,
so `>>TURN EC-I-O KF CHECKING ON` naming the container's GLOBAL file was ignored for a refused COMMIT of KF, while every
other statement on KF honors it (§7.3.25.4 GR6, GR8: checking is enabled or disabled "only for exception conditions
associated with that file-name"). With the fix of this branch that turned a termination into a continuation: the format 1
declarative ran, item 8's ROLLBACK followed and the run unit went on, where checking was enabled for the file and §14.6.13.1.3
rule 7 ends it. Root cause: a refused file of another element had no FileModel in the statement, so no directive naming it
could be folded. `BoundCommitRollback` now carries `ForeignFiles`, the containers' GLOBAL files under commit control that the
element can name (`FilesByName`, §13.18.27.4 GR1); `EcBinder.IoFilesOf` names them, so the checking fold sees the pairs
(EC-I-O, file), and `TransactionEmitter.EmitStatement` writes an arm keyed to the file for each one whose checking a naming
directive changes (`SequentialIoEmitter.ForeignFileCheckedApart`); every other program emits the same text as before. New
witnesses: `NamedFileChecking*` (checking enabled by a directive naming the file, with and without a FILE STATUS item: the
declarative runs and the run unit ends) and `NamedFileCheckingOffUnderGeneralOn` (checking on for every file, off for KF:
the declarative runs and the run unit continues), all red before the fix (12 of 12), plus `ContainerGlobalUse*` (R1-2: the
container's `USE GLOBAL` declarative runs for a refusal in an executing element with none, GR4 b)). R1-3: a format 1
declarative names no exception-name, so §14.6.13.1.3 rule 5 does not select it; the I-O status does (rule 3, §9.1.13.1) and
rule 7 ends the run unit; the test remarks, this entry and the note carry that citation now.

Witnesses: the open-mode and file-name tiers with and without a FILE STATUS item, the open-mode tier with checking on
(a format 1 declarative names no exception-name, so it runs under rule 3 and §9.1.13.1 and the run unit ends under rule 7 of §14.6.13.1.3, not rule 5), and a declarative ending in RESUME AT, whose trace shows item 8's
ROLLBACK ran before the transfer, and (branch review round 1) the containing element's own `USE GLOBAL` declarative reached from an executing element
that has none. 87 of 87 pass in the class. No golden: a refused COMMIT needs the fault-injecting
medium, which the conformance suite cannot drive (the same holds for PB3338 and PB3542-PB3544). Docs: DESIGN-runtime-library
2.2.1.10 and CONFORMANCE A.4.3 determination (a) state the rule. Inventory rows: none claimed, none closed.
