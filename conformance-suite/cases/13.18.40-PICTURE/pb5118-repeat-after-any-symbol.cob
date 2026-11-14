      *>--- conformance-case
      *> id: pb5118-repeat-after-any-symbol
      *> rules: [SR-13.18.40.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5118 - 13.18.40.3 SR6: "An unsigned nonzero integer that is enclosed in parentheses indicates the
      *> number of consecutive occurrences of the symbol that immediately precedes the left parenthesis." (cite.py: OK;
      *> 2002 13.16.38.2 SR7 says the same.) Any symbol takes the factor from COBOL-2002: V(1) is one V and CR(1) one CR.
      *> A-ITEM is 999V9: 123.4 moves to 999.9 as 123.4; C-ITEM is 99CR, -5 edits as 05CR.
      *> conformance:pb5118-repeat-after-v-85 is the refused side at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5118RA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-ITEM PIC 9(3)V(1)9 VALUE 123.4.
       01  C-ITEM PIC 9(2)CR(1).
       01  A-EDIT PIC 999.9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE A-ITEM TO A-EDIT.
           DISPLAY A-EDIT.
           MOVE -5 TO C-ITEM.
           DISPLAY C-ITEM.
           STOP RUN.
