## Entry 3093 — 2026-11-04 12:17 PST — PB4239, PB4586, PB4577: the COBOL-WORDS category rules know the extension words and survive a parse error, and SEARCH identifier-1 takes enclosing subscripts only from COBOL-2014

Wave 20261863 group A took three notes that touch the version-conformance code. They are three mechanisms, fixed one per
checkpoint.

**PB4239, the SR3/SR4 population.** `>>COBOL-WORDS UNDEFINE "JSON"` (also XML, COMP-3, ENTRY and the other 11 words the §4.2.10
register reserves) drew COBOL1623 "is not a reserved word", while `01 JSON` drew COBOL0313 "reserved extension". The owner had
decided on 2026-10-31 that §7.3.10.3 SR3's "reserved word" includes the implementor's additions, and the determination of wave
20261861 d read the standard the same way (§8.3.2.1 1), §7.3.10.1, D.12.1, D.12.3). The re-probe on the pre-fix build reproduced
all of it, and found the other side: `RESERVE "JSON"` and `EQUATE "MOVE" WITH "COMP-3"` compiled clean, though SR4 refuses a
reserved word as literal-2, -5 or -6. Both sides had one cause, the single SR3/SR4 predicate listing only §8.9, §8.10 and §8.11.
The fix is `NonstandardExtensionRegister.ReservesWord`, a lazy set over the register rows' `ReservedWords` compared by the
keyword fold, as the fourth disjunct of that predicate, so the next `extensionReserved` word reaches both rules with no edit.
The SR4 sentence for such a word names §4.2.10 and D-RW1. The parser's COBOL0313 vendor hint for the JSON and XML tokens, which
said only "JSON GENERATE/PARSE is not an ISO construct", now says the word is reserved for a nonstandard extension and so is no
user-defined word, and names a COBOL-WORDS synonym as written (`01 JS` after `EQUATE "JSON" WITH "JS"`, the witness PB4573
left). Goldens: three run cases (`pb4239_cobol_words_undefine_json_is_a_data_name`, `..._undefine_comp_3_keeps_computational_3`,
`..._substitute_comp_3`) and three negatives. The unit theories iterate the register, so a fixed list of the three printed
lists can no longer let words fall through.

**PB4586, the verdict lost behind a parse error.** SR3/SR4 ran inside `VersionConformancePass`, which a failed parse never
reaches, so `UNDEFINE "AUTHOR"` plus `MOVE TO.` reported only the parse error. The rules need no tree. They are now
`CobolWordsCategoryPass`, run by `CompilerDriver.Compile` on the directive stage's map before the frontend gate (the edition
context is built first), and the frontend-error result carries the edition sink's diagnostics beside the parse errors.

**PB4577, SEARCH identifier-1 below 2014.** `SEARCH H (GI)`, GI subscripting the table that encloses H, compiled clean at 85
and 2002, whose SEARCH rule says "Identifier-1 shall not be subscripted or reference modified". The edge is 2014 (the owner
revised it on 2026-11-04). It is gated like the 14 PB4305 relaxations, as a `constructs.json` row
(`search-identifier-1-enclosing-subscript-2014`, COBOL0900, VCR row 7.49). A first version asked it in
`StatementValidation.ResolveSearchTable` and the gate's own drift test (`EditionGateArmDriftTests`, PB353) went red: with the
table name undefined the 2002 compile named no edition. It now sits in the parse arm of `VersionConformancePass` (one body for
both SEARCH visitors), on recognition of a subscript on identifier-1; a subscript at the searched level also names 2014 below
it, beside the COBOL2075 that refuses it at every edition. The note's "COBOL2075" was not the right shape for the edition edge. Three suite goldens that wrote the enclosing subscript at `accept: [85]`
moved to `[2014]` with identical output, and the bare spelling (the CCVS-85 form) has its own 85 golden.

Rows: SR-7.3.10.3-3 and -4 closed by PB4239 (re-witnessed; verdicts stay CONFORMS), SR-14.9.37.3-2 and -3 re-witnessed under
PB4577. Self-review found the second sentence of the COBOL0313 report on a reserved word in a data-name position ("'JS' is not a
clause of the data description entry") is the closed-format residue hint's wording for any reserved word, which is a separate
mechanism and is left for the registrar.

Review round 1. The reviewer found the COBOL0313 sentence for a JSON or XML synonym always cited EQUATE's GR2 ("neither is a
user-defined word"), but the retyped token also arises from SUBSTITUTE, where GR4 frees the original: `SUBSTITUTE "JSON" BY "JSN"`
then `01 JSN` was told JSON is no user-defined word while `01 JSON` compiled clean. `CobolWordsMap.SynonymBy` now returns the directive
that wrote a synonym and the hint (which reads the parser's map) names GR4 and the SUBSTITUTE for that arm; goldens
`pb4239_cobol_words_substitute_json_is_a_data_name` and `pb4239-cobol-words-substitute-synonym-of-extension-word-is-reserved`, and a
unit test. The lead is fixed too: the SEARCH COBOL0900 gate quoted identifier-1 with `GetText()` (`HOFG(GIHI)`) and now quotes it as
written (`SearchIdentifier1GateTests`).

Gate (run 000002-20261104T194613Z, head f8408029e): === BUILD-LOCAL GATE: GREEN — Conformance 21,503/21,503 · Unit 39,811/39,811 · Characterization 37/37 cases ran (skipped 0) ===. The first gate (000001) was RED on one case, EditionGateArmDriftTests.GatedConstruct_NamesItsEdition_EvenWhenItsOperandsDoNotResolve for the new SEARCH construct, which moved the gate from the binder to the parse arm.

