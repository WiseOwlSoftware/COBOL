      *>--- conformance-case
      *> id: g85_bf28_04
      *> rules: [1985-SR-XIII.4.8.3-5, 1985-SR-XIII.4.8.3-6, 1985-GR-XIII.4.8.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.3 SR5 - mixed organization in one USE
      *> §1985:XIII.4.8.3 SR6 - each mode phrase once in DECLARATIVES
      *> §1985:XIII.4.8.4 GR4 - an explicit file-name excludes other USEs
      * SR5: the explicit USE names RPT-F (a report file, sequential)
      *   and IX-A (indexed); fails if refused or the handler misses
      *   either.
      * SR6: OUTPUT and EXTEND are each specified once; accepted, and
      *   each fires once (step 2 IX-O, step 3 IX-X: WRITE out of key
      *   order, IX.4.9.4 GR17a, status 21, not critical, so GR6 returns
      *   control to the next statement).
      * GR4: IX-A (step 1, status 21) and RPT-F (step 4) are open in
      *   output mode while an OUTPUT USE exists; only USE-FILES may run
      *   for them. Step 4 writes the report (INITIATE, GENERATE,
      *   TERMINATE), then a second OPEN OUTPUT of the open report file
      *   gives status 41 (VII.1.3.5, the I-O status GR6 points to). 41
      *   is critical, so what follows a returning USE is the
      *   implementor's; the handler ends the run itself with STOP RUN,
      *   and NOT REACHED must not appear. A USE-OUTPUT line at step 1
      *   or 4, or COUNTS OUTPUT other than 1, fails GR4.
      * §1985:XIII.4.8.4 GR6: after a critical 41 the action is the
      *   implementor's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2804.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "bf2804r.txt"
               FILE STATUS IS ST-R.
           SELECT IX-A ASSIGN TO "bf2804a.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-A
               FILE STATUS IS ST-A.
           SELECT IX-O ASSIGN TO "bf2804o.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-O
               FILE STATUS IS ST-O.
           SELECT IX-X ASSIGN TO "bf2804x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KY-X
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-F
           REPORT IS RPT.
       FD  IX-A.
       01  RC-A.
           05 KY-A PIC X(4).
           05 DT-A PIC X(4).
       FD  IX-O.
       01  RC-O.
           05 KY-O PIC X(4).
           05 DT-O PIC X(4).
       FD  IX-X.
       01  RC-X.
           05 KY-X PIC X(4).
           05 DT-X PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-R PIC XX.
       01  ST-A PIC XX.
       01  ST-O PIC XX.
       01  ST-X PIC XX.
       01  WS-STEP PIC 9 VALUE 0.
       01  N-FILES PIC 9 VALUE 0.
       01  N-OUT PIC 9 VALUE 0.
       01  N-EXT PIC 9 VALUE 0.
       REPORT SECTION.
       RD  RPT.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "LINE-1".
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-FILES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RPT-F IX-A.
       USE-FILES-P.
           ADD 1 TO N-FILES.
           DISPLAY "USE-FILES STEP " WS-STEP.
           IF WS-STEP = 4
               DISPLAY "STEP 4 RPT-F STATUS " ST-R
               DISPLAY "COUNTS FILES " N-FILES " OUTPUT " N-OUT
                   " EXTEND " N-EXT
               STOP RUN.
       USE-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
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
           OPEN OUTPUT IX-X.
           MOVE "0005X-01" TO RC-X.
           WRITE RC-X.
           CLOSE IX-X.
           DISPLAY "SETUP " ST-X.
       STEP-1.
           MOVE 1 TO WS-STEP.
           OPEN OUTPUT IX-A.
           MOVE "0002A-01" TO RC-A.
           WRITE RC-A.
           MOVE "0001A-02" TO RC-A.
           WRITE RC-A.
           DISPLAY "STEP 1 IX-A STATUS " ST-A.
           CLOSE IX-A.
       STEP-2.
           MOVE 2 TO WS-STEP.
           OPEN OUTPUT IX-O.
           MOVE "0002O-01" TO RC-O.
           WRITE RC-O.
           MOVE "0001O-02" TO RC-O.
           WRITE RC-O.
           DISPLAY "STEP 2 IX-O STATUS " ST-O.
           CLOSE IX-O.
       STEP-3.
           MOVE 3 TO WS-STEP.
           OPEN EXTEND IX-X.
           MOVE "0007X-02" TO RC-X.
           WRITE RC-X.
           MOVE "0006X-03" TO RC-X.
           WRITE RC-X.
           DISPLAY "STEP 3 IX-X STATUS " ST-X.
           CLOSE IX-X.
       STEP-4.
           MOVE 4 TO WS-STEP.
           OPEN OUTPUT RPT-F.
           INITIATE RPT.
           GENERATE DTL.
           TERMINATE RPT.
           DISPLAY "REPORT WRITTEN " ST-R.
           OPEN OUTPUT RPT-F.
           DISPLAY "NOT REACHED AFTER SECOND OPEN".
           STOP RUN.
