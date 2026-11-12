      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-10
      *> rules: [1985-GR-VI.6.21.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR10 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the confirmation pass (the redraft of a
      *> WEAK or WRONG record, whose filed defect was fixed; re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERF10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N    PIC S9 VALUE 3.
       01  C    PIC 99 VALUE 0.
       01  I    PIC 9  VALUE 0.
       01  J    PIC 9  VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM BUMP-PARA.
           DISPLAY "A " C.
           MOVE 0 TO C.
           PERFORM BUMP-N-PARA N TIMES.
           DISPLAY "B " C.
           MOVE 0 TO C.
           MOVE 0 TO N.
           PERFORM BUMP-PARA N TIMES.
           MOVE -2 TO N.
           PERFORM BUMP-PARA N TIMES.
           DISPLAY "B0 " C.
           PERFORM BUMP-PARA 2 TIMES.
           DISPLAY "B2 " C.
           PERFORM BUMP-PARA UNTIL C > 4.
           DISPLAY "C1 " C.
           PERFORM BUMP-PARA UNTIL C > 4.
           DISPLAY "C2 " C.
           PERFORM BUMP-PARA WITH TEST AFTER UNTIL C > 4.
           DISPLAY "C3 " C.
           PERFORM SHOW-I VARYING I FROM 1 BY 2 UNTIL I > 5.
           DISPLAY "D1 END " I.
           PERFORM SHOW-I VARYING I FROM 4 BY 1 UNTIL I > 3.
           DISPLAY "D2 END " I.
           PERFORM SHOW-I WITH TEST AFTER
               VARYING I FROM 4 BY 1 UNTIL I > 3.
           DISPLAY "D3 END " I.
           PERFORM SKIP-I VARYING I FROM 1 BY 1 UNTIL I > 5.
           DISPLAY "D4 END " I.
           PERFORM SHOW-IJ
               VARYING I FROM 1 BY 1 UNTIL I > 2
               AFTER J FROM I BY 1 UNTIL J > 2.
           DISPLAY "D5 END " I " " J.
           PERFORM SHOW-IJ WITH TEST AFTER
               VARYING I FROM 1 BY 1 UNTIL I = 2
               AFTER J FROM 1 BY 1 UNTIL J = 2.
           DISPLAY "D6 END " I " " J.
           STOP RUN.
       BUMP-PARA.
           ADD 1 TO C.
       BUMP-N-PARA.
           ADD 1 TO C.
           MOVE 5 TO N.
       SHOW-I.
           DISPLAY "I=" I.
       SKIP-I.
           DISPLAY "S=" I.
           IF I = 2 MOVE 4 TO I.
       SHOW-IJ.
           DISPLAY "I=" I " J=" J.
