      *>--- conformance-case
      *> id: 1985-SR-VI_6_3_2_3-T1_3
      *> rules: [1985-SR-VI.6.3.2.3-T1.3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.2.3 Table 1 row 3 (Complex Conditions). A refusal:
      *> the rule forbids line 13 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL0001 there; a compiler that accepts
      *> it fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-11.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-11 REFUSAL probe (PB5453), VI.6.3.2.3 Table 1:
      * NOT followed by NOT (Table 1 row 3).
      * Control: t1-control.cob (only the IF line differs).
      * Expected: refused at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T1CTRL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X       PIC 9 VALUE 1.
       01  Y       PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NOT NOT X = 1
               DISPLAY "OK"
           END-IF.
           STOP RUN.
