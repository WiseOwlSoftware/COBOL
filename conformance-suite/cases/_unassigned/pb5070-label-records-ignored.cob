      *>--- conformance-case
      *> id: pb5070-label-records-ignored
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.3.6.2: LABEL {RECORD IS | RECORDS ARE} {STANDARD | OMITTED}; IGNORED is neither option
      *> (1985:IV.2.1.4). conformance:pb5070_obsolete_fd_clauses_85 is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070LI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070LI.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE IGNORED.
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
