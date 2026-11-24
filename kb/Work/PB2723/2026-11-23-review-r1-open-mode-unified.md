**2026-11-23, wave 20262161 group B review finisher: the GR3 b) open-mode tier is selected once for USE and for PERFORM WHEN (branch review R1-1).**

**The finding.** The closing addendum (`2026-11-23-closed.md`) said the GR3 tiers are rendered once, but the open-mode tier
was still chosen two ways. The USE Format-1 switch (`UseTierEmitter.EmitScopeTiers`) selects on `CobolFile.UseModeOf(__f,
<declared modes>)`, which is `FileConnector.UseModeView`: the mode the file IS open in when a procedure is declared for it,
else the mode the executing OPEN attempts (D-USE-OPEN-MODE-BOTH, kb/Work PB5878). The WHEN EXCEPTION INPUT | OUTPUT | I-O |
EXTEND arms (`ExceptionSelectionEmitter.OpenModeTest`) tested "attempted mode OR opened mode", so when both modes had a
phrase the first-written one ran. §2023:14.9.28.4 GR17 makes the WHEN match the USE rule ("The rules for determining a
match are specified in General rules 3a to 3g of the USE statement"), so the two must agree.

**Re-probe (this branch, Debug build 0/0, `run_probe.py --std 2023`).** The reviewer's
`E:/COBOL-coord/scratch/review-w20262161-b/perf_both.cob` (F open INPUT, OPEN OUTPUT F refused 41, WHEN EXCEPTION OUTPUT
written before WHEN EXCEPTION INPUT) printed `OUTPUT-MODE 41`; its USE twin `use_both.cob` printed `INPUT-MODE 41`.

**The fix.** `ExceptionSelectionEmitter.OpenModeSelected(declaredModes)` is the one expression of the selected mode, and
`DeclaredModes` builds its mask. `UseTierEmitter` switches on it over its declaratives' modes; `ControlFlowEmitter.EmitMatcherArms`
passes the PERFORM's WHEN-phrase modes to `OpenModeTest(modeIndex, declaredModes)`, which tests that one mode. The two-view
renderers `RuntimeApi.FileOpenModeOf` and `FileOpenedModeOf` and the runtime accessors only they called
(`CobolFile.OpenModeOf`, `CobolFile.OpenedModeOf`, `FileRegistry.OpenedModeOf`) are deleted (`PublicAPI.Unshipped.txt`
records the removals). `ExceptionSelectionTierDriftTests.OpenModeSelection_IsRenderedOnlyByExceptionSelectionEmitter` pins
the one renderer, and `TheRetiredClassifiers_AreGone` refuses the deleted renderers.

**After.** `perf_both.cob` prints `INPUT-MODE 41`, the same as `use_both.cob`. The golden
`conformance-suite/cases/14.9.28-PERFORM/pb2723_when_exception_both_modes_open_mode_runs` (2023) covers both orders and
the single-mode arm (PB5825's rule still holds). `pb5825_when_exception_open_mode_opened` is unchanged.
`docs/conformance/3/D-USE-OPEN-MODE-BOTH.md` now states that the order also covers the WHEN phrases, and
`docs/CONDITIONS_EXCEPTIONS_DESIGN.md` decision (c) no longer says the open-mode WHEN tests `CobolFile.OpenModeOf`.
