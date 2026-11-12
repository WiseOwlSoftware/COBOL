      *>--- conformance-case
      *> id: pb5161-numval-c-currency-content
      *> rules: [AR-15.68.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5161 - from COBOL-2002 NUMVAL-C's argument-2 may not
      *> hold a period.
      *>
      *> §2023:15.68.3 AR2 (cite.py --check): argument-2 "shall not
      *> contain any of the digits 0 through 9; the characters '*',
      *> '+', '–', ',' , or '.'; or the two consecutive letters 'CR'
      *> or 'DB'"; the 2002 text words it the same. The literal "Fr."
      *> holds a period, so the program is refused (COBOL1627) at
      *> 2002, 2014 and 2023. The 1989 module has no such rule and
      *> the same reference is 12.50 there
      *> (pb5161_numval_c_currency_content_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5161B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(5)V99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-N = FUNCTION NUMVAL-C ("Fr.12.50" "Fr.").
           STOP RUN.
