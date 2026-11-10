      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_5-1
      *> rules: [1985-GR-IX.1.3.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.5 GR1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IKEYST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IKEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY   PIC X(4).
           05  IDATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS.
           05  FS1    PIC X.
           05  FS2    PIC X.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IFILE.
           MOVE "AAAA" TO IKEY.
           MOVE "DATA01" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "AAAA" TO IKEY.
           WRITE IREC INVALID KEY DISPLAY "DUP INVALID".
           DISPLAY FS1.
           CLOSE IFILE.
           STOP RUN.
