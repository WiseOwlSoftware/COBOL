## PB4420: the fold of a user-defined word applies only an extended letter's own case pair

**What the owner decided (2026-11-01, "Follow gnucobol").** ISO §8.1.3.2 4) b) folds an uppercase extended letter to "its
corresponding lowercase extended letter, if any". Annex C.2 lists two pairs whose lowercase is a BASIC letter, `(0130,0069)` LATIN
CAPITAL LETTER I WITH DOT ABOVE to `i` and `(212A,006B)` KELVIN SIGN to `k`; GnuCOBOL 3.2 compares extended letters byte for
byte, so `CUSTOMER-<U+0130>D` and `CUSTOMER-ID` are two names there. Under CLAUDE.md rule 1 (ISO, then GnuCOBOL) the standard's
wording leaves that latitude, and WiseOwl now follows GnuCOBOL.

**Re-probe on a Debug build of main (19:32 PST).** `01 CUSTOMER-<U+0130>D` beside `01 CUSTOMER-ID`: COBOL1639 on both
references ("2 declarations share the name"). `CUSTOMER-<U+0130>D` alone read as `CUSTOMER-ID`: printed its value (one name,
silent wrong answer). `WORK-<U+212A>` read as `WORK-K`: printed. The four compiler sites the PB4240 finisher listed
accepted or refused wrongly the same way: a constant `F<U+0130>LLER` was FILLER (COBOL1547 + COBOL0901), `L<U+0130>NE-COUNTER`
a report subscript was the barred register (COBOL2751 + COBOL0901), `CONVERT(XM ANY NAT<U+0130>ONAL HEX)` was NATIONAL, and
`XM IS NUMER<U+0130>C` drew COBOL0901. After the change: two items (`DOTI`, `BASI`), the other spelling COBOL1639, the constant and the
subscript compile as ordinary user words, the CONVERT format word is COBOL1514 and the class condition COBOL1639 alone.

**The shape.** The exclusion is derived, not listed. `scripts/spec/gen_annex_bc.py` takes the three pair lists it transcribes (C.2,
E.2 item 14, E.3.3 item 6), moves every pair whose letters are not both basic or both extended into a new list,
`CobolNames.NotApplied`, and emits the other pairs as before, so `CobolNames.Fold`, `Same`, `UpperFold` and every fold-keyed table
needed no change. The derived set is the two pairs above and, in the 2002/2014 table, `(0131,0069)`: the dotless i is the same
pair class (extended to basic), so below 2023 it no longer folds to `i` either, and `EditionSpelling` now respells only final
sigma. The note named the first two; the third follows from the rule and is the one reading of 4) b) at every edition (the
2002 and 2014 wording is the same).

**Proof that no keyword equals an extended spelling through the fold.** `AnnexBCDriftTests` (a) re-reads the three lists from the
spec and requires the applied lists to be the standard's less the crossing pairs and `NotApplied` to be those pairs, derived in
the test independently of the generator; (b) pins the measured set; (c) requires that no character at or above U+0080 folds to
an ASCII character at 2002, 2014 or 2023 (`NoCharacterAboveAscii_FoldsToAscii`); (d) requires every keyword population (the
§8.9 reserved words, §8.10 context-sensitive words, the intrinsic function names, the implementor names and the §8.12 directive
words) to be basic characters only (`EveryKeywordPopulation_IsBasicCharactersOnly`). With (c) and (d) the roughly thirty
compiler sites that ask `CobolNames.Same` / `UpperFold` about a keyword literal (the PB4240 lead) and the fold-keyed word tables
answer exactly as the basic-letter comparison does, so they stay.

**Dead PB4240-era code removed.** `FunctionWord.OfUserWord` (the third factory, which folded the user word and then asked the
function catalog; the declaration funnel `DeclareUserWord` now asks `OfWrittenWord`), the fold-then-keyword guards in
`VersionConformancePass.ValidateCobolWords` and `IsReservedContextOrIntrinsic`, and `ReservedWordSet.Compose`'s re-keying of the
de-reserved words by the fold. `CobolKeywords` stays as the keyword question's home (no table, allocation-free, the directive
words' spelling as written); its comment and `CobolWordsMap`'s no longer claim that the fold turns U+0130 into `i`.

**Goldens** (`cases/8.1.3-COBOL-character-repertoire/`, each quoted from the gate's `Conformance.trx` below):
`pb4420-dotted-capital-i-name-is-a-word-of-its-own` (three editions), `pb4420-kelvin-sign-name-is-a-word-of-its-own`,
`pb4420-dotted-capital-i-name-other-spelling-undefined` and `pb4420-kelvin-sign-name-other-spelling-undefined` (COBOL1639),
four site goldens (`pb4420-filler-...-constant-name`, `pb4420-line-counter-...-user-word`, `pb4420-convert-format-word-...`,
`pb4420-class-condition-keyword-...`), and the four former refusals now acceptances (`pb4240-reserved-word-...`,
`pb4240-all-intrinsic-name-...`, `pb4240-named-intrinsic-name-...` and `pb1965-kelvin-spelled-reserved-word`, deleted with their
refusal files and re-written as `pb4420-...-is-a-user-word`). `pb1402_extended_letters` (the dotless i and `ITEM-A` are two
items), `pb1402_extended_letters_2023` (the Kelvin sign is its own word) and `pb1965_annex_c_word_keys` (the EQUATE synonym is
`K<U+00DC>P`/`K<U+00FC>P`) are re-derived; `ParameterVariableNameTests`, `PointerImageTests` and `CobolNameFoldDriftTests` follow.

**Docs.** The `CobolNames` class comment (it stated the opposite), DESIGN-frontend-grammar 3.13, DESIGN-cobol-words-directive,
and docs/CONFORMANCE.md DOC-A.1-20 (the 4 b) determination, GnuCOBOL by the precedence). Inventory rows GR-8.1.3.2-4 and
DOC-A.1-20 stay CONFORMS, restated by a `record_verdicts` batch (GAP 19 to 19; the Kelvin refusal golden retired).

**Gate.** Run `000002-20261102T065957Z` on the code head (run 000001 was RED on one finding of my own, `AnnexA1RegisterDriftTests`: a prose
mention of the row key `DOC-A.1-20` in CONFORMANCE.md; reworded):
`=== BUILD-LOCAL GATE: GREEN — Conformance 20,725/20,725 · Unit 39,210/39,210 · Characterization 37/37 cases ran (skipped 0) ===`.
`Conformance.trx` holds 28 `UnitTestResult`s for the new ids, all Passed (18 accepted-edition runs, 10 refusals). `semgrep/verify.py`: PASS,
no rule's count rose. Left open and registered: PB4312's U+0130/U+212A examples no longer reproduce (the diagnostics print the word as
written because the fold no longer maps them), and `CobolWordsDirectiveProcessor.Spelled`'s keyword/user-word split now differs only in
how an extended letter is spelled in a diagnostic.
