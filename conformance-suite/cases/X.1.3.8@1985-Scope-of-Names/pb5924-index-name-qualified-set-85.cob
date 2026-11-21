      *>--- conformance-case
      *> id: pb5924-index-name-qualified-set-85
      *> rules: [1985-GR-X.1.3.8.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.3 (cite.py: OK): "Index-names cannot be qualified"
      *> SET IX OF TBL: a qualified index-name is refused at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXQUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  TBL OCCURS 3 TIMES INDEXED BY IX PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO GRP.
           SET IX OF TBL TO 2.
           DISPLAY TBL (IX).
           STOP RUN.
