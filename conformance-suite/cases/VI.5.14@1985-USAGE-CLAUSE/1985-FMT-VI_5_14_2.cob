      *>--- conformance-case
      *> id: 1985-FMT-VI_5_14_2
      *> rules: [1985-FMT-VI.5.14.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.2 (THE USAGE CLAUSE). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USAGET.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  B-ITEM PIC S9(4) USAGE IS BINARY.
       77  C-ITEM PIC S9(4) USAGE COMPUTATIONAL.
       77  K-ITEM PIC S9(4) COMP.
       77  D-ITEM PIC 9(4) USAGE DISPLAY.
       77  X-ITEM USAGE IS INDEX.
       77  P-ITEM PIC S9(4) PACKED-DECIMAL.
       77  R-ITEM PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           MOVE 12 TO B-ITEM.
           MOVE 30 TO C-ITEM.
           ADD B-ITEM C-ITEM GIVING K-ITEM.
           MOVE 100 TO P-ITEM.
           ADD K-ITEM TO P-ITEM.
           MOVE P-ITEM TO R-ITEM.
           DISPLAY R-ITEM.
           MOVE 7 TO D-ITEM.
           DISPLAY D-ITEM.
           STOP RUN.
