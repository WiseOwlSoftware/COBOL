      *>--- conformance-case
      *> id: pb5924-continuation-literal-area-a-85
      *> rules: [1985-SR-IV.7.2.2-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.2.2 (cite.py: OK):
      *> "Area A of a continuation line must be blank"
      *> A continuation line whose quotation mark stands in area A is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONTAL.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ
      - "END".
           STOP RUN.
