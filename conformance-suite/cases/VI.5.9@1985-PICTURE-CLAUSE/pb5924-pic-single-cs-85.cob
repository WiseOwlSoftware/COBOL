      *>--- conformance-case
      *> id: pb5924-pic-single-cs-85
      *> rules: [1985-SR-VI.5.9.6-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.9.6 (cite.py: OK): "at least two
      *> occurrences of one of the symbols '+', '-', or 'cs' must be
      *> present"
      *> PIC $B holds none of A X Z 9 * and only one currency symbol:
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59249.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E PIC $B.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "X".
           STOP RUN.
