## Entry 2358 — 2026-10-22 12:38 PDT — Running past the last paragraph inside a PERFORM is the implicit GOBACK, not the PERFORM returning (PB3511)

The second overnight fuzz run found fz00481 hanging: a `PERFORM P1` whose P1 says `GO TO P3`, with P3 the last paragraph,
printed `P1`, `P3` and then `AFTER PERFORM`, and the wrong return re-entered later PERFORMs of the fuzz program. The
re-probe on this branch's base (aef934867) reproduced all three of the note's shapes: the GO TO out of the range, the same
fall-off two PERFORM levels deep (`AFTER B1`, `AFTER A1` printed), and `PERFORM P2 THRU P1` with P1 written first
(`P2`, `AFTER PERFORM`).

ISO 14.6.3: "There is also no next executable statement after the last statement in a source element when the procedure
in which it appears is not being executed under the control of some other COBOL statement in that source element ... an
implicit GOBACK statement without any optional phrases is executed." The dispatcher had no such arm: every dispatch call
looped while `__pc < __N` and returned `__pc` when it ran off the end, and an out-of-line PERFORM's nested call read that
return as its range completing.

`DispatchEmitter.EmitDispatchMethod` now emits, after the at-exit return and the transfer label, `if (__pc == LAST + 1)
throw new ProgramReturn();` (`MethodReturn` in a method), LAST being the source element's last paragraph. That is the
path an explicit GOBACK takes, so the throw unwinds every nested frame to the activation entry: a main program ends, a
CALLed program returns to the CALL, a function returns its RETURNING item, a method returns to the INVOKE. Fall-through,
EXIT PARAGRAPH, EXIT SECTION and NEXT SENTENCE in the last paragraph all reach the test. The activation's own call is now
bounded at LAST (`__Dispatch(EntryPc, LAST)`, replacing the literal `-1`), so a program that simply ends still returns
through the at-exit test and pays no exception. A program's Format-3 handler cases moved to the dispatch method's second
case set, so LAST sits below them; the old loop (read, not measured) let a GO TO out of a nested PERFORM run from the
last paragraph on into the synthetic WHEN-phrase handler cases. The inverted THRU never reaches 14.9.28.4 GR5 b)'s return mechanism under any reading; whether
14.6.3's sentence covers it the standard does not decide, so docs/CONFORMANCE.md D-PTHRU records GnuCOBOL 3.2's answer,
the implicit GOBACK. COBOLNET_DESIGN §5.1 shows the new shape, and every emitted C# snapshot gained the one line.

Six goldens in `cases/14.6.3-Explicit-and-implicit-transfers-of-control/` pin it: the GO TO case, two PERFORMs deep, the
inverted THRU, a CALLed program (called twice, plus an EXIT PARAGRAPH variant), a FUNCTION-ID and an OO method in one
run, and a COBOL 2023 program with an exception-checking PERFORM. Probes also covered the ALTERed GO TO at --std 85. The
batch w1147b-pb3511-implicit-goback-witnesses adds them to GR-14.6.3-3 and GR-14.9.28.4-1/5/6 (all already OK, GAP 66 ->
66). Self-review found the return-signal type spelled twice in DispatchEmitter; `ReturnSignal` now serves both.

Gate (run 20261022T175037Z-912edc): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left to
CI's whole-population run ===`, with all six goldens Passed at 2002 (2023 for the Format-3 case) in leg 1.

Branch review round 1 (finding R1-1) found the control-flow deep-dive, COBOLNET_CONTROL_FLOW_DESIGN.md, still describing
the old dispatcher: the activation called with exit pc -1 (D10 and both C# sketches), and the last paragraph's fall-off
exiting the loop as if every caller ended there (D2, the edge cases, the GO-TO-out-of-range hard problem). It now gives
the activation bounded at LAST, the implicit-GOBACK arm in both sketches, and the two cases of running off the last
paragraph (the activation's own return; a nested call's implicit GOBACK). The sibling sweep found the same -1 activation
in COBOLNET_PIPELINE_DESIGN.md's control-flow sketch, rewritten the same way. The ALTER edge case now says what a
never-ALTERed target-less GO TO does inside a PERFORM (it returns to the performer, unchanged by this branch).

The landing's Linux gate stopped on one conformance case outside leg 1: `pb1277_record_size_in_bytes` (2002) ended
after `FA=00 ...` instead of printing `SORTED=2`. Its SORT named `OUTPUT PROCEDURE IS RET-SN` alone, and RET-SN's AT END
said `GO TO RET-SN-EXIT`, the program's last paragraph: control left the output procedure and never reached its return
mechanism (14.9.40.4 GR14), so running past the last paragraph is now the implicit GOBACK, which GnuCOBOL 3.2 also
executes (measured under WSL). The expected output had rested on the old fall-off; the Windows gate passed because the
case is not in leg 1. The case now names `RET-SN THRU RET-SN-EXIT`, the procedure its derivation describes, and prints
`SORTED=2` again. Two goldens pin the SORT arm the case exposed (an input procedure and an output procedure left by a
GO TO), D-PTHRU in docs/CONFORMANCE.md now covers SORT procedures, and batch w1147b-pb3511-sort-procedure-witnesses adds
them to GR-14.6.3-3 and GR-14.9.40.4-11/13/14 (GAP 66 -> 66).

A second finisher sent to the same stop swept the other half of the question: a bounded call whose range ENDS at the
last paragraph must complete there, not take the implicit GOBACK. It does for every form, because the dispatcher tests
the at-exit return before the end-of-element arm; `pb3511_sort_merge_procedure_ends_at_last_paragraph` pins SORT input
and output procedures, a MERGE output procedure and a PERFORM, each ending at the last paragraph, at all four editions.
`pb3511_implicit_goback_sort_merge_called_program` pins the abnormal paths in a called program: a SORT input or output
procedure or a MERGE output procedure left for the last paragraph returns to the CALL, the next SORT still runs, and the
abandoned MERGE's USING files stay open, because 14.9.24.4 GR7 c) closes them only after control passes the output
procedure's last statement (the next OPEN OUTPUT gets 41; GnuCOBOL 3.2 closes them early and gets 00). The control-flow
design's edge cases now state the ordering, and D-PTHRU names the MERGE case.

