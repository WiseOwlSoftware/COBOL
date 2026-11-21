      *>--- conformance-case
      *> id: pb5924-optional-sequence-legal-85
      *> rules: [1985-SR-IV.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.2.1 (cite.py: OK): "Clauses that are optional must appear"
      *> The optional UPON and WITH NO ADVANCING phrases of DISPLAY in
      *> the printed sequence: accepted, A then B on one line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQ3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS CON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A" UPON CON WITH NO ADVANCING.
           DISPLAY "B".
           STOP RUN.
