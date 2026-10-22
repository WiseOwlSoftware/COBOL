## Running past the last paragraph inside a PERFORM is the implicit GOBACK, not the PERFORM returning (PB3511)

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
