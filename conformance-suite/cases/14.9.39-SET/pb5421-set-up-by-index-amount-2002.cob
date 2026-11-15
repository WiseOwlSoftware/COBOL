      *>--- conformance-case
      *> id: pb5421-set-up-by-index-amount-2002
      *> rules: [FMT-14.9.39.2, GR-14.9.39.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5421 - the edition half of the 85 refusals. 14.9.39.2
      *>   prints arithmetic-expression-2 as the Format 2 amount, so an
      *>   index-name amount IY (occurrence 2) moves IX from 1 to 3, and
      *>   an index data item IDX set from IY then moves it to 5.
      *>   Expected: "C" then "E". It fails if either amount is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5421OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABCDE".
           05  E PIC X OCCURS 5 INDEXED BY IX IY.
       01  IDX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1
           SET IY TO 2
           SET IX UP BY IY
           DISPLAY E (IX)
           SET IDX TO IY
           SET IX UP BY IDX
           DISPLAY E (IX)
           STOP RUN.
