      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-L2_13
      *> rules: [1985-SR-IV.4.2.2.1.1-L2.13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 SRL2.13 (COBOL Words). A refusal: the rule
      *> forbids line 12 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2692 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe ds13_section_data:
      * the same word in two disjoint sets is refused at 85
      * (§1985:IV.4.2.2.1.1 p4: "can belong to one and only one
      * of these disjoint sets"; "must be unique").
      * CONTROL: udw_types.cob, udw_types2.cob, udw_cd.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DS13-SECTION-DATA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X VALUE "Z".
       PROCEDURE DIVISION.
       ZQ SECTION.
       P0.
           DISPLAY "NO".
           STOP RUN.
