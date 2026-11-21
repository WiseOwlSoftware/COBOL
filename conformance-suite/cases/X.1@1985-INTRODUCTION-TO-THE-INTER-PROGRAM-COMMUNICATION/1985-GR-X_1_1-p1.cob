      *>--- conformance-case
      *> id: 1985-GR-X_1_1-p1
      *> rules: [1985-GR-X.1.1-p1, 1985-GR-X.1.2-p1, 1985-GR-X.1.2-p2, 1985-GR-X.1.3.1-p1, 1985-GR-X.1.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.1 p1; §1985:X.1.2 p1; §1985:X.1.2 p2; §1985:X.1.3.1
      *> p1; §1985:X.1.3.1 p2 (INTRODUCTION TO THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-14.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-14 probe p04: Inter-Program Communication at 85 -- transfer
      * of control by a name known at compile time (CALL literal) and
      * by one not known (CALL identifier), ON OVERFLOW, parameters
      * passed, GLOBAL data and file shared with a directly and an
      * indirectly contained program, EXTERNAL data.
      * Rows: 1985-GR-X.1.1-p1 X.1.2-p1 X.1.2-p2 X.1.3.1-p1 X.1.3.1-p2.
      * Expected output in p04_ipc.out (from the 1985 text).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "S14GLOB"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  GF IS GLOBAL
           LABEL RECORDS ARE STANDARD.
       01  GF-REC          PIC X(5).
       WORKING-STORAGE SECTION.
       01  G-CNT           PIC 9(2) VALUE 0 GLOBAL.
       01  PNAME           PIC X(8).
       01  PARM            PIC X(5) VALUE "HELLO".
       01  EXT-X           PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "EXT1" TO EXT-X.
           OPEN OUTPUT GF.
           CALL "MIDPROG" USING PARM.
           DISPLAY "BACK " PARM " " G-CNT.
           MOVE "MIDPROG" TO PNAME.
           MOVE "AGAIN" TO PARM.
           CALL PNAME USING PARM.
           DISPLAY "DYN " PARM " " G-CNT.
           MOVE "NOSUCHPG" TO PNAME.
           CALL PNAME ON OVERFLOW DISPLAY "OVERFLOW".
           CLOSE GF.
           OPEN INPUT GF.
           READ GF AT END DISPLAY "EMPTY".
           DISPLAY "FILE " GF-REC.
           CLOSE GF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MIDPROG.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P               PIC X(5).
       PROCEDURE DIVISION USING P.
       MID-PARA.
           ADD 1 TO G-CNT.
           DISPLAY "MID " P.
           MOVE "WORLD" TO P.
           CALL "INNERP".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-X           PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
       IN-PARA.
           ADD 10 TO G-CNT.
           DISPLAY "INNER " EXT-X " " G-CNT.
           MOVE "INNER" TO GF-REC.
           WRITE GF-REC.
           EXIT PROGRAM.
       END PROGRAM INNERP.
       END PROGRAM MIDPROG.
       END PROGRAM OUTERP.
