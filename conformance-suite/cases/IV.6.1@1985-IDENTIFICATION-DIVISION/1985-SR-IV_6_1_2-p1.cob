      *>--- conformance-case
      *> id: 1985-SR-IV_6_1_2-p1
      *> rules: [1985-SR-IV.6.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.1.2 p1 (IDENTIFICATION DIVISION). A refusal: the
      *> rule forbids line 7 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL2987 there; a compiler that accepts it
      *> fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-06.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.6.1.2-p1 REFUSAL: DATE-WRITTEN precedes AUTHOR,
      * against the order of presentation of the format.
      * Control: p05 (AUTHOR before DATE-WRITTEN).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDBAD.
       DATE-WRITTEN. 1985-01-01.
       AUTHOR. A WRITER.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "ID OK".
           STOP RUN.
