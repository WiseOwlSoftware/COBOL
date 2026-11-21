      *>--- conformance-case
      *> id: 1989-GR-A_2_4-p5
      *> rules: [1989-GR-A.2.4-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.4 p5 (THE EVOLUTION OF CODASYL COBOL). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G4FNTYPES.
      * 1989 A.2.4 p5: Type column I integer, N numeric, X alnum.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WI   PIC S9(4)V99.
       01 WN   PIC S9(4)V99.
       01 WX   PIC X(4).
       01 OK-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P0.
      * INTEGER is type I: an integer value.
           COMPUTE WI = FUNCTION INTEGER (3.7) + 0.
           IF WI = 3 ADD 1 TO OK-COUNT
               DISPLAY "I-OK"
           ELSE DISPLAY "I-BAD " WI.
      * SQRT is type N: a numeric value with a fraction.
           COMPUTE WN = FUNCTION SQRT (2.25).
           IF WN = 1.5 ADD 1 TO OK-COUNT
               DISPLAY "N-OK"
           ELSE DISPLAY "N-BAD " WN.
      * UPPER-CASE is type X: an alphanumeric value.
           MOVE FUNCTION UPPER-CASE ("abcd") TO WX.
           IF WX = "ABCD" ADD 1 TO OK-COUNT
               DISPLAY "X-OK"
           ELSE DISPLAY "X-BAD " WX.
           IF OK-COUNT = 3 DISPLAY "ALL-OK"
           ELSE DISPLAY "FAIL".
           STOP RUN.
