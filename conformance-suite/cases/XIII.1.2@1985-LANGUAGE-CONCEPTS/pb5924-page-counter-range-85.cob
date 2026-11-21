      *>--- conformance-case
      *> id: pb5924-page-counter-range-85
      *> rules: [1985-GR-XIII.1.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XIII.1.2.2 (cite.py: OK): "must be capable of representing a range of values from 1 through 999999"
      *> PAGE-COUNTER holds 1 after INITIATE, takes 999998 from a MOVE and
      *> reaches 999999 at the next page advance: the top of its range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGCTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pgctr85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W6      PIC 9(6) VALUE 0.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT 10.
       01  DE-1 TYPE DETAIL LINE 2.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           MOVE PAGE-COUNTER TO W6.
           DISPLAY "A " W6.
           MOVE 999998 TO PAGE-COUNTER.
           GENERATE DE-1.
           MOVE PAGE-COUNTER TO W6.
           DISPLAY "B " W6.
           MOVE 999999 TO PAGE-COUNTER.
           MOVE PAGE-COUNTER TO W6.
           DISPLAY "C " W6.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
