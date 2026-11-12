      *>--- conformance-case
      *> id: 1985-SR-IX_4_3_3-2
      *> rules: [1985-SR-IX.4.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.3 SR2 (THE DELETE STATEMENT). A refusal: the rule
      *> forbids line 26 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05 IDX-KEY PIC X(4).
           05 IDX-DATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           MOVE "DATA" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IDX-FILE.
           OPEN I-O IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           DELETE IDX-FILE RECORD.
           CLOSE IDX-FILE.
           STOP RUN.
