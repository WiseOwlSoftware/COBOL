      *>--- conformance-case
      *> id: pb5175-trailing-currency-cr-85
      *> rules: [SR-13.18.40.3-26]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5175 - 1985:VI.5.9.5 GR6: "The currency symbol must be the leftmost character position to be
      *> counted in the size of the item except that it can be preceded by either a '+' or a '-' symbol." (cite.py: OK).
      *> The 1985 chart (VI.5.9.6) has one non-floating cs row, the leading one. COBOL-2002 adds the trailing place,
      *> "the rightmost symbol in character-string-1" (2002 13.16.38.2 SR23, 13.18.40.3 SR26).
      *> PIC 999$CR puts it before CR, after the digits: refused COBOL1935 at 85.
      *> conformance:pb5175-trailing-currency is the accepted side from COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5175TR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC 999$CR.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REACHED".
           STOP RUN.
