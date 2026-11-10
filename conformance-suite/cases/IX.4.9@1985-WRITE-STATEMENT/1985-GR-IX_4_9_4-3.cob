      *>--- conformance-case
      *> id: 1985-GR-IX_4_9_4-3
      *> rules: [1985-GR-IX.4.9.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR3 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRFROM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS R-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  R.
           05  R-KEY  PIC X(4).
           05  R-DATA PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-SRC PIC X(10) VALUE "K001ABCDEF".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IFILE.
           MOVE ALL "Z" TO R.
           WRITE R FROM WS-SRC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           MOVE "K001" TO R-KEY.
           READ IFILE
               INVALID KEY DISPLAY "READ FAILED".
           DISPLAY "[" R-DATA "]".
           CLOSE IFILE.
           STOP RUN.
