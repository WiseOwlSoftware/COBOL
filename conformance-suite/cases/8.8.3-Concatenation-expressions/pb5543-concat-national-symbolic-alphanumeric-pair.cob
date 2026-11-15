*>--- conformance-case
*> id: pb5543-concat-national-symbolic-alphanumeric-pair
*> rules: [SR-8.8.3.2-1, GR-8.8.3.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5543 - SYM-N is declared SYMBOLIC CHARACTERS FOR
      *> NATIONAL, so it is a national figurative constant (12.3.7.4
      *> GR11 c) and has no alphanumeric character value. In "X" &
      *> SYM-N the figurative takes the class of its partner, which is
      *> alphanumeric (8.8.3.3 GR1 a): the operands are not of the same
      *> class (8.8.3.2 SR1), COBOL1540. (The folder used to drop the
      *> national category and fold the pair as alphanumeric.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG5543NSA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SYM-N IS 67.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "X" & SYM-N TO W-X.
           DISPLAY W-X.
           STOP RUN.
