## Entry 3555 — 2026-11-12 16:47 PST — PB5182: at --std 85 DIVIDE ... REMAINDER forms the remainder from the sign presence of identifier-3

**Re-probe.** `DIVIDE DVD BY 3 GIVING QU REMAINDER RM` with DVD = -11, QU `PIC 9`, RM `PIC S99` stored Q=3 R=-02 at --std 85 and at 2023 (also with ROUNDED, and for `PIC Z9`). §1985:VI.6.11.4 GR6 gives the intermediate field the "same presence or absence of a sign as the quotient (identifier-3)", and an unsigned receiver holds the absolute value (§1985:VI.6.19.4), so 1985 gives -11 - 3 x 3 = -20. From 2002 the subsidiary quotient is always signed (§2002:14.8.11.3 GR6c, §2002:F.1 records the change), which is the -02.

**Fix.** `ArithmeticEmitter.UnsignedSubsidiary`, applied once in `EmitDivideRemainder` to the subsidiary quotient of every lane (native Int128, SDIDI, exact), takes its absolute value when the edition is 1985, identifier-3 is fixed-point and it holds no sign. The sign question is now one property, `PicInfo.HoldsSign` (`S`, or a `+ - CR DB` in an edited mask, or a LOCALE item's `+`); two earlier private copies of the edited-mask test (VALUE SR3 in `DataBinder`, `AlgebraicRange`) read it. After the fix: 85 gives -20 for unsigned and ROUNDED and `PIC Z9`, -2 for `PIC S9` and `PIC -9`, -21 for `PIC 9V9` (3.6 x 3 = 10.8); 2002 and later are unchanged (-2, -2, -2, -2, -2, 0).

**Goldens.** `pb5182-unsigned-quotient-remainder-1985` (85) and `pb5182-unsigned-quotient-remainder-signed-subsidiary` (2002, 2014, 2023): six legs each, compared through IF because the portability lint refuses a DISPLAY of a numeric item and a numeric-edited item compares as alphanumeric (a MOVE to a numeric item de-edits it first).

**Rows.** None closed: the note claimed no inventory row (kb/Work PB5094 keeps 1985 rule ids out of the register's row lists). `docs/NUMERIC_DESIGN.md` states the edition's sign rule.

