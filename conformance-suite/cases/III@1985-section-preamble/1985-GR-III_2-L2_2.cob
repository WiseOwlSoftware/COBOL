      *>--- conformance-case
      *> id: 1985-GR-III_2-L2_2
      *> rules: [1985-GR-III.2-L2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 GRL2.2 ((section preamble)). A refusal: the rule
      *> forbids line 13 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1646 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 (2) Integer: a numeric data item with no digit
      * positions right of the decimal point. PERFORM identifier-1
      * TIMES requires an integer item; N PIC 9V9 is not one.
      * Control: L22_integer_item_control.cob (N PIC 99).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L22R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9V9 VALUE 2.0.
       01  C PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM ADD-ONE N TIMES.
           DISPLAY "L22R ACCEPTED " C.
           STOP RUN.
       ADD-ONE.
           ADD 1 TO C.
