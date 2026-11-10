      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_2_1-2
      *> rules: [1985-GR-IV.4.3.2.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.2.1 GR2 (Concept of Levels). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVL77.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  ITEM-A PIC X(3) VALUE "ONE".
       77  ITEM-B PIC 9(2) VALUE 42.
       PROCEDURE DIVISION.
       P1.
           DISPLAY ITEM-A.
           DISPLAY ITEM-B.
           STOP RUN.
