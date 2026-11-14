## PB5484: a COBOL-85 arithmetic statement with no ON SIZE ERROR phrase ends only itself; a zero divisor or a zero base no longer ends the run unit

**The defect.** At `--std 85`, `COMPUTE R = Z ** 0` (Z = 0) and `COMPUTE R = 5 / Z` ended the run unit with
`abnormal run-unit termination: EC-SIZE-EXPONENTIATION (fatal)` / `EC-SIZE-ZERO-DIVIDE (fatal)`, exit 1, re-probed on this
tree before the fix. That is the 2002+ fatal exception condition, which 1985 does not have. §1985:VI.6.4.2 GR3 says that with no
phrase "the values of the affected resultant identifiers are undefined", the other resultants are stored as if no size error had
occurred, and "control is transferred to the end of the arithmetic statement". §1985:B.2.2 item 45 adds that "the program will
continue". GR1 and GR2 make the exponentiation violation and the zero divisor size error conditions like any other. The kernels
raise `CobolSizeError` whether or not checking is on (PB1605), and at 85 nothing caught the raise.

**The fix.** I added one edition fact, `EditionInfo.UnphrasedSizeErrorEndsOnlyTheStatement` (Year < 2002). When
`ArithmeticEmitter.EmitArith` emits a COBOL-85 arithmetic statement with no phrase, it uses the two-phase shape that a statement
with the phrase uses, with two settings. No condition is checked (`SizeChecks.None`), so every render and landing stays the
unchecked one and an overflowing resultant still keeps its low-order digits. Every raise is caught. A resultant whose value
cannot be formed keeps its content (GnuCOBOL 3.2's disposition, recorded in DOC-A.1-70), the per-receiver try still stores the
other resultants, and the next statement runs. A debugging section for an item that kept its content does not run (§1985:XV.3.2.4
GR6; probed). A reference-modifier position expression rides `EmitArith` but is not an arithmetic statement, so it keeps the
termination, and so does a 1985 condition (`IF 5 / Z > 1`, probed). The condition case is reported as a lead.

**Witnesses.** The golden `cases/VI.6.4@1985-COMMON-OPTIONS-AND-RULES-FOR-STATEMENTS/pb5484_no_phrase_size_error_continues` runs
the change record's own STATEMENT-1 and STATEMENT-2 (DIGIT2 = 10, and the program continues). It also runs a zero base, a power
with no real result, a nested quotient and DIVIDE GIVING REMAINDER by zero. It displays only values the standard defines. I added
`SizeErrorDispositionTests.Cobol85_NoPhraseSizeError_KeepsTheResultantsAndContinues` (7 rows, pinning the kept value) and its
control `Cobol85_NoSizeError_StoresTheResultants`. Row 1985-GR-VI.6.4.2-3 was CONFORMS, but its notes claimed the EC-SIZE
termination was unreachable at 85. Batch `w20262025a-pb5484-1985-no-phrase-size-error` restates those notes and adds the code
site and the witnesses (1985 GAP 232 -> 232). I updated DOC-A.1-70, NUMERIC_DESIGN.md and the `CobolSizeError` docs.
