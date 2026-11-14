- **D-SR8 — what "with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER
  phrase" reaches (kb/Work PB434, row `SR-14.9.28.3-8`).** §2023:14.9.28.3 SR8: "The UNTIL EXIT phrase shall not be specified
  in a PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST
  AFTER phrase". (1) **A TEST phrase counts only when WRITTEN.** SR1's assumed TEST BEFORE ("If neither the TEST BEFORE
  nor the TEST AFTER phrase is specified, the TEST BEFORE phrase is assumed") governs the until-phrase UNTIL EXIT itself
  is, so counting it would make SR8 forbid every UNTIL EXIT; a TIMES PERFORM and an until-phrase with no TEST written do
  not bar it. (2) **"With" includes the superset spelling** `VARYING … UNTIL EXIT` (and an AFTER level's `UNTIL EXIT`):
  the varying-phrase prints `UNTIL condition-1` only, and SR8's first conjunct is exactly that spelling — refused by name
  (COBOL2954) at every edition rather than as a bare parse error. (3) **"Under" is the PERFORM's range** (§2023:14.9.28.4
  GR1, "The range includes all statements that are executed as the result of a transfer of control in the range of the
  PERFORM statement"), decided statically: an inline PERFORM's imperative-statement-1 at any depth, an out-of-line
  PERFORM's specified set (§2023:14.9.28.4 GR4, both physical orders of a GR6 inverted THRU), closed over every transfer that
  RETURNS — an out-of-line PERFORM, a SORT/MERGE INPUT or OUTPUT PROCEDURE, an exception-checking PERFORM's handler
  (`ProcedureReach`). A GO TO's destination and a declarative are NOT followed: where control goes after a GO TO leaves a
  range, and whether an I/O failure runs a USE procedure, are run-time facts, and following them statically would refuse
  statements no execution reaches under the PERFORM. GnuCOBOL 3.2.0 enforces neither half (measured, wave 1021 C), so no
  vendor behavior informs the reading. Witnesses: `conformance:cases/14.9.28-PERFORM/pb434-until-exit-under-varying`,
  `conformance:cases/14.9.28-PERFORM/pb434-varying-until-exit`, `conformance:cases/14.9.28-PERFORM/pb434_until_exit_unbarred`,
  `conformance-test:PerformUntilExitPlacementTests.UntilExit_WithOrUnderAVaryingOrTestPerform_IsRefusedOnce`,
  `conformance-test:PerformUntilExitPlacementTests.UntilExit_InASortInputProcedureUnderAVaryingPerform_IsRefusedOnce`,
  `conformance-test:PerformUntilExitPlacementTests.UntilExit_NotUnderAVaryingOrTestPerform_IsAccepted` (thirteen refused
  shapes, each refused exactly once; four admitted).
