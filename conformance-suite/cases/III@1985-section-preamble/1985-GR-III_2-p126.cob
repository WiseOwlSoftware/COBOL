      *>--- conformance-case
      *> id: 1985-GR-III_2-p126
      *> rules: [1985-GR-III.2-p126]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p126 ((section preamble)). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2241 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IMPNAMEX.
      * Refusal twin of IMPNAME.cob, the one line changed: FOOBAR names
      * no feature of this implementor's system (1985 III.2 p126).
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           FOOBAR IS CON
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IMPNAME VIA CONSOLE" UPON CON.
           IF SW1-OFF
               DISPLAY "SW1 OFF"
           ELSE
               DISPLAY "SW1 ON".
           DISPLAY "IMPNAME PASS".
           STOP RUN.
