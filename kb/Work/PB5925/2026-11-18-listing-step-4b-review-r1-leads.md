## Leads: two low leads of the step 4b review (2002 and 2014), open on origin/main

Source: review-LS4b round 1, CLEAN with two low leads, `E:/COBOL-coord/scratch/reports/w20262104e-PB5925-report.md` lines 91-92. Each was re-read on
origin/main 0d4042678a for this file.

**R1-1. DESIGN-listing.md still says step 4b adjudicates all twelve 2002 rows; ten closed and two are held by PB5930.**
Sites: `docs/rearchitecture/DESIGN-listing.md` (line 1482, step 4b: "the records change adjudicates the twelve 2002 rows of section 2.2"; line 1349,
section 10 item 2; line 1405; line 91 "adjudicates all twelve"), `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing2002Tests.cs` (class summary, line 11:
"the witnesses of the twelve 2002 listing rows"). Section 2.2 (line 94) already says "Ten of the twelve" and names kb/Work PB5930 for
`2002-DOC-B.1-90` and `-165` (PB5930 is open: a 2002 Annex B.1 row of kind DOC cannot be adjudicated CONFORMS because its computed register anchor
names a section 7 row the A.1 register audit refuses). Rule 6: a doc describes the CURRENT state. Reword the four places in the design and the test's
class summary to ten closed and two held, citing PB5930. Do it with the change that closes PB5930, or sooner.

**R1-2. Listing2023Tests keeps its own Prologue and Epilogue arrays, byte-identical to the ones ListingHookCases builds for 2023.**
Sites: `tests/WiseOwl.Cobol.Tests.Conformance/Listing/Listing2023Tests.cs` (`Prologue` line 31, `Epilogue` line 39; used at lines 109, 236, 270),
`tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingHookCases.cs#Prologue` (line 52, built for the edition under test, here `PROGRAM-ID. L2023`).
The step set out to make the shared hook text ONE body (`ListingHookCases`, DESIGN-listing line 1351), and the class's remaining 2023-only cases
already compile through `_cases.Compile`, yet three of them still build their source from the class's private arrays. A change to the shared
program text edits one and not the other. Fix: have the three cases take `_cases`'s prologue and epilogue (expose them from `ListingHookCases`) and
delete the private arrays.
