      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-L2_5
      *> rules: [1985-SR-IV.4.2.2.1.1-L2.5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 SRL2.5 (COBOL Words). A refusal: the rule
      *> forbids line 17 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2692 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe ds05_file_data:
      * the same word in two disjoint sets is refused at 85
      * (§1985:IV.4.2.2.1.1 p4: "can belong to one and only one
      * of these disjoint sets"; "must be unique").
      * CONTROL: udw_types.cob, udw_types2.cob, udw_cd.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DS05-FILE-DATA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT ZQ ASSIGN TO "DS05.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  ZQ.
       01  R1 PIC X.
       WORKING-STORAGE SECTION.
       01  ZQ PIC X.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "NO".
           STOP RUN.
