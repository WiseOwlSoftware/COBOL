*>--- conformance-case
*> id: pb6096-move-corr-two-receivers
*> rules: [FMT-14.9.25.2, 1985-FMT-VI.6.19.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB6096. §2023:14.9.25.2 Format 2 and §1985:VI.6.19.2 print exactly ONE receiving identifier after TO (no ellipsis follows it),
*> so a second receiver is refused at every edition. WHAT THIS PINS: the refusal says MOVE CORRESPONDING names exactly
*> one receiving item (COBOL2269), not 'A period may be missing' (COBOL0307) about the previous sentence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6096MV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05 F1 PIC 9(3).
       01  G2.
           05 F1 PIC 9(3).
       01  G3.
           05 F1 PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE CORR G1 TO G2 G3.
           STOP RUN.
