## Entry 2673 — 2026-10-27 15:16 PDT — PB3952 - NUMVAL-F "1.5E2" is refused by the standard, and both functions already agree: the determination is written and pinned

The note said FUNCTION NUMVAL-F answers 0 for `"1.5E2"` (no sign on the exponent) where GnuCOBOL answers 150 and asked which
reading is right. Re-probe on a fresh build: NUMVAL-F of `"1.5E2"` is 0 into a fixed receiver, a COMP-2 receiver and DISPLAY;
TEST-NUMVAL-F of it is 5; the signed `"1.5E+2"` is 150 and `"1.5e-2"` is 0.015. ISO 15.69.3 r1 makes the exponent sign a
required choice (braces in the figure), so the text violates the argument rules: 15.3 r14 sets EC-ARGUMENT-FUNCTION and leaves the
unchecked result to the implementor, which A.1 item 90 fixes as the zero value of the numeric type. TEST-NUMVAL-F (15.95.1) must
refuse what NUMVAL-F refuses, and both ride the one `NvfScan`, so making NUMVAL-F lenient like GnuCOBOL would split them.
Decision: keep the refusal. No compiler code changed. Added `pb3952_numval_f_exponent_sign_required` (conformance golden, values
computed from the rule: R=0 and TEST positions 5/5/3 for `1.5E2`/`1.5e2`/`1E2`, signed controls 150 and 0.015), two
`TestNumvalFPositionTests` rows and the determination in docs/CONFORMANCE.md beside PB256. PB3952 landed; it claims and closes
no inventory row. Self-review: no code in the diff; the refusal below 2002 is already witnessed by `pb3839_numval_f_below_2002`.

