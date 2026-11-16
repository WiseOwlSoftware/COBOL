      *>--- conformance-case
      *> id: g85_bf37_01
      *> rules: [1985-SR-XIII.3.8.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.8.3 SR1 - the HEADING, FIRST DETAIL, LAST DETAIL
      * and FOOTING phrases written in reverse order (FOOTING 9, LAST
      * DETAIL 7, FIRST DETAIL 5, HEADING 3) must still bind integer-2
      * to 5 to their own phrases; each value is pinned by one line:
      *   BEFORE 03       HEADING 3 (RH LINE PLUS 1 at 1+(3-1), its
      *                   final LINE-COUNTER, seen by USE BEFORE
      *                   REPORTING on the first detail)
      *   PAGE 01 LINE 05 FIRST DETAIL 5 (LINE-COUNTER 3 < 5)
      *   PAGE 01 LINE 06, PAGE 01 LINE 07  trial sums 6, 7 <= LAST
      *                   DETAIL 7
      *   PAGE 02 LINE 05 trial sum 8 > LAST DETAIL 7: page advance
      *   PAGE 02 LINE 09 NEXT GROUP NEXT PAGE puts FOOTING 9 in
      *                   LINE-COUNTER
      * A phrase bound to the wrong integer, or one phrase dropped to
      * its default, moves at least one of these numbers.
      * Derivation: §1985:XIII.4.3.4 GR5 (the RH is processed
      * before the first detail), §1985:XIII.3.10.6 GR3b and GR5d
      * (RH line 1+(3-1)=3, LINE-COUNTER 3), §1985:XIII.4.9.4 GR4
      * (USE runs just before D1 is produced), §1985:XIII.3.10.8
      * GR3b, GR4b, GR6d and GR6c, §1985:XIII.3.5.6 GR4 and
      * §1985:XIII.3.5.5 GR5 (page advance: LINE-COUNTER 0,
      * PAGE-COUNTER 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3701.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G85BF3701.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT
           REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 99.
       01  WS-PC PIC 99.
       REPORT SECTION.
       RD  R
           PAGE LIMIT IS 12 LINES
           FOOTING 9
           LAST DETAIL 7
           FIRST DETAIL 5
           HEADING 3.
       01  TYPE IS REPORT HEADING LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "TITLE".
       01  D1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "FIRST".
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "BODY".
       01  DN TYPE IS DETAIL LINE NUMBER IS PLUS 1
               NEXT GROUP IS NEXT PAGE.
           05  COLUMN 1 PIC X(4) VALUE "LAST".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1-USE SECTION.
           USE BEFORE REPORTING D1.
       D1-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "BEFORE " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT RPT.
           INITIATE R.
           GENERATE D1.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DL.
           PERFORM SHOW-POSITION.
           GENERATE DN.
           PERFORM SHOW-POSITION.
           TERMINATE R.
           CLOSE RPT.
           STOP RUN.
       SHOW-POSITION.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PAGE " WS-PC " LINE " WS-LC.
