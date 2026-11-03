      *>--- conformance-case
      *> id: pb4305-display-signed-numeric-literal-at-85
      *> rules: [FMT-14.9.11.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_display_signed_numeric_literal_2002. COBOL-85
      *>   displayed an unsigned numeric literal only (2002 Annex F.2
      *>   item 46). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NDS.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY -5
           STOP RUN.
