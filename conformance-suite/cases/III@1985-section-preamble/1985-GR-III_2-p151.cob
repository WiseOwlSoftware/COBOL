      *>--- conformance-case
      *> id: 1985-GR-III_2-p151
      *> rules: [1985-GR-III.2-p151]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p151 ((section preamble)). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5506 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p151: a key word is required when its format is
      * used. MOVE without its key word TO is refused.
      * Control: p151_key_word_control.cob (MOVE A TO B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P151R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X VALUE "A".
       01  B PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE A B.
           DISPLAY "P151R ACCEPTED " B.
           STOP RUN.
