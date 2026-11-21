      *>--- conformance-case
      *> id: pb5924-rel-usage-index-alnum-85
      *> rules: [1985-SR-VI.6.3.1.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.1 (cite.py: OK): "for all other
      *> comparisons, the operands must have the same usage"
      *> An index data item (usage INDEX) compared with an alphanumeric
      *> item (usage DISPLAY): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  IX INDEX.
       01  A4 PIC X(4) VALUE SPACES.
       PROCEDURE DIVISION.
       P0.
           IF IX = A4 DISPLAY "Y" ELSE DISPLAY "N".
           STOP RUN.
