      *>--- conformance-case
      *> id: 1985-GR-VII_4_2_4-3
      *> rules: [1985-GR-VII.4.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.2.4 GR3 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLOSET.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISKF1
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS F1-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(7).
       WORKING-STORAGE SECTION.
       01  F1-ST PIC XX.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       F1-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       F1-ERR-P.
           DISPLAY "USE " F1-ST.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "REC-ONE" TO R1.
           WRITE R1.
           CLOSE F1 UNIT.
           MOVE "REC-TWO" TO R1.
           WRITE R1.
           CLOSE F1 REEL FOR REMOVAL.
           MOVE "REC-TRE" TO R1.
           WRITE R1.
           CLOSE F1 WITH NO REWIND.
           OPEN INPUT F1.
           PERFORM READ-PARA UNTIL EOF-SW = "Y".
           CLOSE F1 WITH LOCK.
           DISPLAY "LOCK " F1-ST.
           OPEN INPUT F1.
           STOP RUN.
       READ-PARA.
           READ F1 AT END MOVE "Y" TO EOF-SW.
           IF EOF-SW = "N" DISPLAY R1.
