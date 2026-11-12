      *>--- conformance-case
      *> id: 1985-GR-XII_2_4-5
      *> rules: [1985-GR-XII.2.4-5]
      *> accept: [85]
      *> files: [LIBA.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.4 GR5 (THE COPY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected); run with its library texts
      *> (drafts-85n, kb/Work PB5067) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CPYTST.
      * LIBRARY TEXT LIBA (REFERENCE FORMAT) HOLDS THIS ONE LINE:
      *        DISPLAY "ONE", "TWO" "ONE".
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           COPY LIBA REPLACING =="ONE" "TWO"== BY =="AB"==
               "ONE" BY "CD".
           STOP RUN.
