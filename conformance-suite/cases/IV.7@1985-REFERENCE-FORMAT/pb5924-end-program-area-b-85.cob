      *>--- conformance-case
      *> id: pb5924-end-program-area-b-85
      *> rules: [1985-SR-IV.7.6-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.6 (cite.py: OK):
      *> "The end program header must start in area A"
      *> END PROGRAM beginning in area B is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDB.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "ENDB".
           STOP RUN.
           END PROGRAM ENDB.
