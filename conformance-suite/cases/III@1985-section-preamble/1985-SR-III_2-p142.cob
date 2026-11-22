      *>--- conformance-case
      *> id: 1985-SR-III_2-p142
      *> rules: [1985-SR-III.2-p142]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p142 ((section preamble)). A refusal: the rule
      *> forbids line 10 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2386 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p142: an integer in a general format must not be
      * zero unless the format allows it: OCCURS 0 TIMES (format 1)
      * is refused. Control: p142_zero_allowed.cob, where OCCURS
      * format 2 explicitly allows integer-1 of zero.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P142Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS 0 TIMES.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "P142Z ACCEPTED".
           STOP RUN.
