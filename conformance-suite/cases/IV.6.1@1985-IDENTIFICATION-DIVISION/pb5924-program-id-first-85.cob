      *>--- conformance-case
      *> id: pb5924-program-id-first-85
      *> rules: [1985-SR-IV.6.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.6.1.2 (cite.py: OK): "The name of the program must be given in the first paragraph"
      *> AUTHOR precedes PROGRAM-ID; the program name is not in the first paragraph: refused.
       IDENTIFICATION DIVISION.
       AUTHOR. SOMEONE.
       PROGRAM-ID. PIDFIRST.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "X".
           STOP RUN.
