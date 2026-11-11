      *>--- conformance-case
      *> id: pb5070-value-of-without-implementor-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - 1985:VII.3.9.2 prints implementor-name-1 inside the braces before every operand, and 1985:IV.2.1.4:
      *> "one of the options contained within the braces must be either explicitly specified or implicitly selected".
      *> VALUE OF "F1DATA" has no implementor-name-1. conformance:pb5070_obsolete_fd_clauses_85 is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070VN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070VN.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           VALUE OF "F1DATA".
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
