      *>--- conformance-case
      *> id: pb5924-seq-update-in-place-85
      *> rules: [1985-GR-VII.1.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VII.1.3.1 (cite.py: OK): "new records cannot be added to the file and each replaced record must be the same size"
      *> Updating a sequential mass storage file in place (OPEN I-O): a same-size
      *> REWRITE succeeds (00), another size fails (44), a WRITE fails (48).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQINPL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "SEQINPL.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC-A PIC X(10).
       01  SREC-B PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           MOVE "AAAAAAAAAA" TO SREC-A.
           WRITE SREC-A.
           MOVE "BBBBBBBBBB" TO SREC-A.
           WRITE SREC-A.
           CLOSE SFILE.
           OPEN I-O SFILE.
           DISPLAY "OPEN I-O " WS-STAT.
           READ SFILE AT END CONTINUE END-READ.
           MOVE "CCCCCCCCCC" TO SREC-A.
           REWRITE SREC-A.
           DISPLAY "REWRITE SAME SIZE " WS-STAT.
           READ SFILE AT END CONTINUE END-READ.
           MOVE "DDDDD" TO SREC-B.
           REWRITE SREC-B.
           DISPLAY "REWRITE OTHER SIZE " WS-STAT.
           MOVE "EEEEEEEEEE" TO SREC-A.
           WRITE SREC-A.
           DISPLAY "WRITE IN I-O " WS-STAT.
           CLOSE SFILE.
           OPEN INPUT SFILE.
           PERFORM UNTIL WS-STAT NOT = "00"
               READ SFILE
                   AT END CONTINUE
                   NOT AT END
                       ADD 1 TO WS-N
                       DISPLAY "REC " SREC-A
               END-READ
           END-PERFORM.
           DISPLAY "RECORDS " WS-N.
           CLOSE SFILE.
           STOP RUN.
