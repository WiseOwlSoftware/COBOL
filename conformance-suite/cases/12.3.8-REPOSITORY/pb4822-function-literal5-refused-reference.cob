*>--- conformance-case
*> id: pb4822-function-literal5-refused-reference
*> rules: [SR-12.3.8.3-2, FMT-12.3.8.2, DOC-A.1-163]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.8.3 SR2: literal-5 shall not be a zero-length literal. The refusal is reported where the
      *> specifier is written; the reference to the function below is its cascade and draws no second
      *> diagnostic of its own (kb/Work PB4822). Below 2002 there is no REPOSITORY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4822NEG1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION MYF AS "".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R = FUNCTION MYF(1)
           STOP RUN.
