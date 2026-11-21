      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1_1-p3
      *> rules: [1985-SR-IV.4.2.2.1.1-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 p3 (COBOL Words). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-SR-IV.4.2.2.1.1-p3 the disjoint sets are per
      * program "excluding any contained program": a contained program
      * may reuse as another type a word its container uses
      * (§1985:IV.4.2.2.1.1 p3). CONTROL: ds04_data_para.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ZQ                   PIC X VALUE "O".
       PROCEDURE DIVISION.
       P0.
           DISPLAY ZQ.
           CALL "INNER1".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P0                   PIC X VALUE "I".
       PROCEDURE DIVISION.
       ZQ.
           DISPLAY P0.
           EXIT PROGRAM.
       END PROGRAM INNER1.
       END PROGRAM OUTER1.
