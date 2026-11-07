## PB4901: the CALL USING screen cites the 1985 rule at COBOL-85, and SET DEBUG-SUB-1 UP BY 1 is confirmed a correct refusal

Wave 20261905 F, one note. Re-probe at `--std 85`: both repros reproduce (`CALL "X" USING DEBUG-NAME` is COBOL1677, `SET DEBUG-SUB-1 UP BY 1` is COBOL2112).

CALL. `§1985:X.5.2.3 SR3` requires every USING operand to be "defined as a data item in the File Section, Working-Storage Section, Communication Section, or Linkage Section"; the '85 DEBUG-ITEM has only an implicit description (`§1985:XV.3.2.4 GR10`), so the refusal is correct and GnuCOBOL's leniency is not authority. The defect was the diagnostic: it cited the 2023 rule, and the screen skipped a Format 2 BY CONTENT operand that the 1985 rule also names. `CallBinder.ScreenCallOperand` now screens every operand at 85 under `§1985:X.5.2.3 SR3`; the `CallOperandSection` description names the rule. Golden: `pb4901-call-debug-register-at-85` (refused at 85, both formats and modes).

SET. `§1985:VI.6.23.3` Format 2 takes only index-names for UP/DOWN BY; DEBUG-SUB-1 is `S9999 SIGN LEADING SEPARATE`. The refusal is correct; discharged. Residual lead: the COBOL2112 text lists the 2023 formats at 85.

No inventory rows closed.
