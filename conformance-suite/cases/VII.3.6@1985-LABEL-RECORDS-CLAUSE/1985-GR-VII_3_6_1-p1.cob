      *>--- conformance-case
      *> id: 1985-GR-VII_3_6_1-p1
      *> rules: [1985-GR-VII.3.6.1-p1, 1985-GR-VII.4.2.1-p1, 1985-GR-VII.4.2.4-p1, 1985-GR-VII.4.3.1-p1, 1985-GR-VII.4.7.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.6.1 p1; §1985:VII.4.2.1 p1; §1985:VII.4.2.4 p1;
      *> §1985:VII.4.3.1 p1; §1985:VII.4.7.1 p1 (THE LABEL RECORDS
      *> CLAUSE). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-13.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-13 probe: 1985 Sequential I-O OPEN and CLOSE forms (VII.4.2,
      * VII.4.3) and WRITE ADVANCING (VII.4.7) at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQOC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "S13SEQOC.DAT"
               FILE STATUS IS WS-STAT.
           SELECT PFILE ASSIGN TO "S13PRINT.DAT"
               FILE STATUS IS WS-PSTAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE.
       01  SREC PIC X(4).
       FD  PFILE
           LABEL RECORD IS OMITTED.
       01  PREC PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-STAT  PIC XX.
       01  WS-PSTAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SFILE PFILE.
           DISPLAY "OPEN TWO " WS-STAT " " WS-PSTAT.
           MOVE "AAAA" TO SREC. WRITE SREC.
           MOVE "BBBB" TO SREC. WRITE SREC.
           MOVE "L1" TO PREC. WRITE PREC AFTER ADVANCING 1 LINE.
           MOVE "L3" TO PREC. WRITE PREC AFTER ADVANCING 2 LINES.
           MOVE "P2" TO PREC. WRITE PREC AFTER ADVANCING PAGE.
           CLOSE PFILE.
           CLOSE SFILE REEL.
           DISPLAY "CLOSE REEL " WS-STAT.
           CLOSE SFILE UNIT FOR REMOVAL.
           DISPLAY "CLOSE UNIT REMOVAL " WS-STAT.
           CLOSE SFILE.
           DISPLAY "CLOSE " WS-STAT.
           OPEN EXTEND SFILE.
           MOVE "CCCC" TO SREC. WRITE SREC.
           CLOSE SFILE WITH NO REWIND.
           DISPLAY "CLOSE NO REWIND " WS-STAT.
           OPEN INPUT SFILE REVERSED.
           DISPLAY "OPEN REVERSED " WS-STAT.
           READ SFILE AT END DISPLAY "END".
           DISPLAY "REV READ1 " SREC.
           READ SFILE AT END DISPLAY "END".
           DISPLAY "REV READ2 " SREC.
           CLOSE SFILE.
           OPEN INPUT SFILE WITH NO REWIND.
           DISPLAY "OPEN NO REWIND " WS-STAT.
           READ SFILE AT END DISPLAY "END".
           DISPLAY "NR READ1 " SREC.
           CLOSE SFILE WITH LOCK.
           DISPLAY "CLOSE LOCK " WS-STAT.
           OPEN INPUT SFILE.
           DISPLAY "OPEN AFTER LOCK " WS-STAT.
           STOP RUN.
