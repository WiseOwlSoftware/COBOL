      *>--- conformance-case
      *> id: pb5070-data-record-not-an-01
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.3.5.3 SR1: "Data-name-1 is the name of a data record and must have an 01 level-number
      *> record description, with the same name, associated with it." OUT-FLD is an 02 item of R1.
      *> conformance:pb5070_obsolete_fd_clauses_85 is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070DR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070DR.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           DATA RECORD IS OUT-FLD.
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
