## The second overnight fuzz run, triaged: three new defects filed (PB3510 crash, PB3511 and PB3512 wrong answers)

**What.** The second differential fuzz night (`conformance-suite/results/fuzz-20261021-211424`: qwen3.6:35b, seed
20261022, 8 hours, 4,931 programs, judge built from main at 20:38 on 2026-10-21, before PB3489, PB3504 and PB3505 landed)
ended at 05:14. Survivors: 1,260 programs in 314 signatures. This landing files the three WiseOwl defects it found, each
reduced and re-confirmed on a fresh build of main at fd407980f:

- **PB3510, a compiler crash.** `OCCURS … DEPENDING ON` naming the group that CONTAINS the table overflows the binder's
  stack (`ReferenceResolver.WrapIfOdoGroup` resolves the depending item, which is the group it is wrapping, which resolves
  the depending item …). §13.18.38.3 SR17 and SR20 forbid the program and their diagnostics (COBOLNET0852, COBOLNET0857)
  exist, but the crash comes first. GnuCOBOL refused the fuzz program for its other fault (a PICTURE on the group).
- **PB3511, a wrong answer.** A GO TO out of a PERFORM's range that runs to the end of the procedure division returns to
  the PERFORM instead of executing the implicit GOBACK of §14.6.3 item 3: `DispatchEmitter`'s nested `__Dispatch` returns
  `__pc` at `__N` and the performer reads that as its range's completion. GnuCOBOL ends the run; WiseOwl prints the
  statement after the PERFORM. The fuzz saw it as a hang (the wrong return re-entered a genuine infinite loop later in the
  program). A second witness with no GO TO at all, `PERFORM P2 THRU P1` whose exit is never reached, came from the
  output-difference rows.
- **PB3512, a wrong answer.** `MOVE` of a signed numeric item into a REFERENCE-MODIFIED alphanumeric receiver writes the
  sender's raw stored form, sign included: `2468` arrives as `246H`, a packed `-1234` as `-1234`, a SIGN SEPARATE item with
  its sign character. §14.9.25.4 GR6 a) says the operational sign is not moved, and §8.4.3.3.4 GR6 makes the slice an
  elementary receiver. `MoveClassifier.Kind` routes every `RefModPlace` receiver to the `RefModSlice` arm before the
  sender's category is classified; the un-modified receiver is right.

**How the run was read.** The crash-class rows first: the three compile hangs (fz04522, fz00062, fz02549) are the landed
PB3489/PB3504 shapes and refuse in under a second on main; fz03769 is the model's own `GO TO MAIN-PARA` loop; the five
EXIT_DIFFERS rows are the first night's two classes (STOP RUN 1234 against 210, a NULL BASED pointer). Every
OUTPUT_DIFFERS, EXIT_DIFFERS and THEY_FAIL_RUN row (392) was re-run on the fresh judge against GnuCOBOL's recorded output:
144 now agree (PB3505's display form), 242 still differ, 6 differ in exit status. Those 100 signatures and the 707
WiseOwl-refusal rows were then read by Opus adjudicators (owner, 10:00: "We should be using the appropriate models
(Opus, Sonnet, etc.) for the work"), one verdict per signature with a checked citation, the Mythos session re-checking
each defect before filing: the §8.x residue (52 signatures) is 20 latitude, 14 uninitialized items, 8 GnuCOBOL-side, 6
WiseOwl-correct, 1 defect (PB3512, two witnesses); the §13/§14 residue (48) is 19 uninitialized, 15 latitude, 8
WiseOwl-correct, 3 GnuCOBOL-side, 1 PB3511 witness; the WiseOwl refusals (123 signatures) are 118 WiseOwl-correct by the
cited rule, 3 latitude, 2 model errors, none a refusal of legal source. The rows that looked worst on their face were not
defects: the DIVIDE results of zero were an uninitialized receiver, the EXTERNAL values are §13.18.63.4 GR4 a) (an EXTERNAL
item's VALUE takes effect on INITIALIZE), the float-to-fixed last digit is the documented truncation of the exact binary64
value. The GnuCOBOL-side refusal rows (THEY_REJECT 154, THEY_FAIL_RUN 19) are being classified by a last adjudicator; the
experiment record for the night follows with them.

**Why it matters.** The first night found a wrong answer in a control-flow shape no suite case had; the second found two
more of the same kind (the end of the procedure division inside a PERFORM, a reference-modified MOVE receiver) and a crash
on an illegal program. The fuzz keeps writing the programs nobody thought to write, and after PB3505 half of its output
noise is gone.

**Process.** Notes filed by a Sonnet clerk from the session's structured findings; verdicts in the session scratchpad
(`adj/{A,B,C}/verdicts-*.jsonl`); the fresh judge in the detached worktree `E:\COBOL-wt\fuzz-triage-20261022`.
