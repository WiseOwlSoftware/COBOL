## Intrinsic binder: PHYSICAL and the LOCALE phrase get their 2014 edge, and a function a COBOL-WORDS directive removed says so (PB4308, PB4309, PB4311)

Three defects shared one root file, `IntrinsicBinder.cs`, and one shape: a function phrase or a refusal that told the
user less than the standard does.

**PB4308 - `FUNCTION LENGTH(x PHYSICAL)` / `BYTE-LENGTH(x PHYSICAL)` at 85 and 2002.** Re-probed on the wave's build: clean
at 2002 (at 85 only BYTE-LENGTH's own COBOL1502 spoke). The printed 2002 formats are `( argument-1 )`; PHYSICAL is reserved
from 2014 (`reserved-words.json` r85/r2002 false, r2014 true), and 2023 Annex E lists no change to it. The PHYSICAL arm in
`BindLengthFamily` now asks `ConstructRegistry.Check` on a new row `length-physical-argument-2014` (one row for both
functions), so 85 and 2002 answer COBOL0900. Golden `pb4308_length_physical_2014` (2014, 2023) and two refusals.

**PB4311 - the LOCALE phrase of UPPER-CASE / LOWER-CASE was dated 2002.** Re-probed: `--std 2002` accepted it. The row's
2002 date rested on the 2023 Annex A.4.9 list, which names an optional module and records no edition. The printed 2002
15.97.2 / 15.57.2 have no phrase; the 2009 CD prints `[ LOCALE locale-name-1 ]` and its D.3 items 9 and 23 introduce it. The
row is renamed `case-function-locale-phrase-2014` (introducedIn 2014), every caller moved (the one binder site, the unit
theory, CONFORMANCE.md, DESIGN-locale-facility.md), `pb64t5_case_locale_phrase` is accepted from 2014 (its CHARACTER
CLASSIFICATION half stays witnessed at 2002 by `pb64t5_character_classification`), and a 2002 refusal was added. The
INITIALIZE half of the note was refuted by its own review and left alone.

**PB4309 - COBOL1501 for a name the user's own directive removed.** Re-probed: `UNDEFINE "LENGTH"` and
`SUBSTITUTE "LENGTH" BY "SIZEOF"` both drew "FUNCTION LENGTH is not an intrinsic function of ISO/IEC 1989", the sentence a
misspelling draws and false of LENGTH (15.50). `CobolWordsMap` now keeps the directive that removed each word
(`RemovedBy`, built with the de-reserved set, one structure) and the binder refuses with COBOL5776, naming the UNDEFINE or
SUBSTITUTE, its source line through the line map and the rule (7.3.10.4 GR3 / GR4; a SUBSTITUTE also names literal-5 as the
spelling that now denotes the function). COBOL1501 stays for a name that is no intrinsic function; the unit tests
`Intrinsic_Undefine_NoLongerAFunction`, `Intrinsic_Substitute_NewResolves_OldRemoved` and the new control
`Intrinsic_Misspelled_StaysNotAnIntrinsicFunction` pin both, and two refusals sit in the conformance suite.

Inventory: no row closed (GAP 19 -> 19); `record_verdicts` restated RV-15.57.4-2, RV-15.97.4-2, FMT-15.57.2, FMT-15.97.2
(editions and notes now 2014) and added witnesses to FMT-15.50.2 and FMT-15.14.2.

Sweep: the other keyword arms of the binder (TRIM, FIND-STRING, SUBSTITUTE, CONVERT, STANDARD-COMPARE) sit inside functions
introduced at or after the keyword. One sibling is left as a lead: NUMVAL-C's ANYCASE is accepted at `--std 85`. A second:
a de-reserved verb used as a verb (`UNDEFINE "DISPLAY"` then `DISPLAY "X"`) answers a bare COBOL0001 that never names the
directive (parser layer).
