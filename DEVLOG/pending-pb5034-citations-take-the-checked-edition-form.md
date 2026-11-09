## PB5034 citations that never passed cite.py --check take the checked edition form

Two reviews (PB5024 R1-1 and PB5025 R1-1) found citations in comments that nobody had run through `cite.py --check`. I re-ran
each probe on this tree. `cite.py --check "§2023:E.3.2 2" ...` refuses the citation because "2" is not a rule designator.
`cite.py --check "§2023:12.1" "comment-entry"` fails because the 2023 text has no comment-entry anywhere (`--find` answers NO
CLAUSE).

The fix changes comments and one citation string. `MarginRCutTokenTests.ACutInACommentEntry_IsNotNamed` now quotes the 1985
definition, `§1985:III.2`: a comment-entry "may be any combination of characters from the computer's character set". The two
`(§2023:E.3.2 2)` sites (`VersionConformancePass` `ParseArm.VisitApplyCommitClause` and the `CobolIO.g4` `ioControlClause`
comment) now read `(§2023:E.3.2 item 2)`, the form the tree already uses for an annex list item. The sibling sweep covered every
`E.3.2` citation under `src/`, the four construct rows that cite it (`constructs.json`, which regenerates
`ConstructRegistry.g.cs`) and two test summaries. Each bare `Annex E.3.2 item N` became `§2023:E.3.2 item N`, and items 1, 2,
5 and 6 were each checked against their own words. A construct row's citation reaches COBOL0900's text, so at `--std 2014`
APPLY COMMIT now names `(ISO §2023:12.4.6.3 (APPLY COMMIT clause; §2023:E.3.2 item 2))`. No expected output quotes that text.
The same test file had two more unchecked citations. Its free-form comment paraphrased `§2023:6.4` and now quotes
`§2023:6.4.1`, and its `Annex A.1 item 158` gained its edition. Every `§` citation that the PB5024 and PB5025 landings added
passed `--check`.

`audit_code_citations.py` already reads these comments and reports 0 findings. It cannot refuse the `§2023:<clause> N)` shape: `cite.py`
prints its own rule paths in that shape and the tree holds about 7,600 such lines, so the shape is a tooling lead in the wave
report. No inventory row closes (`closes_rows: []`, with a reason). The branch continues PB5026 (wave 20261946 group A) and lands
with it.
