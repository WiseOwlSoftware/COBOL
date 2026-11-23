      *>--- conformance-case
      *> id: pb5736-if-directive-trailing-comment
      *> rules: [SR-7.3.16.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:7.3.3 SR3 - a compiler directive "may be followed only by
      *>   space characters and an optional inline comment". So the >>IF
      *>   operand may end in an inline comment, which a trailing semicolon
      *>   may not (pb5736-if-directive-trailing-semicolon).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5736OK.
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF 1 = 1 *> text-1 follows on the next line
           DISPLAY "A".
       >>ELSE
           DISPLAY "B".
       >>END-IF
           STOP RUN.
