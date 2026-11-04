## Entry 3107 — 2026-11-04 14:27 PST — PB4609: a subscript on SEARCH identifier-1 is a COBOL-2023 permission, and `--std 2014` refuses it

PB4577 had placed the edge for `SEARCH H (GI)` (GI subscripting the table G that encloses H) at COBOL-2014, because the 2023 Annex E
lists no SEARCH change. The 2014 cycle's final committee draft (FCD 1.0, WG4, 2010-07-01, 14.9.34.2 ALL FORMATS rule 1) still says
"Identifier-1 shall not be subscripted or reference modified", as the 2002 text and the 2009 CD 1.2 do. Only the 2023 text of
14.9.37.3 SR2 and SR3 relaxes it (`cite.py --check 14.9.37.3` is OK on both). The owner's decision of 2026-11-04 on PB4547 is
"Revert: 2023-only".

**Re-probe.** On the pre-fix build the PB4577 positive compiled and ran at `--std 2014` (FOUND 8, NOT FOUND, FOUND 9). After the fix
`--std 85`, `2002` and `2014` draw COBOL0900 "a subscript on SEARCH identifier-1 requires COBOL-2023" at each of the three subscripted
identifier-1 operands, and `--std 2023` compiles clean. The bare spelling (`SEARCH H`, the enclosing occurrence in the WHEN phrases) is
legal at every edition and is unchanged.

**Fix.** The construct row is renamed `search-identifier-1-enclosing-subscript-2023` and its `introducedIn` is 2023, with the FCD sentence
in its citation and description. `Constructs.g.cs` and `ConstructRegistry.g.cs` are regenerated and the one caller,
`VersionConformancePass.ParseArm.GateSearchIdentifier1`, asks the new constant, with no alias. VCR row 7.49, `docs/CONFORMANCE.md`
D-SRCH-DYN, the `SearchIdentifier1Operand` catalog text and the code comments follow.

**Goldens.** The PB4577 positive is now `pb4577_search_enclosing_subscript_2023` (accept 2023) and the two negatives are
`pb4577-search-enclosing-subscript-below-2023` and `pb4577-search-all-enclosing-subscript-below-2023`, refusing 85, 2002 and 2014 with
COBOL0900. The sweep over `SEARCH ... (` found four more goldens writing the enclosing subscript, now `accept: [2023]`:
`pb443_search_identifier_1`, `pb877_subscripted_subscript`, `pb1263_occurs_key_is_table_in_outer_table`, and
`pb3365_search_nested_dynamic_table` (`SEARCH EP(I)`, which PB4577 had missed). `SearchIdentifier1GateTests` became a theory over 85,
2002 and 2014. SR-14.9.37.3-2 and -3 had their test-refs re-pointed with `record_verdicts` (2 rows, GAP 1 to 1; no row closed).

**Self-review.** Architecture: the edge stays in the one parse-arm gate and one construct row; the two COBOL2075 advice texts in `StatementValidation` (SR2 count, PB3365 dynamic arm) read it through `IdentifierOneMayWriteEnclosingSubscripts` = `ConstructRegistry.IsAvailable` on that row (review round 1 found the first draft left a hard-coded `Year >= 2014` copy and an unconditional "write the enclosing subscripts" advice; `SearchIdentifier1GateTests.TheCobol2075Advice_OffersIdentifier1SubscriptsOnlyFrom2023` pins both over 85, 2002, 2014, 2023). Full code: the
gate asks the recognised subscript, which is the rule's wording in all three drafts; a subscript at the searched level still draws COBOL2075
at every edition and now also names 2023 below it. Duplication: no second copy of the rule, the old row name appears nowhere (sweep
`grep -rn "enclosing-subscript-2014"` is empty outside the frozen DEVLOG and the landed PB4577 note's history). Performance: no hot-path change.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 21,640/21,640 · Unit 39,855/39,855 · Characterization 37/37 cases ran (skipped 0) ===` (the re-gate after review round 1, run 000003-20261104T221559Z; the first gate was run 000001-20261104T215915Z)
`semgrep/verify.py` PASS with no count raised.

