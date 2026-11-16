      *>--- conformance-case
      *> id: g85_bf37_02
      *> rules: [1985-GR-XIII.4.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.3.4 GR7 - GENERATE executed only after INITIATE
      * and before TERMINATE, over two INITIATE/TERMINATE cycles of
      * one report. Each GENERATE presents its detail line and moves
      * LINE-COUNTER (no PAGE clause: line = LINE-COUNTER + 1,
      * §1985:XIII.3.10.8 GR4d), and the INITIATE between cycles
      * resets it to zero (§1985:XIII.4.4.4 GR1):
      *   GEN 01, GEN 02  first cycle; GEN 01  second cycle.
      * The read-back counts the tag bytes the three GENERATEs put in
      * the file (spaces and line or page controls are skipped):
      *   TAGS 03 AAB     two lines of cycle 1, one of cycle 2.
      * A GENERATE that presented nothing, or a second cycle whose
      * INITIATE did not reset LINE-COUNTER (GEN 03), fails a line.
      * Derivation: §1985:XIII.3.10.8 GR4d and GR6d, §1985:XIII.4.4.4
      * GR1b.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3702.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G85BF3702.RPT".
           SELECT CHK ASSIGN TO "G85BF3702.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT
           REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 99.
       01  WS-TAG  PIC X VALUE "A".
       01  WS-EOF  PIC X VALUE "N".
       01  WS-N    PIC 99 VALUE 0.
       01  WS-TAGS PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X SOURCE WS-TAG.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT.
           INITIATE R.
           GENERATE DL.
           PERFORM SHOW-LC.
           GENERATE DL.
           PERFORM SHOW-LC.
           TERMINATE R.
           MOVE "B" TO WS-TAG.
           INITIATE R.
           GENERATE DL.
           PERFORM SHOW-LC.
           TERMINATE R.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM READ-ONE UNTIL WS-EOF = "Y".
           CLOSE CHK.
           DISPLAY "TAGS " WS-N " " WS-TAGS (1:3).
           STOP RUN.
       SHOW-LC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "GEN " WS-LC.
       READ-ONE.
           READ CHK
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               IF CHK-REC = "A" OR CHK-REC = "B"
                   ADD 1 TO WS-N
                   MOVE CHK-REC TO WS-TAGS (WS-N:1).
