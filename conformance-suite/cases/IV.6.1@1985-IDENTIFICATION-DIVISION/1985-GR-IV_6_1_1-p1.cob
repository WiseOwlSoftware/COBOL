      *>--- conformance-case
      *> id: 1985-GR-IV_6_1_1-p1
      *> rules: [1985-GR-IV.6.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.1.1 p1 (IDENTIFICATION DIVISION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-IV.6.1.1-p1 / 1985-SR-IV.6.1.2-p1: PROGRAM-ID first,
      * then the optional paragraphs in the order of the format.
      * Expect: ID OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDPARAS.
       AUTHOR. A WRITER.
       INSTALLATION. A SHOP.
       DATE-WRITTEN. 1985-01-01.
       DATE-COMPILED. ANY TEXT.
       SECURITY. NONE.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "ID OK".
           STOP RUN.
