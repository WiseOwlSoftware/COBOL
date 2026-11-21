      *>--- conformance-case
      *> id: pb5924-procedure-section-header-area-b-85
      *> rules: [1985-SR-IV.7.3.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.3.2 (cite.py: OK):
      *> "The section header must start in area A"
      *> A procedure section header beginning in area B is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PSECB.
       PROCEDURE DIVISION.
           S1 SECTION.
       P1.
           DISPLAY "PSECB".
           STOP RUN.
