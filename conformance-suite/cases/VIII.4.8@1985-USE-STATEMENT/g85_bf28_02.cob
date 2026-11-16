      *>--- conformance-case
      *> id: g85_bf28_02
      *> rules: [1985-SR-VIII.4.8.3-5, 1985-SR-VIII.4.8.3-6, 1985-GR-VIII.4.8.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.3 SR5 - mixed organization in one USE
      *> §1985:VIII.4.8.3 SR6 - each mode phrase once in DECLARATIVES
      *> §1985:VIII.4.8.4 GR4 - an explicit file-name excludes other USEs
      * SR5: the explicit USE names RL-A (relative, sequential access)
      *   and IX-B (indexed, random); the INPUT USE serves SQ-C
      *   (sequential) and RL-D (relative, random). Fails if the mix is
      *   refused or a handler misses one of its files.
      * SR6: INPUT, OUTPUT, I-O and EXTEND are each specified once;
      *   accepted, and each mode USE fires (steps 3-7, COUNTS line).
      *   Step 6 is a relative random WRITE of a duplicate key: status
      *   22 (VIII.1.3.4).
      * GR4: RL-A is opened INPUT and IX-B opened I-O while INPUT and
      *   I-O USEs exist; only USE-FILES may run for them (step 1 RL-A
      *   READ at end 10, step 2 IX-B random READ of a missing key 23).
      * STEPS 3-7: each mode-scoped USE fires once for a file it serves
      *   and that no explicit USE names (GR5 b-e). Statuses: sequential
      *   READ at end 10 (VII.1.3.5); random READ of a missing key 23;
      *   indexed sequential WRITE out of key order in output or extend
      *   mode 21 (IX.4.9.4 GR17a, IX.1.3.4). Each is 1x/2x, NOT
      *   critical (VII.1.3.5: critical values are 'any that begin with
      *   the digit 3 or 4'), so by GR6 control returns to the statement
      *   after the failing one and its DISPLAY line follows the
      *   handler's line.
      * §1985:VIII.4.8.4 GR6: a non-critical status returns control to
      *   the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2802.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL-A ASSIGN TO "bf2802a.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT IX-B ASSIGN TO "bf2802b.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS KY-B
               FILE STATUS IS ST-B.
           SELECT SQ-C ASSIGN TO "bf2802c.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-C.
           SELECT RL-D ASSIGN TO "bf2802d.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK-D
               FILE STATUS IS ST-D.
           SELECT RL-E ASSIGN TO "bf2802e.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-E.
           SELECT RL-F ASSIGN TO "bf2802f.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK-F
               FILE STATUS IS ST-F.
           SELECT IX-G ASSIGN TO "bf2802g.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-G
               FILE STATUS IS ST-G.
       DATA DIVISION.
       FILE SECTION.
       FD  RL-A.
       01  RC-A PIC X(8).
       FD  IX-B.
       01  RC-B.
           05 KY-B PIC X(4).
           05 DT-B PIC X(4).
       FD  SQ-C.
       01  RC-C PIC X(8).
       FD  RL-D.
       01  RC-D PIC X(8).
       FD  RL-E.
       01  RC-E PIC X(8).
       FD  RL-F.
       01  RC-F PIC X(8).
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
       01  RK-D PIC 9(4).
       01  RK-F PIC 9(4).
       01  WS-STEP PIC 9 VALUE 0.
       01  N-FILES PIC 9 VALUE 0.
       01  N-IN PIC 9 VALUE 0.
       01  N-IO PIC 9 VALUE 0.
       01  N-OUT PIC 9 VALUE 0.
       01  N-EXT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-FILES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RL-A IX-B.
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
           OPEN OUTPUT RL-A.
           MOVE "A-REC-01" TO RC-A.
           WRITE RC-A.
           CLOSE RL-A.
           OPEN OUTPUT IX-B.
           MOVE "0001B-01" TO RC-B.
           WRITE RC-B.
           CLOSE IX-B.
           OPEN OUTPUT SQ-C.
           MOVE "C-REC-01" TO RC-C.
           WRITE RC-C.
           CLOSE SQ-C.
           OPEN OUTPUT RL-D.
           MOVE 1 TO RK-D.
           MOVE "D-REC-01" TO RC-D.
           WRITE RC-D.
           CLOSE RL-D.
           OPEN OUTPUT RL-E.
           MOVE "E-REC-01" TO RC-E.
           WRITE RC-E.
           CLOSE RL-E.
           OPEN OUTPUT IX-G.
           MOVE "0005G-01" TO RC-G.
           WRITE RC-G.
           CLOSE IX-G.
           DISPLAY "SETUP " ST-A ST-B ST-C ST-D ST-E ST-G.
       STEP-1.
           MOVE 1 TO WS-STEP.
           OPEN INPUT RL-A.
           READ RL-A.
           READ RL-A.
           DISPLAY "STEP 1 RL-A STATUS " ST-A.
           CLOSE RL-A.
       STEP-2.
           MOVE 2 TO WS-STEP.
           OPEN I-O IX-B.
           MOVE "0009" TO KY-B.
           READ IX-B.
           DISPLAY "STEP 2 IX-B STATUS " ST-B.
           CLOSE IX-B.
       STEP-3.
           MOVE 3 TO WS-STEP.
           OPEN INPUT SQ-C.
           READ SQ-C.
           READ SQ-C.
           DISPLAY "STEP 3 SQ-C STATUS " ST-C.
           CLOSE SQ-C.
       STEP-4.
           MOVE 4 TO WS-STEP.
           OPEN INPUT RL-D.
           MOVE 9 TO RK-D.
           READ RL-D.
           DISPLAY "STEP 4 RL-D STATUS " ST-D.
           CLOSE RL-D.
       STEP-5.
           MOVE 5 TO WS-STEP.
           OPEN I-O RL-E.
           READ RL-E.
           READ RL-E.
           DISPLAY "STEP 5 RL-E STATUS " ST-E.
           CLOSE RL-E.
       STEP-6.
           MOVE 6 TO WS-STEP.
           OPEN OUTPUT RL-F.
           MOVE 3 TO RK-F.
           MOVE "F-REC-01" TO RC-F.
           WRITE RC-F.
           MOVE "F-REC-02" TO RC-F.
           WRITE RC-F.
           DISPLAY "STEP 6 RL-F STATUS " ST-F.
           CLOSE RL-F.
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
