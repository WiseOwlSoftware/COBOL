## Fixed on train85b-seg (wave 20262131 group b), with PB6008: one reach walk

Re-probed at --std 85 on the base build (be6c60fd80): s19_p16_perform_reach_out_and_back_bad compiled with warnings only
(the defect); s19_p13f refused COBOL6877. On the fix both are refused COBOL6877 ("names lie in segments 30, 60").

Root cause: `Segmentation.ReachedPcs` was its own walker over the parse tree, and it dropped every step outside the named
span, so a path that left the span and came back was lost. It is now no walker at all: `ProcedureReach.Walk` is the one
walk over the bound procedures (`FlowFromEntry` for segmentation: entry-seeded, GO TO and fall-through followed, no
PERFORM range inside followed), and `Segmentation.ReachedPcs` keeps its result to the named span. Because the walk needs
the bound procedures, `Segmentation.CheckProcedures` screens the span where the statement binds and records a span that
breaks the rule (`BinderContext.SegmentedRanges`); `Segmentation.ReportProcedures` decides it after the unit binds, beside
`UntilExitPlacement.Report`. The parse-tree ALTER prescan (`AlterDestinations`) is deleted: the bound walk reads the
BoundAlter entries. The choice of what is diagnosed (the named procedures only, docs/conformance/3.8) is unchanged.
Behaviour changes only at --std 85 (`RulesInForce`).

Tests: conformance pb6007-perform-reach-out-and-back-85, pb6007-merge-output-reach-out-and-back-85 (refused COBOL6877),
pb6007-detour-out-of-the-named-procedures-s1 (accepted, output A D C DONE); unit ProcedureReachFlowTests.
