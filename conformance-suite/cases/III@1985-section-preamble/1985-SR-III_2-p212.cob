      *>--- conformance-case
      *> id: 1985-SR-III_2-p212
      *> rules: [1985-SR-III.2-p212]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p212 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P212PARA.
       AUTHOR.
       INSTALLATION.
       DATE-WRITTEN.
       DATE-COMPILED.
       SECURITY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER.
       OBJECT-COMPUTER.
       SPECIAL-NAMES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-START.
           GO TO P-EMPTY.
       P-EMPTY.
       P-ONE.
           ADD 1 TO N.
       P-TWO.
           ADD 1 TO N. ADD 1 TO N.
       P-END.
           IF N = 3
               DISPLAY "P212 OK"
           ELSE
               DISPLAY "P212 FAIL " N.
           STOP RUN.
