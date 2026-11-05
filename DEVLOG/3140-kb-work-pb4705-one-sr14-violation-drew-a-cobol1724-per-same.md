## Entry 3140 — 2026-11-04 23:08 PST — kb/Work PB4705: one SR14 violation drew a COBOL1724 per SAME AS copy of its group

`01 PG. 05 PX PIC X. 05 PP USAGE POINTER. 01 PHH SAME AS PG. 01 G. 05 Q SAME AS PG.` is one violation of ISO 13.18.60.3 SR14 (a
pointer at level 05 in a group that is no strong type declaration), and the compiler reported it three times at PP's position: once for
the written member, once for each SAME AS copy of PG. The `05 PZ TYPE PT` spelling did the same. The note had it as "SAME AS twice";
the re-probe showed one report per copy, and the cause is not in `ScreenSr14ElementaryPlacement` alone. `ConformanceForest` prunes TYPE
clones only, so the members `CloneItem` copies for a SAME AS subject (a level-1 group, 13.18.49.3 SR7) look written to the first loop of
`CheckUsageDeclarationsPass`, and the second loop's `written` test lets a TYPE entry under a SAME AS subject through as well.

Both SR14 arms (the group that wrote a phrase, the elementary item) now skip a member whose `ClonedFrom` is set: its verdict is its
source member's, which is in the same forest and reported at its own position. The SAME AS subject is still screened at its own
placement, a source that is legal under a strong type stays clean (`01 SV TYPE ST`, `01 Q SAME AS SV`), and the screens whose verdict
the copy's placement changes (SR4 CONSTANT RECORD, SR15, EXTERNAL, BASED) still ask the copies.

`UsageDeclarationPlacementMatrixTests.ASameAsCopyOfAViolatingGroup_AddsNoSecondReport` asserts exactly one COBOL1724 for every SR14
phrase in both spellings, and fails with the guard off; the strongly-typed copy is pinned legal. Golden
`pb4705-usage-pointer-same-as-group` (refused 2002-2023; the harness cannot count reports, so the unit test does).

