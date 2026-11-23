      *>--- conformance-case
      *> id: pb5736-evaluate-when-trailing-semicolon
      *> rules: [SR-7.3.13.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:7.3.13.3 SR4 - text-1 shall begin on a new line; the
      *>   operand of >>EVALUATE and >>WHEN is "specified entirely on that line".
      *> ISO §2023:7.3.3 SR3 - a compiler directive "may be followed only by
      *>   space characters and an optional inline comment".
      *>   cite.py: OK  §2023:7.3.3 SR3  (Syntax rules)
      *> The semicolon ending the >>WHEN line is neither a space character nor
      *> a comment, so the directive line is malformed, as with a comma. The
      *> lexer hid the semicolon, so the line compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5736EV.
       PROCEDURE DIVISION.
       MAIN-P.
       >>EVALUATE 1
       >>WHEN 1;
           DISPLAY "A".
       >>END-EVALUATE
           STOP RUN.
