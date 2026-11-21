      *>--- conformance-case
      *> id: pb5924-identification-header-area-b-85
      *> rules: [1985-SR-IV.7.3.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.3.1 (cite.py: OK):
      *> "The division header must start in area A"
      *> IDENTIFICATION DIVISION beginning in column 12 (area B) is refused.
           IDENTIFICATION DIVISION.
       PROGRAM-ID. IDB.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
