      *>--- conformance-case
      *> id: 1985-FMT-IX_4_6_2
      *> rules: [1985-FMT-IX.4.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.2 (THE REWRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05 IKEY  PIC X(4).
           05 IDATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WREC.
           05 WKEY  PIC X(4).
           05 WDATA PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "K001" TO IKEY.
           MOVE "OLDVAL" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IFILE.
           OPEN I-O IFILE.
           MOVE "K001" TO WKEY.
           MOVE "NEWVAL" TO WDATA.
           REWRITE IREC FROM WREC
               INVALID KEY DISPLAY "REWRITE INVALID"
               NOT INVALID KEY DISPLAY "REWRITE OK"
           END-REWRITE.
           MOVE "K999" TO WKEY.
           MOVE "BADVAL" TO WDATA.
           REWRITE IREC FROM WREC
               INVALID KEY DISPLAY "REWRITE INVALID"
               NOT INVALID KEY DISPLAY "REWRITE OK"
           END-REWRITE.
           MOVE "K001" TO IKEY.
           READ IFILE INVALID KEY DISPLAY "READ FAILED".
           DISPLAY "DATA=" IDATA.
           CLOSE IFILE.
           STOP RUN.
