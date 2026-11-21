      *>--- conformance-case
      *> id: pb5924-paragraph-outside-section-85
      *> rules: [1985-SR-IV.6.4.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.1.2 (cite.py: OK): "If one paragraph is in a section, all paragraphs must be in sections"
      *> P0 precedes the first section header while P1 is in section S1: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARNOSEC.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "A".
       S1 SECTION.
       P1.
           DISPLAY "B".
           STOP RUN.
