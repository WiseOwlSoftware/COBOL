      *>--- conformance-case
      *> id: pb5924-index-name-unqualified-85
      *> rules: [1985-GR-X.1.3.8.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.3 (cite.py: OK): "Index-names cannot be qualified"
      *> The unqualified index-name subscripts a qualified table reference: legal, prints the second element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  TBL OCCURS 3 TIMES INDEXED BY IX PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO GRP.
           SET IX TO 2.
           DISPLAY TBL OF GRP (IX).
           STOP RUN.
