      *>--- conformance-case
      *> id: 1985-GR-III_2-p80
      *> rules: [1985-GR-III.2-p80]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p80 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVBODY.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
      * III.2 p80: a division body of zero sections or paragraphs
      * (Environment and Data above); four divisions in the program.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DIV=OK".
           STOP RUN.
