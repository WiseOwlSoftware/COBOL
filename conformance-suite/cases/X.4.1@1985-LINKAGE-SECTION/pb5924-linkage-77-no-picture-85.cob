      *>--- conformance-case
      *> id: pb5924-linkage-77-no-picture-85
      *> rules: [1985-SR-X.4.1.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.4.1.1 (cite.py: OK): "The following data clauses are required in each data description entry"
      *> A level-77 Linkage item with neither a PICTURE clause nor USAGE IS INDEX is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A PIC X(4) VALUE "ABCD".
       01  W-B PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBP" USING W-A W-B.
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-Y PIC X(4) VALUE "SSSS".
       LINKAGE SECTION.
       77  LK-A PIC X(4).
       77  LK-B.
       PROCEDURE DIVISION USING LK-A LK-B.
       P2.
           DISPLAY LK-A.
           EXIT PROGRAM.
       END PROGRAM SUBP.
