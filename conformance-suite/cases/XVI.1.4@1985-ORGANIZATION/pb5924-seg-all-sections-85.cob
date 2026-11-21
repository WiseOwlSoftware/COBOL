      *>--- conformance-case
      *> id: pb5924-seg-all-sections-85
      *> rules: [1985-SR-XVI.1.4.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.1.4.1 (cite.py: OK): "when segmentation is used, the entire Procedure Division must be in sections"
      *> A section with a segment-number and a paragraph outside every section: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGNS.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "P0".
       S1 SECTION 50.
       P1.
           DISPLAY "P1".
           STOP RUN.
