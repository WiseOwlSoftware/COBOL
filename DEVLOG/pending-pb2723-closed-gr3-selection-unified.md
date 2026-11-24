## PB2723 closed: the USE GR3 selection tiers are decided and rendered once

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
