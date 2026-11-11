      *>--- conformance-case
      *> id: pb5074-display-decimal-literal-at-85
      *> rules: [FMT-14.9.11.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - the edition half of
      *>   pb5074_display_decimal_literal_2002. 1985 VI.6.10.3 SR2:
      *>   "If literal-1 is numeric, then it must be an unsigned
      *>   integer." 1.5 is no integer. Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074NDD.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY 1.5
           STOP RUN.
