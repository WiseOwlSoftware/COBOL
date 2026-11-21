      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-L2_14
      *> rules: [1985-SR-IV.4.2.2.1.1-L2.14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 SRL2.14 (COBOL Words). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2692 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe ds14_symbolic_data:
      * the same word in two disjoint sets is refused at 85
      * (§1985:IV.4.2.2.1.1 p4: "can belong to one and only one
      * of these disjoint sets"; "must be unique").
      * CONTROL: udw_types.cob, udw_types2.cob, udw_cd.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DS14-SYMBOLIC-DATA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS ZQ IS 66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X VALUE "Z".
       PROCEDURE DIVISION.
       P0.
           DISPLAY "NO".
           STOP RUN.
