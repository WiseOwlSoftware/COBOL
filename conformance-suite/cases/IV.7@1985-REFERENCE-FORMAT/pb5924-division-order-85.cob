      *>--- conformance-case
      *> id: pb5924-division-order-85
      *> rules: [1985-SR-IV.7.1-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.1 (cite.py: OK): "The divisions of a COBOL source program must be ordered as follows"
      *> The Data Division precedes the Environment Division: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVORDER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W  PIC 9 VALUE 1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       PROCEDURE DIVISION.
       P1.
           DISPLAY W.
           STOP RUN.
