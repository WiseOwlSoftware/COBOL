      *>--- conformance-case
      *> id: 1985-GR-VII_1_2-p1
      *> rules: [1985-GR-VII.1.2-p1, 1985-GR-VII.1.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.2 p1; §1985:VII.1.2 p2 (INTRODUCTION TO THE
      *> SEQUENTIAL I-O MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-99.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-99 g2: Sequential I-O level 1 and level 2 (VII.1.2 p1, p2)
      * at --std 85: file control, FD, I-O-CONTROL, and the CLOSE,
      * OPEN, READ, REWRITE, USE and WRITE statements, level 1 forms
      * (OPEN INPUT/OUTPUT/I-O, READ, WRITE, REWRITE, CLOSE, USE) and
      * level 2 forms (OPEN EXTEND, SAME RECORD AREA, RESERVE,
      * FILE STATUS, CLOSE REEL-less LOCK-free WITH NO REWIND).
      * Expected: COUNT=3 FIRST=AAX LAST=CC USE=1 then SEQLVL OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQLVL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "G2SEQLVL.DAT"
               RESERVE 2 AREAS
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT TFILE ASSIGN TO "G2SEQLVT.DAT"
               FILE STATUS IS WS-TSTAT.
       I-O-CONTROL.
           SAME RECORD AREA FOR SFILE TFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(4).
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-STAT  PIC XX.
       01  WS-TSTAT PIC XX.
       01  WS-USE   PIC 9 VALUE 0.
       01  WS-CNT   PIC 9 VALUE 0.
       01  WS-FIRST PIC X(4).
       01  WS-LAST  PIC X(4).
       01  WS-EOF   PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       S-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SFILE.
       S-PARA.
           ADD 1 TO WS-USE.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           MOVE "AA" TO SREC. WRITE SREC.
           MOVE "BB" TO SREC. WRITE SREC.
           CLOSE SFILE.
           OPEN EXTEND SFILE.
           MOVE "CC" TO SREC. WRITE SREC.
           CLOSE SFILE.
           OPEN I-O SFILE.
           READ SFILE AT END DISPLAY "BAD EOF1".
           MOVE "AAX" TO SREC.
           REWRITE SREC.
           CLOSE SFILE.
           OPEN INPUT SFILE.
           PERFORM UNTIL WS-EOF = "Y"
               READ SFILE AT END MOVE "Y" TO WS-EOF
               END-READ
               IF WS-EOF = "N"
                   ADD 1 TO WS-CNT
                   IF WS-CNT = 1 MOVE SREC TO WS-FIRST END-IF
                   MOVE SREC TO WS-LAST
               END-IF
           END-PERFORM.
           READ SFILE AT END CONTINUE END-READ.
           CLOSE SFILE WITH NO REWIND.
           DISPLAY "COUNT=" WS-CNT " FIRST=" WS-FIRST " LAST=" WS-LAST
               " USE=" WS-USE.
           IF WS-CNT = 3 AND WS-FIRST = "AAX" AND WS-LAST = "CC"
               AND WS-USE = 1
               DISPLAY "SEQLVL OK"
           ELSE
               DISPLAY "SEQLVL FAIL"
           END-IF.
           STOP RUN.
