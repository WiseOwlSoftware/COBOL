      *>--- conformance-case
      *> id: pb5924-proc-div-no-header-85
      *> rules: [1985-SR-IV.6.4.1.4.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.5.1 (cite.py: OK): "The Procedure Division is identified by, and must begin with, the following header"
      *> A Procedure Division with no PROCEDURE DIVISION header: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOHDR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A PIC X(4) VALUE "ABCD".
       P1.
           DISPLAY W-A.
           STOP RUN.
