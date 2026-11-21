      *>--- conformance-case
      *> id: pb5924-index-name-qualified-subscript-85
      *> rules: [1985-GR-X.1.3.8.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.3 (cite.py: OK): "Index-names cannot be qualified"
      *> TBL (IX IN GRP): a qualified index-name used as a subscript is refused at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXQUAL2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  TBL OCCURS 3 TIMES INDEXED BY IX PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO GRP.
           SET IX TO 2.
           DISPLAY TBL (IX IN GRP).
           STOP RUN.
