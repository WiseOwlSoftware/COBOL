## PB5847: the implicit CLOSE of a CANCEL and of an INITIAL program's return is the one CLOSE, and stores the I-O status

**The defect.** During a CANCEL "an implicit CLOSE statement without any optional phrases is executed" for each open
internal file (`§1985:X.5.3.4 GR10`, `§2023:14.9.5.4 GR9`), and an INITIAL program's EXIT PROGRAM or GOBACK executes that
CANCEL (`§1985:X.5.4.4 GR3`, `§2023:14.9.18.4 GR2`). A CLOSE "causes the value of the I-O status associated with
file-name-1 to be updated" (`§1985:VII.4.2.4 GR4`, `§2023:14.9.6.4 GR4`). The re-probe of the note's `iclose.cob` on a
build of main e14987286b printed `AFTER CANCEL GST=10` and `AFTER INITIAL GST=10` at `--std 85` and `--std 2023`: the
FILE STATUS item, a GLOBAL item of the container, kept the subprogram's last READ status where 00 is due.

**Root cause.** `ProgramEmitter.EmitProgramClass` emitted `CloseFiles()` as one `CobolFile.CloseIfOpen` call per file, a
second and smaller close beside the explicit CLOSE, with no status store.

**The fix.** `SequentialIoEmitter.EmitCloseStatement` now renders every CLOSE the program executes: the CLOSE scope, the
written form's Table 14 entry, the status store, then the reach's own USE hook. The explicit CLOSE, the SORT and MERGE
USING and GIVING closes, and the new `EmitImplicitClose` all go through it. The implicit reach runs only for a file that is
open (`CobolFile.IsOpen`, so a closed file keeps its status and gets no 42) and passes no USE hook, as the rule says.
`CobolFile.CloseIfOpen`, `RuntimeApi.FileCloseIfOpen` and the one-argument `RuntimeApi.FileClose` are deleted.
`CloseSequenceDriftTests` holds the shape: only `EmitCloseStatement` renders a CLOSE's runtime entry or scope.

**Found on the way.** A CANCEL of a RECURSIVE container whose RECURSIVE containee had returned built the containee's
transient instance with a null parent, so the new store into an outer GLOBAL status item threw. `ProgramTable.InstanceForClose`
completes the transient parent chain (`CancelCascadeOrderTests`).

**Witnesses and records.** `pb5847_cancel_implicit_close_status_85` and `pb5847_initial_exit_implicit_close_status_85`
(1985, fixed form) and `pb5847_cancel_implicit_close_status` and `pb5847_initial_exit_implicit_close_status` (2023) pin 00
after the implicit close, a reopen at 00 (the file was closed), a closed file's 42 that stands, and the contained-program
cascade. Rows `GR-14.9.5.4-9` (notes restated) and `GR-14.9.14.4-3` take the 2023 witnesses; `1985-GR-X.5.3.4-10` and
`1985-GR-X.5.4.4-3` take the 1985 ones. Three 1985 code reads named the deleted `EmitCloseScope` (`1985-GR-IX.4.2.4-7`,
`1985-GR-VIII.4.2.4-7`, `1985-SR-IX.4.2.3-1`); they were withdrawn (`code_reads.py withdraw`, kb/Work PB5842), so each
row is owed a read of the fixed code. Their probes re-run at the branch head print the recorded values. kb/Work PB5685 (the
implicit CLOSE's report-active check) is unchanged by this fix, measured in `kb/Work/PB5685/2026-11-16-after-pb5847.md`.
`docs/FILES_DESIGN.md` describes the one CLOSE.
