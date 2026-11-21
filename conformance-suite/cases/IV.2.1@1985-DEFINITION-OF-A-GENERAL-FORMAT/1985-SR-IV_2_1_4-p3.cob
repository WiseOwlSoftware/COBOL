      *>--- conformance-case
      *> id: 1985-SR-IV_2_1_4-p3
      *> rules: [1985-SR-IV.2.1.4-p3, 1985-SR-IV.2.1.4-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.2.1.4 p3; §1985:IV.2.1.4 p4 (DEFINITION OF A GENERAL
      *> FORMAT). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R04OUTER.
      * 1985 IV.2.1.4 p3, p4: PROGRAM-ID (V-2) encloses COMMON and
      * INITIAL in choice indicators: one or both, in any order.
       PROCEDURE DIVISION.
       M.
           CALL "R04BOTH".
           CALL "R04BOTH".
           CALL "R04REV".
           CALL "R04ONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R04BOTH IS COMMON INITIAL PROGRAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       B.
           ADD 1 TO N.
           DISPLAY "BOTH N=" N.
           EXIT PROGRAM.
       END PROGRAM R04BOTH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R04REV IS INITIAL COMMON PROGRAM.
       PROCEDURE DIVISION.
       R.
           DISPLAY "REV".
           EXIT PROGRAM.
       END PROGRAM R04REV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R04ONE IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       O.
           DISPLAY "ONE".
           EXIT PROGRAM.
       END PROGRAM R04ONE.
       END PROGRAM R04OUTER.
