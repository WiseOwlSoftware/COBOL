      *>--- conformance-case
      *> id: g85_bf28_01
      *> rules: [1985-SR-VII.4.6.3-5, 1985-SR-VII.4.6.3-6, 1985-GR-VII.4.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.3 SR5 - mixed organization in one USE
      *> §1985:VII.4.6.3 SR6 - each mode phrase once in DECLARATIVES
      *> §1985:VII.4.6.4 GR4 - an explicit file-name excludes other USEs
      * SR5: the explicit USE names SQ-A (sequential) and RL-B
      *   (relative, random); the INPUT USE serves SQ-C (sequential) and
      *   IX-D (indexed, random). Fails if the pin refuses the mix, or a
      *   handler does not fire for one of its files (USE-FILES at steps
      *   1-2, USE-INPUT at 3-4).
      * SR6: INPUT, OUTPUT, I-O and EXTEND are each specified once; the
      *   program must be accepted and each mode USE fires (steps 3-7
      *   and the COUNTS line).
      * GR4: SQ-A and RL-B are opened INPUT while an INPUT USE exists;
      *   only USE-FILES may run for them (step 1 SQ-A READ at end 10,
      *   step 2 RL-B random READ of a missing record 23, VIII.1.3.4). A
      *   second handler line, or COUNTS INPUT other than 2, fails it.
      * STEPS 3-7: each mode-scoped USE fires once for a file it serves
      *   and that no explicit USE names (GR5 b-e). Statuses: sequential
      *   READ at end 10 (VII.1.3.5); random READ of a missing key 23;
      *   indexed sequential WRITE out of key order in output or extend
      *   mode 21 (IX.4.9.4 GR17a, IX.1.3.4). Each is 1x/2x, NOT
      *   critical (VII.1.3.5: critical values are 'any that begin with
      *   the digit 3 or 4'), so by GR6 control returns to the statement
      *   after the failing one and its DISPLAY line follows the
      *   handler's line.
      * §1985:VII.4.6.4 GR6: a non-critical status returns control to
      *   the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2801.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-A ASSIGN TO "bf2801a.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT RL-B ASSIGN TO "bf2801b.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK-B
               FILE STATUS IS ST-B.
           SELECT SQ-C ASSIGN TO "bf2801c.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-C.
           SELECT IX-D ASSIGN TO "bf2801d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS KY-D
               FILE STATUS IS ST-D.
           SELECT SQ-E ASSIGN TO "bf2801e.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-E.
           SELECT IX-F ASSIGN TO "bf2801f.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-F
               FILE STATUS IS ST-F.
           SELECT IX-G ASSIGN TO "bf2801g.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-G
               FILE STATUS IS ST-G.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-A.
       01  RC-A PIC X(8).
       FD  RL-B.
       01  RC-B PIC X(8).
       FD  SQ-C.
       01  RC-C PIC X(8).
       FD  IX-D.
       01  RC-D.
           05 KY-D PIC X(4).
           05 DT-D PIC X(4).
       FD  SQ-E.
       01  RC-E PIC X(8).
       FD  IX-F.
       01  RC-F.
           05 KY-F PIC X(4).
           05 DT-F PIC X(4).
       FD  IX-G.
       01  RC-G.
           05 KY-G PIC X(4).
           05 DT-G PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  ST-B PIC XX.
       01  ST-C PIC XX.
       01  ST-D PIC XX.
       01  ST-E PIC XX.
       01  ST-F PIC XX.
       01  ST-G PIC XX.
       01  RK-B PIC 9(4).
       01  WS-STEP PIC 9 VALUE 0.
       01  N-FILES PIC 9 VALUE 0.
       01  N-IN PIC 9 VALUE 0.
       01  N-IO PIC 9 VALUE 0.
       01  N-OUT PIC 9 VALUE 0.
       01  N-EXT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-FILES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SQ-A RL-B.
       USE-FILES-P.
           ADD 1 TO N-FILES.
           DISPLAY "USE-FILES STEP " WS-STEP.
       USE-IN SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
       USE-IN-P.
           ADD 1 TO N-IN.
           DISPLAY "USE-INPUT STEP " WS-STEP.
       USE-IO SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON I-O.
       USE-IO-P.
           ADD 1 TO N-IO.
           DISPLAY "USE-I-O STEP " WS-STEP.
       USE-OUT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       USE-OUT-P.
           ADD 1 TO N-OUT.
           DISPLAY "USE-OUTPUT STEP " WS-STEP.
       USE-EXT SECTION.
           USE AFTER EXCEPTION PROCEDURE ON EXTEND.
       USE-EXT-P.
           ADD 1 TO N-EXT.
           DISPLAY "USE-EXTEND STEP " WS-STEP.
       END DECLARATIVES.
       MAIN-S SECTION.
       SETUP-P.
           OPEN OUTPUT SQ-A.
           MOVE "A-REC-01" TO RC-A.
           WRITE RC-A.
           CLOSE SQ-A.
           OPEN OUTPUT RL-B.
           MOVE 1 TO RK-B.
           MOVE "B-REC-01" TO RC-B.
           WRITE RC-B.
           CLOSE RL-B.
           OPEN OUTPUT SQ-C.
           MOVE "C-REC-01" TO RC-C.
           WRITE RC-C.
           CLOSE SQ-C.
           OPEN OUTPUT IX-D.
           MOVE "0001D-01" TO RC-D.
           WRITE RC-D.
           CLOSE IX-D.
           OPEN OUTPUT SQ-E.
           MOVE "E-REC-01" TO RC-E.
           WRITE RC-E.
           CLOSE SQ-E.
           OPEN OUTPUT IX-G.
           MOVE "0005G-01" TO RC-G.
           WRITE RC-G.
           CLOSE IX-G.
           DISPLAY "SETUP " ST-A ST-B ST-C ST-D ST-E ST-G.
       STEP-1.
           MOVE 1 TO WS-STEP.
           OPEN INPUT SQ-A.
           READ SQ-A.
           READ SQ-A.
           DISPLAY "STEP 1 SQ-A STATUS " ST-A.
           CLOSE SQ-A.
       STEP-2.
           MOVE 2 TO WS-STEP.
           OPEN INPUT RL-B.
           MOVE 7 TO RK-B.
           READ RL-B.
           DISPLAY "STEP 2 RL-B STATUS " ST-B.
           CLOSE RL-B.
       STEP-3.
           MOVE 3 TO WS-STEP.
           OPEN INPUT SQ-C.
           READ SQ-C.
           READ SQ-C.
           DISPLAY "STEP 3 SQ-C STATUS " ST-C.
           CLOSE SQ-C.
       STEP-4.
           MOVE 4 TO WS-STEP.
           OPEN INPUT IX-D.
           MOVE "0009" TO KY-D.
           READ IX-D.
           DISPLAY "STEP 4 IX-D STATUS " ST-D.
           CLOSE IX-D.
       STEP-5.
           MOVE 5 TO WS-STEP.
           OPEN I-O SQ-E.
           READ SQ-E.
           READ SQ-E.
           DISPLAY "STEP 5 SQ-E STATUS " ST-E.
           CLOSE SQ-E.
       STEP-6.
           MOVE 6 TO WS-STEP.
           OPEN OUTPUT IX-F.
           MOVE "0002F-01" TO RC-F.
           WRITE RC-F.
           MOVE "0001F-02" TO RC-F.
           WRITE RC-F.
           DISPLAY "STEP 6 IX-F STATUS " ST-F.
           CLOSE IX-F.
       STEP-7.
           MOVE 7 TO WS-STEP.
           OPEN EXTEND IX-G.
           MOVE "0007G-02" TO RC-G.
           WRITE RC-G.
           MOVE "0006G-03" TO RC-G.
           WRITE RC-G.
           DISPLAY "STEP 7 IX-G STATUS " ST-G.
           CLOSE IX-G.
       FINISH.
           DISPLAY "COUNTS FILES " N-FILES " INPUT " N-IN " I-O " N-IO
               " OUTPUT " N-OUT " EXTEND " N-EXT.
           STOP RUN.
