      *>--- conformance-case
      *> id: 1985-GR-IX_3_2_4-3
      *> rules: [1985-GR-IX.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.3.2.4 GR3 (THE FILE DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRIDX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO DISK
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS IREC.
       01  IREC.
           05  IKEY PIC X(4).
           05  IVAL PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IFILE.
           MOVE "K001" TO IKEY.
           MOVE "ABCD" TO IVAL.
           WRITE IREC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           READ IFILE AT END DISPLAY "EOF".
           DISPLAY IKEY " " IVAL.
           CLOSE IFILE.
           STOP RUN.
