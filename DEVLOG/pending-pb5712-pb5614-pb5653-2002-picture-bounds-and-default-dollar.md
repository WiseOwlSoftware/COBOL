## PB5712, PB5614, PB5653: COBOL-2002's floating-point edited bounds and default '$' sign, and a note retired

Wave 20262150 group D, the picture analyzer's three notes. Each was re-probed on this branch's build first.

**PB5712 (landed).** At `--std 2002` `PIC +9.9E+9999` and `PIC +9(32)E+99` compiled, because `PictureAnalyzer.AnalyzeFloatEdited` applied the later
editions' bounds (four exponent digits, 36 significand digits) at every edition. COBOL-2002 allows the exponent `+9`, `+99`, `+999` or `+9(n)`, n = 1 to 3
(§2002:13.16.38.3 GR12) and 1 to 31 digit positions (§2002:13.16.38.2 SR6); 2014 adds `+9999` and 36 (§2014:13.18.40.3 GR12, §2014:13.18.40.2 SR12).
The two bounds are now `EditionInfo.MaxFloatEditedExponentDigits` and `EditionInfo.MaxFloatEditedSignificandDigits`, which the analyzer asks, and the
refusal (COBOL1658) cites the running edition's rule. The fixed-point arms already refused 32 digit positions at 2002 (COBOL0801), so there was no sibling.
Goldens: `g2002-pic-float-edited-bounds-accepted` (31 digits and `+999`, sizes 38, 9, 7), `g2002-pic-float-edited-exponent-4-refused`,
`g2002-pic-float-edited-significand-32-refused`. Rows re-verdicted CONFORMS: 2002-GR-13.16.38.3-12 and 2002-SR-13.16.38.2-6 (GAP 1161 to 1159).

**PB5614 (landed).** `ConfigurationSlice.FinalizeCurrencySigns` added 2023 SR25's implied `CURRENCY SIGN '$' PICTURE SYMBOL '$'` at every edition, and the
class screen then refused `PIC $9.99 USAGE NATIONAL` (COBOL2883). The implied clause starts at COBOL-2014 (§2014:12.3.6.2 SR25); COBOL-2002's SR24 speaks
only of a written literal-7 (§2002:12.2.6.2 SR24) and a national item edits its insertion characters as national ones (§2002:13.16.38.3 GR2). The slice
now takes the edition: `EditionInfo.ImpliesDollarCurrencyClause` chooses `CurrencyDefinition.ImpliedDollar` or the new `CurrencyDefinition.DefaultDollar`
(`IsDefaultSign`), and `PictureAnalyzer` asks no class of a default sign. An explicit alphanumeric CURRENCY SIGN string on a national item is still
refused at 2002 and the implied `$` still at 2014 and 2023. The COBOL2883 message now cites the running edition's rule. Sibling sweep: the only readers of
the set's class are the analyzer and the repository encoder (which never decodes it back into the set). Goldens:
`g2002-default-currency-sign-national-edited` (`NC5=$1.25`), `g2002-alphanumeric-currency-string-national-refused`,
`pb5614-implied-dollar-national-refused-from-2014`; `pb1089-currency-symbol-literal7-class-usage` keeps its refusal at 2002 through NC2.
Row re-verdicted CONFORMS: 2002-SR-12.2.6.2-24 (GAP 1159 to 1158).

**PB5653 (retired).** Neither named site is reachable by a program: the DEBUG-SUB-n registers exist only at 85, where passing one to CALL USING is refused
COBOL1677 and 85 has no same-PICTURE rule, and the implied FD record is unnamed. The probes and the special-register siblings are in the note.

Self-review: the architecture lens (one register per edition fact, no inline year comparison, WOCA0001 held), the code lens (each message cites its
edition's rule, checked with `cite.py --check`), the performance lens (no allocation on a hot path; two integer properties) and the duplication lens (the
bounds are written once, in `EditionInfo`).
