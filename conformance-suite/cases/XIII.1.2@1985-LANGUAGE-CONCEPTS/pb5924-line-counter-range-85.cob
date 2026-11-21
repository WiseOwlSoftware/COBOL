      *>--- conformance-case
      *> id: pb5924-line-counter-range-85
      *> rules: [1985-GR-XIII.1.2.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XIII.1.2.3 (cite.py: OK): "representing a range of values from 0 through 999999"
      *> LINE-COUNTER is 0 after INITIATE and, with no PAGE clause, 1001
      *> details of LINE PLUS 999 carry it to 999999 without loss.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LNCTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "lnctr85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W6      PIC 9(6) VALUE 0.
       01  N       PIC 9(4) VALUE 0.
       REPORT SECTION.
       RD  R-1.
       01  DE-1 TYPE DETAIL LINE PLUS 999.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE LINE-COUNTER TO W6.
           DISPLAY "A " W6.
           GENERATE DE-1.
           MOVE LINE-COUNTER TO W6.
           DISPLAY "B " W6.
           PERFORM GEN-P 1000 TIMES.
           MOVE LINE-COUNTER TO W6.
           DISPLAY "C " W6.
           TERMINATE R-1.
           CLOSE PRT.
           STOP RUN.
       GEN-P.
           GENERATE DE-1.
