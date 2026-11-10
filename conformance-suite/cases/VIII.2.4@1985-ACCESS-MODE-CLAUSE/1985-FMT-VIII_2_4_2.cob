      *>--- conformance-case
      *> id: 1985-FMT-VIII_2_4_2
      *> rules: [1985-FMT-VIII.2.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.2 (THE ACCESS MODE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELACC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "RELSEQ"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS SKEY.
           SELECT RFILE ASSIGN TO "RELRAN"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKEY.
           SELECT DFILE ASSIGN TO "RELDYN"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS DKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(5).
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(5).
       FD  DFILE
           LABEL RECORDS ARE STANDARD.
       01  DREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  SKEY PIC 9(4) VALUE 0.
       01  RKEY PIC 9(4).
       01  DKEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           MOVE "SEQ-1" TO SREC.
           WRITE SREC INVALID KEY DISPLAY "WRITE S1 FAILED".
           MOVE "SEQ-2" TO SREC.
           WRITE SREC INVALID KEY DISPLAY "WRITE S2 FAILED".
           DISPLAY "SKEY " SKEY.
           CLOSE SFILE.
           OPEN OUTPUT RFILE.
           MOVE 3 TO RKEY.
           MOVE "THIRD" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE R3 FAILED".
           MOVE 1 TO RKEY.
           MOVE "FIRST" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE R1 FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE 3 TO RKEY.
           READ RFILE INVALID KEY DISPLAY "READ R3 FAILED".
           DISPLAY RREC.
           MOVE 2 TO RKEY.
           READ RFILE INVALID KEY DISPLAY "NO RECORD 2".
           MOVE 1 TO RKEY.
           READ RFILE INVALID KEY DISPLAY "READ R1 FAILED".
           DISPLAY RREC.
           CLOSE RFILE.
           OPEN OUTPUT DFILE.
           MOVE 2 TO DKEY.
           MOVE "DYN-2" TO DREC.
           WRITE DREC INVALID KEY DISPLAY "WRITE D2 FAILED".
           MOVE 1 TO DKEY.
           MOVE "DYN-1" TO DREC.
           WRITE DREC INVALID KEY DISPLAY "WRITE D1 FAILED".
           CLOSE DFILE.
           OPEN INPUT DFILE.
           READ DFILE NEXT RECORD AT END DISPLAY "READ NEXT FAILED".
           DISPLAY DREC.
           MOVE 2 TO DKEY.
           READ DFILE INVALID KEY DISPLAY "READ D2 FAILED".
           DISPLAY DREC.
           CLOSE DFILE.
           STOP RUN.
