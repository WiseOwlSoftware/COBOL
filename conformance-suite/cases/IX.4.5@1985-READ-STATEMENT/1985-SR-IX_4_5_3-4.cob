      *>--- conformance-case
      *> id: 1985-SR-IX_4_5_3-4
      *> rules: [1985-SR-IX.4.5.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.3 SR4 (THE READ STATEMENT). A refusal: the rule
      *> forbids line 25 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1720 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDSEQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXF
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY  PIC X(4).
           05  IDX-DATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           MOVE "HELLO" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IDX-FILE.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE INVALID KEY
               DISPLAY "NO RECORD".
           CLOSE IDX-FILE.
           STOP RUN.
