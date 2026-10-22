## Entry 2338 — 2026-10-21 22:37 PDT — PB3467 decided: DISPLAY of a numeric item follows GnuCOBOL's default form; PB3505 filed to implement it

**Decision.** The owner answered PB3467 "yes" (2026-10-21 ~22:12 PDT): the §14.9.11.4 GR1 conversion of a numeric
DISPLAY operand (Annex A.1 item 56) defaults to GnuCOBOL 3.2.0's default-configuration form, `00012.50` for
`PIC 9(5)V99` holding 12.5 and `+00012.50` / `-00012.50` signed, and today's digit image (`0001250`, `000125{`) stays
behind a compile option.

**Evidence.** GnuCOBOL 3.2.0 was run under WSL in its `default`, `cobol2014`, `mf`, `ibm` and `mvs` configurations: the
first three print the decimal point and a leading sign, and the IBM-style ones print digits only. IBM Enterprise COBOL
6.4's documentation shows digits with an overpunched sign under its default `DISPSIGN(COMPAT)`. The Micro Focus
documentation did not load, so its row stays unverified. The note's claim that PB803 pulled the other way was wrong:
PB803 decided how an unseparated sign is STORED (§13.18.52.4 GR4), a different latitude, and DOC-A.1-56 already
followed GnuCOBOL for this one (numeric literals as written).

**Next.** PB3505 carries the implementation and its bar: survey every numeric usage and sign form by running
GnuCOBOL, one conversion selected by a compile option, goldens computed from the surveyed rule, and the fuzz harness's
display-form fold revisited. The owner made it tonight's work, past the weekly allowance.

