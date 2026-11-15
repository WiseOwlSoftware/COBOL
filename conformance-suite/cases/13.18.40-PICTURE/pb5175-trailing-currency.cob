      *>--- conformance-case
      *> id: pb5175-trailing-currency
      *> rules: [SR-13.18.40.3-26]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5175 - 13.18.40.3 SR26: the currency symbol is the leftmost symbol, optionally preceded by + or -,
      *> "or the rightmost symbol in character-string-1, optionally followed by one of the symbols '+', '-', 'CR', or
      *> 'DB'" (cite.py: OK; 2002 13.16.38.2 SR23 first). 12 in 999$ is 012$; -12 in +999$ is -012$; -12 in 999$CR
      *> is 012$CR; 12 in $999 is $012.
      *> conformance:pb5175-trailing-currency-85 is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5175TA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC 999$.
       01  E2 PIC +999$.
       01  E3 PIC 999$CR.
       01  C1 PIC $999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 12 TO E1.
           DISPLAY "[" E1 "]".
           MOVE -12 TO E2.
           DISPLAY "[" E2 "]".
           MOVE -12 TO E3.
           DISPLAY "[" E3 "]".
           MOVE 12 TO C1.
           DISPLAY "[" C1 "]".
           STOP RUN.
