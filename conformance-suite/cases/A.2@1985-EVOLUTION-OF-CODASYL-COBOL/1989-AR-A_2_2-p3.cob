      *>--- conformance-case
      *> id: 1989-AR-A_2_2-p3
      *> rules: [1989-AR-A.2.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 p3 (THE EVOLUTION OF CODASYL COBOL). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-FLAG PIC 9 VALUE 0.
       01  W-R    PIC S9(4)V9(4) VALUE 0.
       PROCEDURE DIVISION.
       P0.
           IF W-FLAG = 1
               COMPUTE W-R = FUNCTION LOG (0)
               COMPUTE W-R = FUNCTION SQRT (-4)
               COMPUTE W-R = FUNCTION MOD (7, 0)
           END-IF.
           DISPLAY "R01 OK".
           STOP RUN.
