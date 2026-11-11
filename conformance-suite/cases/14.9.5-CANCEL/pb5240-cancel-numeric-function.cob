      *>--- conformance-case
      *> id: pb5240-cancel-numeric-function
      *> rules: [SR-14.9.5.3-1]
      *> source-format: fixed
      *>---
      *> ISO 14.9.5.3 SR1: "Identifier-1 shall be defined as an
      *> alphanumeric or national data item." A function-identifier is
      *> an identifier there (kb/Work PB5240), and FUNCTION LENGTH is a
      *> numeric function, so the CANCEL below is refused at every
      *> edition with COBOL1696, as a numeric data item is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5240N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC X(6) VALUE "CFSUB1".
       PROCEDURE DIVISION.
       P1.
           CANCEL FUNCTION LENGTH (N).
           STOP RUN.
