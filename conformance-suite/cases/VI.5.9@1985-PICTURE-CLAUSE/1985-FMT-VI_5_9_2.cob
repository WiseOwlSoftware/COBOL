      *>--- conformance-case
      *> id: 1985-FMT-VI_5_9_2
      *> rules: [1985-FMT-VI.5.9.2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.5.9.2 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ITEM-A PICTURE IS X(3) VALUE "ABC".
       01  ITEM-B PIC 9(2) VALUE 42.
       01  ITEM-C PICTURE 9(3) VALUE 7.
       01  ITEM-D PIC IS XX VALUE "ZZ".
       01  ITEM-B-E PIC Z9.
       01  ITEM-C-E PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY ITEM-A.
           MOVE ITEM-B TO ITEM-B-E
           DISPLAY ITEM-B-E.
           MOVE ITEM-C TO ITEM-C-E
           DISPLAY ITEM-C-E.
           DISPLAY ITEM-D.
           STOP RUN.
