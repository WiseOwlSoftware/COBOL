      *>--- conformance-case
      *> id: pb5736-if-directive-trailing-semicolon
      *> rules: [SR-7.3.16.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:7.3.16.3 SR2 - "Text-1 shall begin on a new line."
      *>   cite.py: OK  §2023:7.3.16.3 SR2  (Syntax rules)
      *> ISO §2023:7.3.3 SR3 - a compiler directive "may be followed only by
      *>   space characters and an optional inline comment".
      *>   cite.py: OK  §2023:7.3.3 SR3  (Syntax rules)
      *> The semicolon ending the >>IF line is neither a space character nor
      *> a comment, so the directive line is malformed, exactly as its comma
      *> twin (>>IF 1 = 1,) is. The lexer hid the semicolon, so the line
      *> compiled and ran. Removing the semicolon makes the program legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5736IF.
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF 1 = 1;
           DISPLAY "A".
       >>END-IF
           STOP RUN.
