      *>--- conformance-case
      *> id: 1985-SR-IX_4_7_3-2
      *> rules: [1985-SR-IX.4.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.3 SR2 (THE START STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS CUST-KEY OF CUST-REC.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  CUST-REC.
           05  CUST-KEY  PIC X(3).
           05  CUST-NAME PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05  CUST-KEY  PIC X(3).
           05  CUST-NAME PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "AAA" TO CUST-KEY OF CUST-REC.
           MOVE "ALFA" TO CUST-NAME OF CUST-REC.
           WRITE CUST-REC INVALID KEY DISPLAY "W1 FAIL".
           MOVE "BBB" TO CUST-KEY OF CUST-REC.
           MOVE "BETA" TO CUST-NAME OF CUST-REC.
           WRITE CUST-REC INVALID KEY DISPLAY "W2 FAIL".
           MOVE "CCC" TO CUST-KEY OF CUST-REC.
           MOVE "GAMA" TO CUST-NAME OF CUST-REC.
           WRITE CUST-REC INVALID KEY DISPLAY "W3 FAIL".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           MOVE "BBB" TO CUST-KEY OF CUST-REC.
           START IFILE KEY IS NOT LESS THAN CUST-KEY OF CUST-REC
               INVALID KEY DISPLAY "START FAIL".
           READ IFILE NEXT RECORD AT END DISPLAY "EOF1".
           DISPLAY CUST-NAME OF CUST-REC.
           READ IFILE NEXT RECORD AT END DISPLAY "EOF2".
           DISPLAY CUST-NAME OF CUST-REC.
           CLOSE IFILE.
           STOP RUN.
