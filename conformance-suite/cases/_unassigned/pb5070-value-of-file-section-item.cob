      *>--- conformance-case
      *> id: pb5070-value-of-file-section-item
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.3.9.3 SR2: "Data-name-1 must be in the Working-Storage Section." R1 is the file's own
      *> record, an item of the File Section. conformance:pb5070_obsolete_fd_clauses_85 is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070VF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070VF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           VALUE OF FILE-ID IS R1.
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
