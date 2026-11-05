*>--- conformance-case
*> id: pb4705-usage-pointer-same-as-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 through a SAME AS copy of a level-1 GROUP (13.18.49.3 SR7 admits only an elementary item or a
*> level 1 group). 13.18.49.4 GR1 reproduces PG's members under PC and under Q, so PP stands at level 05 in three
*> places; the rule is violated by the ONE written member, and the compiler reports it once, where it is written
*> (kb/Work PB4705: it used to add a COBOL1724 per copy at that same position). The refusal is the fixture; the
*> report count is pinned by UsageDeclarationPlacementMatrixTests.ASameAsCopyOfAViolatingGroup_AddsNoSecondReport.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB4705.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PG.
   05 PX PIC X(4).
   05 PP USAGE POINTER.
01 PC SAME AS PG.
01 G.
   05 Q SAME AS PG.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
