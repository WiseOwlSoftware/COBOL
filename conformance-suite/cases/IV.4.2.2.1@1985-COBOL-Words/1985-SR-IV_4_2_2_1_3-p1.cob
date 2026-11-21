      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_3-p1
      *> rules: [1985-SR-IV.4.2.2.1.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.3 p1 (COBOL Words). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL7654 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.4.2.2.1.3-p1: a reserved word must not appear as a
      * system-name (computer-name). Control: p07_resv_control.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P07RESV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. MERGE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN".
           STOP RUN.
