      *>--- conformance-case
      *> id: 1985-GR-III_2-p229
      *> rules: [1985-GR-III.2-p229]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p229 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-04.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P229PSTX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           REPLACE ==BUMP   THE
      * A COMMENT LINE INSIDE PSEUDO-TEXT
                     COUNT== BY ==ADD 1 TO N
      * ANOTHER COMMENT LINE INSIDE PSEUDO-TEXT
                     == ==GONE== BY ====.
       P-MAIN.
           BUMP THE COUNT.
           BUMP
           THE COUNT.
           GONE
           IF N = 2
               DISPLAY "P229 OK"
           ELSE
               DISPLAY "P229 FAIL " N.
           STOP RUN.
