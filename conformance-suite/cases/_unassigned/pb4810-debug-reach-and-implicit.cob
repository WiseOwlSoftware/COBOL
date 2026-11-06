      *>--- conformance-case
      *> id: pb4810-debug-reach-and-implicit
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810, review round 2. X3.23-1985 USE FOR DEBUGGING
      *> general rule 5 d: an ALL REFERENCES OF section runs after "any
      *> other COBOL statement that explicitly references identifier-1";
      *> rule 5 closes: "If identifier-1 is specified in a phrase that is
      *> not executed or evaluated, the associated debugging section is
      *> not executed". Rule 9 (a group reference does not reference its
      *> subordinates) and rule 14 (a subscript is a reference).
      *> A: IF D = 1 GO TO M2 M3 DEPENDING ON K jumps for K = 1: the
      *> section for D runs before the jump; for K = 9 the statement falls
      *> through and runs it after. B: IF D = 1 EXIT PROGRAM in a called
      *> program returns: the section runs before the return. C: INITIALIZE
      *> G references G, not its member X. D: the condition-name YC (I)
      *> written with a subscript references I. E: EVALUATE and SEARCH
      *> WHEN phrases that are never reached do not reference D. F: an
      *> unused DEBUG-SUB-1 holds spaces; moved to an alphanumeric item it
      *> is spaces (implementor latitude, docs/CONFORMANCE.md D-DEBUG-SUB).
      *> Why each leg can fail: a section placed only after the statement
      *> is lost to the jump or the return; a lowered operand or a skipped
      *> subscript position runs a section the program did not earn or
      *> misses one it did; a per-statement plan runs a section for a WHEN
      *> phrase control never evaluated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810RI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 1.
       01 K PIC 9 VALUE 1.
       01 G.
          02 X PIC 9 VALUE 1.
          02 Y PIC X VALUE "A".
       01 TB.
          02 CC PIC 9 OCCURS 3.
             88 YC VALUE 1.
       01 I PIC 9 VALUE 2.
       01 TT.
          02 E PIC 9 OCCURS 3 INDEXED BY XI.
       01 Z PIC X VALUE "A".
       01 X5 PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB0 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       DB0-B.
           DISPLAY "  DB0".
       DBG SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF G.
       DBG-B.
           DISPLAY "  DBG".
       DBX SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF X.
       DBX-B.
           DISPLAY "  DBX".
       DBI SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF I.
       DBI-B.
           DISPLAY "  DBI".
       DBZ SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF Z.
       DBZ-B.
           MOVE DEBUG-SUB-1 TO X5.
           DISPLAY "  DBZ [" X5 "]".
           IF DEBUG-SUB-1 = SPACES
              DISPLAY "  SPACES"
           ELSE
              DISPLAY "  NOT".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "111" TO TB TT.
           DISPLAY "A1: K = 1 jumps".
           IF D = 1 GO TO M2 M3 DEPENDING ON K.
           DISPLAY "NOT HERE".
       M2.
           DISPLAY "A2: K = 9 falls through".
           MOVE 9 TO K.
           IF D = 1 GO TO M2 M3 DEPENDING ON K.
           DISPLAY "FELL".
       M3.
           DISPLAY "B: EXIT PROGRAM".
           CALL "PB4810S".
           DISPLAY "BACK".
           DISPLAY "C: INITIALIZE G".
           INITIALIZE G.
           DISPLAY "D: IF YC (I)".
           IF YC (I) DISPLAY "T".
           DISPLAY "E1: WHEN D not reached".
           MOVE 1 TO K.
           EVALUATE K
             WHEN 1 DISPLAY "ONE"
             WHEN D DISPLAY "D"
           END-EVALUATE.
           DISPLAY "E2: WHEN D reached".
           EVALUATE K
             WHEN 2 DISPLAY "TWO"
             WHEN D DISPLAY "D"
           END-EVALUATE.
           DISPLAY "E3: SEARCH second WHEN not reached".
           SET XI TO 1.
           SEARCH E AT END DISPLAY "NONE"
              WHEN E (XI) = 1 DISPLAY "HIT"
              WHEN E (XI) = D DISPLAY "HIT2".
           DISPLAY "E4: SEARCH second WHEN reached".
           SET XI TO 1.
           SEARCH E AT END DISPLAY "NONE"
              WHEN E (XI) = 7 DISPLAY "HIT"
              WHEN E (XI) = D DISPLAY "HIT2".
           DISPLAY "F: unused DEBUG-SUB-1".
           MOVE "Q" TO Z.
           DISPLAY "END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SB0 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       SB0-B.
           DISPLAY "  SB0".
       END DECLARATIVES.
       MAIN SECTION.
       S1.
           DISPLAY "IN SUB".
           IF D = 1 EXIT PROGRAM.
           DISPLAY "AFTER".
           EXIT PROGRAM.
       END PROGRAM PB4810S.
       END PROGRAM PB4810RI.
