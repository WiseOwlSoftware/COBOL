      *>--- conformance-case
      *> id: 1985-SR-IV_7_3_3-p2
      *> rules: [1985-SR-IV.7.3.3-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.3.3 p2 (DIVISION, SECTION, PARAGRAPH FORMATS). A
      *> refusal: the rule forbids line 8 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL3734 there; a compiler that
      *> accepts it fails. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.7.3.3-p2 refusal: paragraph-name in area B.
      * CONTROL: t-paragraph-forms-85.cob (P2. in column 8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARARB.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "X".
            P2.
           STOP RUN.
