      *>--- conformance-case
      *> id: pb5070-value-of-index-item
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.3.9.3 SR1: data-name-1 cannot "be an item described with the USAGE IS INDEX clause".
      *> WS-IX is an index data item. conformance:pb5070_obsolete_fd_clauses_85 is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070VX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070VX.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           VALUE OF ID IS WS-IX.
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       01  WS-IX USAGE IS INDEX.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
