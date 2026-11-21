      *>--- conformance-case
      *> id: pb5924-proc-div-using-level-05-85
      *> rules: [1985-SR-X.5.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.5.1 (cite.py: OK): "Data-name-1 must be defined as a level 01 entry or a level 77 entry in the Linkage Section"
      *> data-name-1 a level-05 Linkage item: refused.
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
       01  LK-A PIC X(4).
       01  LK-G.
           05  LK-G1 PIC X(4).
       PROCEDURE DIVISION USING LK-A LK-G1.
       P2.
           DISPLAY LK-A.
           EXIT PROGRAM.
       END PROGRAM SUBP.
