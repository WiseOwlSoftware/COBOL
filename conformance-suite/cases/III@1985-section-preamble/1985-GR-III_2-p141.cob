      *>--- conformance-case
      *> id: 1985-GR-III_2-p141
      *> rules: [1985-GR-III.2-p141]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p141 ((section preamble)). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p141 / p142: where a general format says integer,
      * the literal must be an integer; 3.0 has a digit position to
      * the right of the decimal point, so OCCURS 3.0 is refused.
      * Control: p141_integer_literal_control.cob (OCCURS 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P141R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS 3.0 TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "P141R ACCEPTED".
           STOP RUN.
