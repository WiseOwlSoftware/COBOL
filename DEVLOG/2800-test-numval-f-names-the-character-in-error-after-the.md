## Entry 2800 — 2026-10-30 12:30 PDT — TEST-NUMVAL-F names the character in error after the exponent's sign (PB4096)

Differential fuzz night 9 found `TEST-NUMVAL-F("12.34E+AB")` returning 10 and `TEST-NUMVAL-F("1E++2")` returning 6, where
ISO/IEC 1989:2023 §15.95.4 1) b) ("Otherwise, if one or more characters are in error, the position of the first character
in error") wants 8 and 4, and GnuCOBOL agrees. The exponent arm of `NvfScan` returned LENGTH + 1 (leg c) whenever no
exponent digit followed `E{+|-}`, even when a character stood there. PB121 gave the significand arm the end-of-text versus
stopped-on-a-character test and missed this arm.

**Re-probe** on the build, before the fix: `"12.34E+AB"` 10, `"1E++2"` 6, `"1E+"` 4, and two more the note did not list,
`"1E+ A"` 6 and `"1E-X"` 5.

**Fix.** `if (ed == 0)` now returns `Pos()` when text remains and `n + 1` only when the text ends there. The spaces after
the sign are consumed first, so a remaining character is never a space. Afterwards the probes read 8, 4, 4, 5, 5, 4.

**Sweep.** Every `n + 1` and `Length + 1` return in `CobolIntrinsics.Exact.cs` and `CobolIntrinsics.NumvalLocale.cs` was
read; each already separates end-of-text from a stopping character. NUMVAL-F's value twin and a literal argument ride the
same `NvfScan`, so no second scanner needed the fix.

**Goldens.** `15.95.4-Returned-value-rule/pb4096_test_numval_f_exponent_char_in_error` (accept 2002 onward, positions
hand-derived in its header, each shape with a literal and a data-item argument), and six rows in
`TestNumvalFPositionTests.Native_PositionLegs`. The edition refusal below 2002 is the existing
`test_validators_below_2002`. No inventory row was claimed or closed.

