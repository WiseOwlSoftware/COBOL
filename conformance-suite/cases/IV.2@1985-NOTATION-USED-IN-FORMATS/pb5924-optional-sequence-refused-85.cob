      *>--- conformance-case
      *> id: pb5924-optional-sequence-refused-85
      *> rules: [1985-SR-IV.2.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1 (cite.py: OK): "Clauses that are optional must appear"
      *> WITH NO ADVANCING written before UPON, against the DISPLAY
      *> format's sequence: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQ4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS CON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A" WITH NO ADVANCING UPON CON.
           DISPLAY "B".
           STOP RUN.
