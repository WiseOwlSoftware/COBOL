      *>--- conformance-case
      *> id: pb5924-proc-div-using-duplicate-85
      *> rules: [1985-SR-X.5.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.5.1 (cite.py: OK): "A particular user-defined word may not appear more than once as data-name-1"
      *> The same data-name twice as data-name-1: refused.
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
       PROCEDURE DIVISION USING LK-A LK-A.
       P2.
           DISPLAY LK-A.
           EXIT PROGRAM.
       END PROGRAM SUBP.
