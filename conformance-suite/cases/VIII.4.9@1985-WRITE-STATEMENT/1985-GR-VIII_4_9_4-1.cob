      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-1
      *> rules: [1985-GR-VIII.4.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR1 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELWRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKEY
               FILE STATUS IS FSTAT.
           SELECT SFILE ASSIGN TO RELSEQ
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SSTAT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(10).
       WORKING-STORAGE SECTION.
       77  RKEY PIC 9(4).
       77  FSTAT PIC XX.
       77  SSTAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFILE.
       ERR-PARA.
           DISPLAY "USE " FSTAT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE 1 TO RKEY.
           MOVE "RECORD-ONE" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "OUTPUT WRITE FAILED".
           IF FSTAT = "00" DISPLAY "OUTPUT WRITE OK".
           CLOSE RFILE.
           OPEN I-O RFILE.
           MOVE 2 TO RKEY.
           MOVE "RECORD-TWO" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "I-O WRITE FAILED".
           IF FSTAT = "00" DISPLAY "I-O WRITE OK".
           CLOSE RFILE.
           OPEN OUTPUT SFILE.
           MOVE "SEQ-ONE" TO SREC.
           WRITE SREC INVALID KEY DISPLAY "SEQ WRITE FAILED".
           CLOSE SFILE.
           OPEN EXTEND SFILE.
           MOVE "SEQ-TWO" TO SREC.
           WRITE SREC INVALID KEY DISPLAY "EXTEND WRITE FAILED".
           IF SSTAT = "00" DISPLAY "EXTEND WRITE OK".
           CLOSE SFILE.
           OPEN INPUT RFILE.
           MOVE 3 TO RKEY.
           MOVE "RECORD-3RD" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "INVALID KEY".
           DISPLAY "NOT REACHED".
           STOP RUN.
