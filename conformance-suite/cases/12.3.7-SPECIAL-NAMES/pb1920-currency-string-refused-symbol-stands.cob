      *>--- conformance-case
      *> id: pb1920-currency-string-refused-symbol-stands
      *> rules: [SR-12.3.7.3-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR23 b) - literal-7 "may consist of any characters
      *> from the computer's coded character set except for the digits 0
      *> through 9 ...", so "E1" is refused (COBOL0890). The PICTURE
      *> SYMBOL phrase (2002) still names '#', a valid literal-8 (SR26,
      *> SR27), so #9.99 below is the numeric-edited PICTURE the program
      *> means: the clause's one diagnostic stands alone, with no
      *> COBOL0808 / COBOL0819 cascade reading '#' as an invalid PICTURE
      *> character (kb/Work PB1920).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1920CS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "E1" WITH PICTURE SYMBOL "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC #9.99.
       PROCEDURE DIVISION.
           MOVE 1.5 TO A
           DISPLAY A
           STOP RUN.
