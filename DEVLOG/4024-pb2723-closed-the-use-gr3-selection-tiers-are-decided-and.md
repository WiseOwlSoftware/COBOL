## Entry 4024 — 2026-11-23 18:22 PST — PB2723 closed: the USE GR3 selection tiers are decided and rendered once, the open-mode tier included

Wave 20262161 group B was handed PB2723 with `ExceptionSelectionTierDriftTests.cs` as its root. The re-probe at main
ddf3400a61 found nothing of the note's own mechanism left to fix. Two earlier landings did the work. The GR3 unify
(commit 3e3877e3da, DEVLOG 3997) added `BoundExceptionSelector`, `ExceptionOpenMode` and `ExceptionSelectionEmitter`
and deleted `ControlFlowEmitter.ClassifyOperand` and `ModeOrdinal`. The "nothing selected" change (commit 13ad1c8d12)
made `DispatchResult.NoHandler` the one encoding of "nothing selected". The clerk filing of 2026-11-23 split the
three remaining merged findings into PB6139 (grammar rule pairs), PB6140 (GOBACK exception cost) and PB6141 (Tier-A
REDEFINES alias).

The evidence: a grep of `src/` for the retired names finds only the PublicAPI lines that record the removal. Only
`ExceptionSelectionEmitter.cs` writes `ExceptionCatalog.UnderLevel2(` or `IsIoName(__ec)` in CodeGen. The two drift
classes pass, 20 of 20. The GR3 c) and GR3 b) goldens (`l1_perform_f3_when_file_ec_io_prefix`,
`pb5825_when_exception_open_mode_opened`) print their expected output on this build. §2023:14.9.28.4 GR17 is the rule.

The change is register-only: PB2723 goes to `status: landed`, with an addendum `kb/Work/PB2723/2026-11-23-closed.md`.
It claimed no inventory rows, so it closes none (`closes_rows: []` with its reason).

The branch review (round 1, finding R1-1) found the one rendering still missing: the GR3 b) open-mode tier. The USE
switch selected on `FileConnector.UseModeView` (the mode the file is open in wins when both modes have a procedure,
D-USE-OPEN-MODE-BOTH), while the WHEN EXCEPTION mode arms tested "attempted or opened mode" and ran the first-written
phrase. The reviewer's probe, a file open INPUT and an OPEN OUTPUT refused 41 with WHEN OUTPUT written before WHEN INPUT,
printed `OUTPUT-MODE 41` against the USE twin's `INPUT-MODE 41`. Now `ExceptionSelectionEmitter.OpenModeSelected` is the
one mode expression both selectors use, the PERFORM's WHEN-phrase modes forming its mask. The two-view renderers
(`RuntimeApi.FileOpenModeOf`, `FileOpenedModeOf`) and the runtime accessors only they called are deleted. A drift
test pins the one renderer, and the golden `14.9.28-PERFORM/pb2723_when_exception_both_modes_open_mode_runs` covers both
phrase orders and the single-mode arm. The probe now prints `INPUT-MODE 41`. The determination
D-USE-OPEN-MODE-BOTH names the WHEN phrases too.

