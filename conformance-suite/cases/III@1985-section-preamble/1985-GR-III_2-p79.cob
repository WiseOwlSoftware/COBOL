      *>--- conformance-case
      *> id: 1985-GR-III_2-p79
      *> rules: [1985-GR-III.2-p79]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p79 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIGPOS.
      * III.2 p79: with usage DISPLAY a digit position is a character
      * position: a 3-digit DISPLAY item spans 3 characters.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  N           PIC 999 USAGE DISPLAY VALUE 123.
           05  T           PIC X VALUE "Z".
       01  R REDEFINES G   PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "R=" R.
           STOP RUN.
