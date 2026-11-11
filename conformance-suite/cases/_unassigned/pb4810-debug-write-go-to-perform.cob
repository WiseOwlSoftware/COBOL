      *>--- conformance-case
      *> id: pb4810-debug-write-go-to-perform
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING general rules 5 a, 5 b,
      *> 5 c, 5 d and 6 b (docs/CONTROL_FLOW_DESIGN.md D20), where the section
      *> does not run after the statement:
      *> 5 a. WRITE: "immediately before the execution of that WRITE ... and
      *>      after the execution of any implicit move resulting from the
      *>      presence of the FROM phrase". The section sees the moved record
      *>      and changes it; the file read back holds the change.
      *> 5 b. GO TO DEPENDING ON: "immediately before control is transferred".
      *> 5 c / 6 b. PERFORM: "immediately after each initialization,
      *>      modification, or evaluation of the content of the data item
      *>      referenced by identifier-1" by a VARYING, AFTER or UNTIL phrase.
      *>      I (ALL REFERENCES OF) runs after the initialization, each test
      *>      and each augment; J (alone) after its initialization and its
      *>      augments, never after a test, which does not change it.
      *> 5 d. The TIMES count is no loop phase: N runs after the statement.
      *> Rule 7: each imperative verb is a statement of its own, so the ADD
      *> in the PERFORM UNTIL body runs U's section itself, and the loop's
      *> own tests run it besides.
      *> Why each leg can fail: a section run after the WRITE would show the
      *> file unchanged; one run after the GO TO never runs; a loop that runs
      *> it once per statement prints one line, not one per phase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810WG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4810wg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  SRC   PIC X(4) VALUE "ABCD".
       01  SEL   PIC 9 VALUE 2.
       01  I     PIC 9 VALUE 0.
       01  J     PIC 9 VALUE 0.
       01  CNT   PIC 9 VALUE 0.
       01  N     PIC 9 VALUE 2.
       01  U     PIC 9 VALUE 0.
       01  W-NAME PIC X(8).
       01  W-CONT PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-REC SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF F-REC.
       DBG-REC-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "RECORD " W-NAME "=" W-CONT.
           MOVE "ZZZZ" TO F-REC.
       DBG-SEL SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF SEL.
       DBG-SEL-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "SELECTOR " W-NAME "=" W-CONT.
       DBG-I SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF I.
       DBG-I-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "ALL " W-NAME "=" W-CONT.
       DBG-J SECTION.
           USE FOR DEBUGGING ON J.
       DBG-J-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "CHANGED " W-NAME "=" W-CONT.
       DBG-N SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF N.
       DBG-N-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "COUNT " W-NAME "=" W-CONT.
       DBG-U SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF U.
       DBG-U-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "UNTIL " W-NAME "=" W-CONT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F.
           WRITE F-REC FROM SRC.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END CONTINUE END-READ
           .
           DISPLAY "FILE " F-REC.
           CLOSE F.
           GO TO T1 T2 DEPENDING ON SEL.
       T1.
           DISPLAY "AT T1".
       T2.
           DISPLAY "AT T2".
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "BODY"
           END-PERFORM.
           PERFORM VARYING J FROM 1 BY 2 UNTIL CNT > 1
               ADD 1 TO CNT
               DISPLAY "BODY"
           END-PERFORM.
           PERFORM N TIMES
               DISPLAY "TIMES"
           END-PERFORM.
           PERFORM UNTIL U > 1
               ADD 1 TO U
           END-PERFORM.
           STOP RUN.
