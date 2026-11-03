      *>--- conformance-case
      *> id: pb4435-occurs-bound-omitted-at-85
      *> rules: [FMT-13.18.38.2, SR-5.5-1, SR-13.10.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4435 - OCCURS 0 TO TIMES omits integer-2. At COBOL-85
      *>   the position takes an integer literal only: ISO 5.5 1) makes
      *>   integer-n "a fixed-point integer literal", and the word 13.10.3
      *>   SR2 lets stand there ("constant-name-1 may be used anywhere that
      *>   a format specifies a literal") is a constant-name, which the
      *>   COBOL-2002 constant entry introduced. So the refusal expects an
      *>   integer literal and names no user-defined word. It fails if the
      *>   sentence offers 'a user-defined word' at 85. Its twin
      *>   pb4301-occurs-to-without-integer-2 names the word from 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4435A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       01 T.
          05 E PIC X OCCURS 0 TO TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
