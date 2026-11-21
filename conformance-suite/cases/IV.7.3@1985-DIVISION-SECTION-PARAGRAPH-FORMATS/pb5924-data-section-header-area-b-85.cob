      *>--- conformance-case
      *> id: pb5924-data-section-header-area-b-85
      *> rules: [1985-SR-IV.7.3.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.3.2 (cite.py: OK):
      *> "The section header must start in area A"
      *> WORKING-STORAGE SECTION beginning in area B is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SECB.
       DATA DIVISION.
           WORKING-STORAGE SECTION.
       01 X PIC X VALUE "S".
       PROCEDURE DIVISION.
       P1.
           DISPLAY X.
           STOP RUN.
