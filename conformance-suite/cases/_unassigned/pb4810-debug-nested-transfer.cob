      *>--- conformance-case
      *> id: pb4810-debug-nested-transfer
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING general rule 5 d:
      *> an ALL REFERENCES OF section runs "immediately after the execution
      *> of ... any other COBOL statement that explicitly references
      *> identifier-1", and rule 6 c: the bare data-name section runs after a
      *> statement that "explicitly references and causes the content of the
      *> data item ... to be changed". Rule 7: each imperative verb of a
      *> statement's imperative phrase is a statement of its own.
      *> A statement whose imperative phrase transfers control (IF D = 1
      *> GO TO P, ADD 1 TO E NOT ON SIZE ERROR GO TO P) has finished its own
      *> action when the nested transfer runs, and the code after a transfer
      *> is never reached, so the section runs before the transfer. A
      *> statement whose nested statement is not a transfer runs the section
      *> after the whole statement. Each path runs each section once.
      *> Why each leg can fail: a section placed only after the statement
      *> never runs when the nested GO TO leaves, and an IF whose two arms
      *> both transfer would never run it at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810NT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D        PIC 9(3) VALUE 1.
       01  E        PIC 9(3) VALUE 1.
       01  W-NAME   PIC X(10).
       01  W-CONT   PIC X(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB0 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       DB0-B.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "DB0 [" W-NAME "][" W-CONT "]".
       DB1 SECTION.
           USE FOR DEBUGGING ON E.
       DB1-B.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "DB1 [" W-NAME "][" W-CONT "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "A: IF D = 1 GO TO".
           IF D = 1 GO TO M2.
           DISPLAY "NOT HERE A".
       M2.
           DISPLAY "B: ADD NOT SIZE ERROR GO TO".
           ADD 1 TO E NOT ON SIZE ERROR GO TO M3.
           DISPLAY "NOT HERE B".
       M3.
           DISPLAY "C: ADD NOT SIZE ERROR DISPLAY".
           ADD 1 TO E NOT ON SIZE ERROR DISPLAY "NESTED" E.
           DISPLAY "D: IF, both arms GO TO".
           IF D = 1 GO TO M4 ELSE GO TO M4.
           DISPLAY "NOT HERE D".
       M4.
           DISPLAY "E: IF, arm not taken".
           IF D = 2 GO TO M5.
           DISPLAY "F: IF, NEXT SENTENCE".
           IF D = 1 NEXT SENTENCE ELSE DISPLAY "ELSE F".
           DISPLAY "AFTER F".
       M5.
           DISPLAY "END".
           STOP RUN.
