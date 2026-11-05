      *>--- conformance-case
      *> id: pb4677-cd-in-function
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO/IEC 1989:2002 Annex A: "The communication facility may be
      *> used only in a program definition."
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4677RE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RET PIC X.
       COMMUNICATION SECTION.
       CD C1 FOR INPUT STATUS KEY IS S1.
       PROCEDURE DIVISION RETURNING RET.
       MAIN.
           GOBACK.
       END FUNCTION PB4677RE.
