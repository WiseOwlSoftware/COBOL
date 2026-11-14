      *>--- conformance-case
      *> id: pb5118-repeat-after-cr-85
      *> rules: [SR-13.18.40.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5118 - 1985:VI.5.9.4 GR7: "An unsigned nonzero integer which is enclosed in parentheses
      *> following the symbols 'A', ',', 'X', '9', 'P', 'Z', '*', 'B', '/', '0', '+', '-', or the currency symbol
      *> indicates the number of consecutive occurrences of the symbol" (cite.py: OK).
      *> CR is not in the list, so CR(1) is not a COBOL-85 character-string: refused COBOL7078.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5118RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C-ITEM PIC 9(3)CR(1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REACHED".
           STOP RUN.
