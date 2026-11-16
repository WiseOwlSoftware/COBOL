      *>--- conformance-case
      *> id: g85_bf37_04
      *> rules: [1985-GR-XIII.4.5.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.5.4 GR3 - the report file is opened before each
      * INITIATE of its report: first OPEN OUTPUT, then (after CLOSE,
      * GR4) OPEN EXTEND, the two modes §1985:XIII.4.4.4 GR2 names.
      * In each run the INITIATE, GENERATE and TERMINATE must work on
      * the open file:
      *   OUTPUT LC=1     detail at LINE-COUNTER 0 + 1 (no PAGE clause,
      *                   §1985:XIII.3.10.8 GR4d)
      *   EXTEND LC=1     the second INITIATE reset LINE-COUNTER
      * The read-back counts the tag bytes in the file (spaces and line
      * or page controls skipped): OUTPUT wrote "A", EXTEND appended
      * "B" after it (GR10), so
      *   TAGS 02 AB
      * A report run that wrote nothing into the file it was opened
      * on, or an EXTEND that lost the OUTPUT run's line, fails it.
      * Derivation: §1985:XIII.4.4.4 GR1b, §1985:XIII.3.10.8 GR4d and
      * GR6d, §1985:XIII.4.5.4 GR4 and GR10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3704.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G85BF3704.RPT".
           SELECT CHK ASSIGN TO "G85BF3704.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT
           REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 9.
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
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "OUTPUT LC=" WS-LC.
           TERMINATE R.
           CLOSE RPT.
           MOVE "B" TO WS-TAG.
           OPEN EXTEND RPT.
           INITIATE R.
           GENERATE DL.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "EXTEND LC=" WS-LC.
           TERMINATE R.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM READ-ONE UNTIL WS-EOF = "Y".
           CLOSE CHK.
           DISPLAY "TAGS " WS-N " " WS-TAGS (1:2).
           STOP RUN.
       READ-ONE.
           READ CHK
               AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               IF CHK-REC = "A" OR CHK-REC = "B"
                   ADD 1 TO WS-N
                   MOVE CHK-REC TO WS-TAGS (WS-N:1).
