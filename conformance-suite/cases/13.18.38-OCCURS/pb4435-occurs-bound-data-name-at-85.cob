      *>--- conformance-case
      *> id: pb4435-occurs-bound-data-name-at-85
      *> rules: [FMT-13.18.38.2, SR-5.5-1, SR-13.10.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4435 - OCCURS K TIMES with K a data-name. integer-2
      *>   is "a fixed-point integer literal" (ISO 5.5 1)); the word
      *>   13.10.3 SR2 lets stand there is a constant-name, which COBOL-85
      *>   does not have. So at 85 the refusal (COBOL1547) says K is not
      *>   an integer literal and offers no constant-name. It fails if the
      *>   85 refusal names a constant-name as what the position admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4435C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 3.
       01 T.
          05 E PIC X OCCURS K TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
