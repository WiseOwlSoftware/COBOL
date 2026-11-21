      *>--- conformance-case
      *> id: pb5924-division-header-column-11-85
      *> rules: [1985-SR-IV.7.3.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.3.1 (cite.py: OK):
      *> "The division header must start in area A"
      *> PROCEDURE DIVISION beginning in column 11, the last of area A, compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIV11.
          PROCEDURE DIVISION.
       P1.
           DISPLAY "DIV11".
           STOP RUN.
