      *>--- conformance-case
      *> id: pb5924-pd-header-missing-85
      *> rules: [1985-SR-IV.6.4.1.4.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.4.1.4.1 (cite.py: OK): "The Procedure Division is identified by, and must begin with, the following header"
      *> Procedures follow the Data Division with no PROCEDURE DIVISION header: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PDNOHDR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W  PIC 9 VALUE 1.
       P1.
           DISPLAY "X".
           STOP RUN.
