      *>--- conformance-case
      *> id: 1985-SR-IX_3_2_3-1
      *> rules: [1985-SR-IX.3.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.3.2.3 SR1 (THE FILE DESCRIPTION ENTRY). A refusal: the
      *> rule forbids line 12 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0001 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO "IDXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
           IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY  PIC X(5).
           05  IDX-DATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
