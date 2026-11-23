## PB5712, PB5614, PB5653: COBOL-2002's floating-point edited bounds and default '$' sign, and a note retired

Wave 20262150 group D, the picture analyzer's three notes. Each was re-probed on this branch's build first.

**PB5712 (landed).** At `--std 2002` `PIC +9.9E+9999` and `PIC +9(32)E+99` compiled, because `PictureAnalyzer.AnalyzeFloatEdited` applied the later
editions' bounds (four exponent digits, 36 significand digits) at every edition. COBOL-2002 allows the exponent `+9`, `+99`, `+999` or `+9(n)`, n = 1 to 3
(§2002:13.16.38.3 GR12) and 1 to 31 digit positions (§2002:13.16.38.2 SR6); 2014 adds `+9999` and 36 (§2014:13.18.40.3 GR12, §2014:13.18.40.2 SR12).
The edge is one register row, `DialectBehavior.FloatEditedPictureWideBounds` (from 2014, VCR row 7.118), which the analyzer asks; the refusal (COBOL1658)
cites the running edition's rule. The fixed-point arms already refused 32 digit positions at 2002 (COBOL0801), so there was no sibling. First gate: the
implementor's own widest intermediate (`SendingValueTemp.WidestFloatEditedPic`, `+9.9(35)E+9999`) went through the same analyzer at the program's edition
and was refused at 2002 (`pb370_move_function_value_digits`); it is the implementor's item (§2023:14.9.25.4 GR1, "an intermediate result item provided by
the implementor"), not the program's PICTURE, so it is now analyzed at the newest edition. Two older 2002-accepted cases wrote 33 and 34-digit significands
(`pb4975_float_edited_wide_significand_quotient`, `pb4641_float_edited_receiver_quotient`), which 2002 does not allow; they are accepted from 2014.
Goldens: `g2002-pic-float-edited-bounds-accepted` (31 digits and `+999`, sizes 38, 9, 7), `g2002-pic-float-edited-exponent-4-refused`,
`g2002-pic-float-edited-significand-32-refused`. Rows re-verdicted CONFORMS: 2002-GR-13.16.38.3-12 and 2002-SR-13.16.38.2-6 (GAP 1161 to 1159).

**PB5614 (landed).** `ConfigurationSlice.FinalizeCurrencySigns` added 2023 SR25's implied `CURRENCY SIGN '$' PICTURE SYMBOL '$'` at every edition, and the
class screen then refused `PIC $9.99 USAGE NATIONAL` (COBOL2883). The implied clause starts at COBOL-2014 (§2014:12.3.6.2 SR25); COBOL-2002's SR24 speaks
only of a written literal-7 (§2002:12.2.6.2 SR24) and a national item edits its insertion characters as national ones (§2002:13.16.38.3 GR2). The edge is
`DialectBehavior.ImpliedDollarCurrencyClause` (from 2014, VCR row 7.119): below it `FinalizeCurrencySigns` joins `CurrencyDefinition.DefaultDollar`
(`IsDefaultSign`) and `PictureAnalyzer` asks no class of a default sign. The change made the default `$` differ from an explicit `"$"` under another
symbol in `StrongTypeModel.SameAnalyzedProfile` (`l1c34_strong_type_currency_string_match`, COBOL1688): exception 1 of §2023:8.5.3.1 compares currency
STRINGS, which `PicInfo.Clause` carries, and SR28 ties the class to the usage, so the class leaves that record compare. An explicit alphanumeric CURRENCY SIGN
string on a national item is still refused at 2002 and the implied `$` still at 2014 and 2023. The COBOL2883 message now cites the running edition's rule.
Sibling sweep: the only readers of the set's class are the analyzer and the repository encoder (which never decodes it back into the set). Goldens:
`g2002-default-currency-sign-national-edited` (`NC5=$1.25`), `g2002-alphanumeric-currency-string-national-refused`,
`pb5614-implied-dollar-national-refused-from-2014`; `pb1089-currency-symbol-literal7-class-usage` keeps its refusal at 2002 through NC2.
Row re-verdicted CONFORMS: 2002-SR-12.2.6.2-24 (GAP 1159 to 1158).

**PB5653 (retired).** Neither named site is reachable by a program: the DEBUG-SUB-n registers exist only at 85, where passing one to CALL USING is refused
COBOL1677 and 85 has no same-PICTURE rule, and the implied FD record is unnamed. The probes and the special-register siblings are in the note.

Gate 1 was RED on the citation audit (a SPECIAL-NAMES subject word in one golden, and a RESOLVE baseline entry the edit fixed, which `--write-baseline`
removed); gate 2 on the four cases above and on the inline-edition-comparison ratchet, which is why the two bounds are register rows and not `EditionInfo`
properties.

Self-review: the architecture lens (each edition edge is a register row with its VCR row, no inline year comparison; the implementor's intermediate is not
screened as a written PICTURE), the code lens (each message cites its edition's rule, checked with `cite.py --check`), the performance lens (the register
read is an array index; no allocation on a hot path) and the duplication lens (each bound is written once, in the analyzer's one arm).
