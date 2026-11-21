## Fixed on train85b-seg (wave 20262131 group b), with PB6007: the flow stops at the range's return mechanism

Re-probed at --std 85 on the base build (be6c60fd80): s19_p17_return_outside_after compiled (exit 0, the defect) and
s19_p17b_return_outside_before was refused COBOL7081. On the fix both are refused COBOL7081.

Root cause: `ProcedureReach.Closure(followGoTo: true)` took every GO TO destination to flow on to the END OF THE PROGRAM,
so the GO TO O20-X inside the output procedure reached every section written after it. The 1985 derivation of the range:
it "includes all statements that are executed as the result of a transfer of control by CALL, EXIT, GO TO, and PERFORM
statements in the range of the output procedure" (§1985:XI.4.4.4 GR10; §1985:XI.4.1.4 GR8 for MERGE), and "When control
passes the last statement in the output procedure, the return mechanism provides for termination of the merge"
(§1985:XI.4.1.4 GR9; §1985:XI.4.4.4 GR8 and GR11 for the SORT's procedures). So a destination's sequential flow ends at
the range's last paragraph, at an unconditional GO TO (§1985:VI.6.15.4 GR1) or at STOP RUN (§1985:VI.6.24.4 GR1): each
step of `ProcedureReach.Walk` carries the return point of the range that reached it. Every citation ran `cite.py --check`
OK. Readers affected: `GateReleaseReturnRange` and `CalledFromRange` (both 85 only); `UntilExitPlacement` and
`GateBeforeReportingPerformRange` do not follow GO TO and are unchanged.

Tests: conformance pb6008-return-in-unreached-section-after-85, pb6008-release-in-unreached-section-after-85 (refused
COBOL7081), pb6008_return_through_fall_through_85 (accepted, output H1 AAA BBB); unit ProcedureReachFlowTests.
