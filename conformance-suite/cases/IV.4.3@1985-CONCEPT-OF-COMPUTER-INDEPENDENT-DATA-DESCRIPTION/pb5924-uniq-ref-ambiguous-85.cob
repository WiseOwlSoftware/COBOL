      *>--- conformance-case
      *> id: pb5924-uniq-ref-ambiguous-85
      *> rules: [1985-SR-IV.4.3.8-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8 (cite.py: OK): "must contain a reference which uniquely identifies that resource"
      *> X names an item of G1 and of G2; MOVE 1 TO X identifies no one resource: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNIQAMB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  X  PIC 9.
       01  G2.
           05  X  PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO X.
           STOP RUN.
