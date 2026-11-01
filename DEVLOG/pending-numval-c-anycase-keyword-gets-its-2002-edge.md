## NUMVAL-C's ANYCASE keyword gets its 2002 edge (PB4359)

PB4308 gated the PHYSICAL phrase of LENGTH and BYTE-LENGTH and left one sibling as a lead: `FUNCTION NUMVAL-C(X "USD" ANYCASE)`
compiled and ran at `--std 85`, where ANYCASE is a user-defined word. `BindNumvalCFamily` gated the LOCALE keyword
(`numval-c-locale-phrase-2002`) and not its neighbour.

**Re-probe.** On the wave's build both `NUMVAL-C("$1,234.56" ANYCASE)` and `NUMVAL-C("1,234.56" "USD" ANYCASE)` printed
`+0001234.56` at 85. TEST-NUMVAL-C was already refused at 85 by its own COBOL1502, it being a 2002 function.

**The edge.** The note could not settle 85 without the 1989 amendment. The archived ISO/IEC 1989:2002 text (15.55.1,
15.76.1) prints `[ ANYCASE ]` in both formats, `reserved-words.json` records ANYCASE as reserved from 2002 (r85 false), and
2023 Annex E.3.3 item 1 only "clarified" it after 2014. The edge is 2002.

**Fix.** A new construct row `numval-c-anycase-keyword-2002` (one row for both functions: 15.94.3 r1 imports 15.68.3
whole) and one `ConstructRegistry.Check` in the ANYCASE arm of `BindNumvalCFamily`. 85 now answers COBOL0900; 2002 and later
are unchanged. Golden `pb4359_numval_c_anycase_2002` (2002, 2014, 2023) computes from 15.68.3 r4 f and 15.94.4: the fold
makes `"usd 1,234.56" "USD"` conform (1234.56, TEST 0), and the same call without ANYCASE reports position 1. Refusal
`pb4359-numval-c-anycase-below-2002` (85).

**Sweep.** FIND-STRING and SUBSTITUTE also write ANYCASE, but both are 2023 functions whose catalog gate covers the keyword.
The other function phrase words (LEADING, TRAILING, FIRST, LAST, START, AFTER, ANY) sit in functions introduced at or after
them. No inventory row closed (GAP 19 -> 19); FMT-15.68.2 and FMT-15.94.2 gained witnesses.

**Review round 1: the gate refused legal 85 source.** Below 2002 ANYCASE is a free word, so a COBOL-85 program may declare
`01 ANYCASE PIC X(3)` and write `NUMVAL-C("USD1,234.56" ANYCASE)` with the data item as argument-2. The ANYCASE arm took every
bare ANYCASE at slot 1 as the keyword and now answered COBOL0900 for that conforming program (before the gate it had silently
swallowed the operand). The fix is in the funnel, `IntrinsicBinder.KeywordWordOf`: a bare word that §8.9 leaves free at the
edition and that a declaration names is an operand (null keyword), the same determination the parser makes in
`IsKeywordReadingHere`; an undeclared free word keeps its keyword reading and the edition gate still answers. LOCALE, the
neighbouring 2002 word, had the same swallowing at 85 and is covered by the same line. Golden
`pb4359_numval_c_anycase_free_word_85` (accept 85) declares both words as data items: 1234.56 and 9.99.
