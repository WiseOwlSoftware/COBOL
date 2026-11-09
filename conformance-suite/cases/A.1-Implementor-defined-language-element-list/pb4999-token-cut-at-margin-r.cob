      *>--- conformance-case
      *> id: pb4999-token-cut-at-margin-r
      *> rules: [DOC-A.1-158]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 6.3.1 (A.1 item 158): margin R is position 72.
      *>   cite.py --check 6.3.1 'Margin R is immediately to the right
      *>   of the rightmost character position of the program-text
      *>   area.'  -> OK
      *> The COMPUTE below has its '.' at position 72 and its '7' at
      *> position 73, past margin R, so the program text is
      *> COMPUTE X = Y ** 0. and the 7 is ignored. With Y = 4 the
      *> exponent 0 gives X = 1; had the 7 been read the exponent
      *> would be 0.7 and X = 4 ** 0.7 = 2.64, shown as 2 in PIC 9.
      *> kb/Work PB4999: COBOL6688 warns on that line (unit test).
      *> Derivation: OUT=[1]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4999P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC 9 VALUE 4.
       01 X PIC 9.
       PROCEDURE DIVISION.
       PB4999-LEAD.
           COMPUTE X = Y ** 0                                          .7
           DISPLAY X.
           STOP RUN.
