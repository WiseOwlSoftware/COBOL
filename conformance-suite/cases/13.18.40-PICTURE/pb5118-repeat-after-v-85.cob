      *>--- conformance-case
      *> id: pb5118-repeat-after-v-85
      *> rules: [SR-13.18.40.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5118 - 1985:VI.5.9.4 GR7: "An unsigned nonzero integer which is enclosed in parentheses
      *> following the symbols 'A', ',', 'X', '9', 'P', 'Z', '*', 'B', '/', '0', '+', '-', or the currency symbol
      *> indicates the number of consecutive occurrences of the symbol" (cite.py: OK).
      *> V is not in the list, so V(1) is not a COBOL-85 character-string: refused COBOL7078.
      *> conformance:pb5118-repeat-after-any-symbol is the accepted side from COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5118RV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-ITEM PIC 9(3)V(1)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REACHED".
           STOP RUN.
