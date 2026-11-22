      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_5-T4_1
      *> rules: [1985-GR-VI.6.18.5-T4.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.5 Example 4, its result row (Table 4 row 1): ITEM
      *> "ABABABABC", TALLYING COUNT-0 FOR ALL "AB" AFTER "BA" BEFORE "BC"
      *> gives COUNT-0 1, and REPLACING ALL "AB" BY "XY" AFTER "BA" BEFORE
      *> "BC" gives "ABABXYABC" (COUNT-0 zero before the statement, as
      *> the clause's p1 assumes). The AB at positions 7-8 runs into the
      *> BEFORE "BC" at 8-9; only the printed row fixes that it is not
      *> counted (kb/Work PB6001, refuter 85e-01). The program checks
      *> itself: OK lines only when the printed results come back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EX4T41.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ITEM     PIC X(9).
       01  COUNT-0  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABABABABC" TO ITEM.
           INSPECT ITEM TALLYING
                COUNT-0 FOR ALL "AB" AFTER "BA" BEFORE "BC".
           INSPECT ITEM REPLACING
                ALL "AB" BY "XY" AFTER "BA" BEFORE "BC".
           DISPLAY "COUNT-0 " COUNT-0 " ITEM " ITEM.
           IF COUNT-0 = 1
               DISPLAY "COUNT OK"
           ELSE
               DISPLAY "COUNT WRONG".
           IF ITEM = "ABABXYABC"
               DISPLAY "FINAL OK"
           ELSE
               DISPLAY "FINAL WRONG".
           STOP RUN.
