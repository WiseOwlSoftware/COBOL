      *>--- conformance-case
      *> id: 1985-FMT-VI_6_26_2-function-operand-s3
      *> rules: [1985-FMT-VI.6.26.2, 1985-SR-IV.4.3.8.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85. §1985:VI.6.26.2 writes SUBTRACT's
      *> identifier-1 as an identifier, and §1985:IV.4.3.8.3.3
      *> SR4 says a numeric or integer function "may be used only
      *> in an arithmetic expression" (both cite.py --check OK); an
      *> arithmetic statement's operand is not one. The program writes
      *> FUNCTION INTEGER (2.5) there, so WiseOwl refuses it at 85
      *> with COBOL0900 at the function; a compiler that accepts it
      *> fails (registrar re-read, 2026-11-15; the landed fix is
      *> kb/Work PB5108).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85S3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 99 VALUE 50.
       01  R PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT FUNCTION INTEGER (2.5) FROM A
           STOP RUN.
