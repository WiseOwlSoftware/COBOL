      *>--- conformance-case
      *> id: 1985-SR-IV_7_3_3-p1
      *> rules: [1985-SR-IV.7.3.3-p1, 1985-SR-IV.7.3.3-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.3.3 p1; §1985:IV.7.3.3 p3 (DIVISION, SECTION,
      *> PARAGRAPH FORMATS). An output test: the program must translate
      *> and run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.7.3.3-p1/p3: a paragraph is a paragraph-name, a
      * period and zero or more sentences, or a paragraph header and
      * entries; the first sentence or entry on the header's line or
      * in area B of the next line that is neither blank nor comment.
      * Expect: AB C D
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARFRM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL.
       OBJECT-COMPUTER.

      * a comment line before the entry
           WISEOWL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "A".
       PROCEDURE DIVISION.
       P0.
       P1. DISPLAY W WITH NO ADVANCING. DISPLAY "B" WITH NO ADVANCING.
       P2.

      * a comment line before the first sentence
           DISPLAY " C" WITH NO ADVANCING.
                                       DISPLAY " D".
       P3.
           STOP RUN.
