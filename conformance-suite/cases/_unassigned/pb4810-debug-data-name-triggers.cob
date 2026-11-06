      *>--- conformance-case
      *> id: pb4810-debug-data-name-triggers
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING, a data-name and an
      *> ALL REFERENCES OF target (the facility is in no later edition;
      *> docs/CONTROL_FLOW_DESIGN.md D20). General rule 6 c: the section of
      *> identifier-1 alone runs "immediately after the execution of any
      *> other COBOL statement that explicitly references and causes the
      *> content of the data item referenced by identifier-1 to be
      *> changed". General rule 5 d: ALL REFERENCES OF runs it after any
      *> statement that references the item. General rule 7: not more than
      *> once for an operand in a single statement, and each imperative verb
      *> is a statement of its own. General rule 9: a qualifier is no
      *> reference. General rules 13 and 24: DEBUG-NAME is the name as
      *> written, DEBUG-CONTENTS the content of the item.
      *> Why each leg can fail: MOVE A TO C only sends A, so A's section
      *> must not run; ADD A TO A runs it once, not twice; the nested MOVE
      *> inside IF runs it and the IF (which references only C) does not;
      *> a size error skips the store and the section still runs, after the
      *> imperative statement of its phrase; G1 OF G references G1 and not
      *> G, so G's section runs for MOVE G only; B is sent and received and
      *> both references run its section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810DN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A     PIC 9(3) VALUE 0.
       01  B     PIC X(5) VALUE "HELLO".
       01  C     PIC 9(3) VALUE 0.
       01  G.
           02  G1  PIC 99 VALUE 11.
           02  G2  PIC 99 VALUE 22.
       01  W-NAME PIC X(8).
       01  W-CONT PIC X(8).
       01  W-GROUP PIC X(4).
       01  W-B PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-A SECTION.
           USE FOR DEBUGGING ON A.
       DBG-A-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "CHANGED " W-NAME "=" W-CONT.
       DBG-B SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF B.
       DBG-B-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "REFERENCED " W-NAME "=" W-CONT.
       DBG-G1 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF G1 OF G.
       DBG-G1-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "REFERENCED " W-NAME "=" W-CONT.
       DBG-G SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF G.
       DBG-G-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "REFERENCED " W-NAME "=" W-CONT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE 5 TO A.
           MOVE A TO C.
           MOVE B TO W-B.
           MOVE "WORLD" TO B.
           IF C = 5
               MOVE 7 TO A
           END-IF.
           ADD 1 TO A.
           ADD A TO A.
           ADD 999 TO A
               ON SIZE ERROR DISPLAY "SIZE ERROR"
           END-ADD.
           DISPLAY G1 OF G.
           MOVE G TO W-GROUP.
           MOVE 33 TO G2.
           MOVE 44 TO G2 OF G.
           MOVE 55 TO G1 OF G.
           STOP RUN.
