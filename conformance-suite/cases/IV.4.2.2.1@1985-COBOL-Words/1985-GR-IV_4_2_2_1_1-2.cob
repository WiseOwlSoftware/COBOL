      *>--- conformance-case
      *> id: 1985-GR-IV_4_2_2_1_1-2
      *> rules: [1985-GR-IV.4.2.2.1.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 GR2 (COBOL Words). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-02.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-02 probe: 1985-GR-IV.4.2.2.1.1-2 cd-name CD-21 as a
      * user-defined word (§1985:IV.4.2.2.1.1 GR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDW-CD.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CD-21 FOR INPUT
           SYMBOLIC QUEUE IS Q-22
           MESSAGE COUNT IS CNT-23.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "CD-OK".
           STOP RUN.
