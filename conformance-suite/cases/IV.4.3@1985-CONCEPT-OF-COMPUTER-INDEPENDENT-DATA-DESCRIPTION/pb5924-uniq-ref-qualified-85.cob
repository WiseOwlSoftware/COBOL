      *>--- conformance-case
      *> id: pb5924-uniq-ref-qualified-85
      *> rules: [1985-SR-IV.4.3.8-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8 (cite.py: OK): "must contain a reference which uniquely identifies that resource"
      *> Each reference to the two X items is made unique by qualification; each identifies its own item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNIQQUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  X  PIC 9.
       01  G2.
           05  X  PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO X OF G1.
           MOVE 2 TO X IN G2.
           DISPLAY "G1=" X OF G1 " G2=" X IN G2.
           STOP RUN.
