      *>--- conformance-case
      *> id: 1985-GR-III_2-T7_10
      *> rules: [1985-GR-III.2-T7.10, 1985-GR-III.2-T7.11, 1985-GR-III.2-T7.12, 1985-GR-III.2-T7.13, 1985-GR-III.2-T7.14, 1985-GR-III.2-T7.15, 1985-GR-III.2-T7.6, 1985-GR-III.2-T7.8, 1985-GR-III.2-T7.9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 Table 7 row 10; §1985:III.2 Table 7 row 11;
      *> §1985:III.2 Table 7 row 12; §1985:III.2 Table 7 row 13;
      *> §1985:III.2 Table 7 row 14; §1985:III.2 Table 7 row 15;
      *> §1985:III.2 Table 7 row 6; §1985:III.2 Table 7 row 8;
      *> §1985:III.2 Table 7 row 9 ((section preamble)). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SPECCHAR.
      * 1985 III.2 Table 7 rows 6-15: each special character in use.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-AMT        PIC 9(4)V99 VALUE 1234.5.
       01  W-CUR        PIC $$,$$9.99.
       01  W-DEC        PIC 9.99.
       01  W-TAB.
           05  W-EL     PIC 9 OCCURS 3 TIMES.
       01  W-STR        PIC X(6) VALUE "AB""CDE".
       01  W-N          PIC 99 VALUE 0.
       01  W-OK         PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE W-AMT TO W-CUR; MOVE 1.5 TO W-DEC.
           IF W-CUR = "$1,234.50"  ADD 1 TO W-OK.
           IF W-DEC = "1.50" ADD 1 TO W-OK.
           MOVE 7 TO W-EL (2).
           COMPUTE W-N = (W-EL (2) + 1) * 2.
           IF W-N = 16 ADD 1 TO W-OK.
           IF W-N > 15 ADD 1 TO W-OK.
           IF W-N < 17 ADD 1 TO W-OK.
           IF W-STR (3:1) = """" ADD 1 TO W-OK.
           IF W-STR (4:3) = "CDE" ADD 1 TO W-OK.
           DISPLAY "CUR=" W-CUR " DEC=" W-DEC " N=" W-N.
           IF W-OK = 7 DISPLAY "SPECCHAR PASS"
               ELSE DISPLAY "SPECCHAR FAIL " W-OK.
           STOP RUN.
