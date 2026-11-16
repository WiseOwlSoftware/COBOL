      *>--- conformance-case
      *> id: g2-rgd-next-group-level-1
      *> rules: [2002-SR-13.13.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.13.2 SR6 - "The NEXT GROUP clause may be specified
      *>  only in a level 1 entry." The granted form: NEXT GROUP written
      *>  in level 1 entries, once in a level 01 group entry whose LINE
      *>  is on a subordinate level 05 entry (D1), once in a level 01
      *>  that carries its own LINE (D2); both are accepted and act.
      *> "The NEXT GROUP clause modifies the value of the current
      *>  report's LINE-COUNTER after the printing of the last line"
      *>  (§2002:13.16.35.3 GR2), so LINE-COUNTER after each GENERATE
      *>  is the observable.
      *> DERIVATION:
      *>  GENERATE D1: first body group since INITIATE, no page fit
      *>   test (§2002:13.16.33.3 GR4); absolute LINE 3 -> line 3
      *>   (§2002:13.16.33.3 GR5 a). NEXT GROUP PLUS 2 is relative and
      *>   3 + 2 is less than FOOTING 18, so "integer-2 is added to
      *>   LINE-COUNTER" (§2002:13.16.35.3 GR4 b) -> "LC 05" (03 without
      *>   the clause).
      *>  GENERATE D2: relative LINE PLUS 1, trial sum 5 + 1 = 6 is not
      *>   above LAST DETAIL 15, page fit successful (§2002:13.16.33.3
      *>   GR4); not the first body group on the page, so line
      *>   5 + 1 = 6 (GR5 b 3.). NEXT GROUP 9 is absolute and 6 is less
      *>   than 9, so "LINE-COUNTER is set equal to integer-1"
      *>   (§2002:13.16.35.3 GR4 a) -> "LC 09".
      *>  GENERATE D3 (no NEXT GROUP): trial sum 9 + 1 = 10, line 10
      *>   -> "LC 10" (07 without D2's NEXT GROUP).
      *> Fails if NEXT GROUP is refused in a level 1 entry, or if it
      *> does not move LINE-COUNTER as GR4 a and b require.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RGD02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2rgd02.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 99.
       REPORT SECTION.
       RD  R PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  D1 TYPE DETAIL NEXT GROUP PLUS 2.
           05  LINE 3.
               10  COLUMN 1 PIC XX VALUE "D1".
       01  D2 TYPE DETAIL LINE PLUS 1 NEXT GROUP 9.
           05  COLUMN 1 PIC XX VALUE "D2".
       01  D3 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC XX VALUE "D3".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D2.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D3.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           TERMINATE R.
           CLOSE PRT.
           STOP RUN.
