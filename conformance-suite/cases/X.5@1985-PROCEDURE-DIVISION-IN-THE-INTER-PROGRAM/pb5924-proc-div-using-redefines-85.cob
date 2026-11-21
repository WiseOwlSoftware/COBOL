      *>--- conformance-case
      *> id: pb5924-proc-div-using-redefines-85
      *> rules: [1985-SR-X.5.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.5.1 (cite.py: OK): "The data description entry for data-name-1 must not contain a REDEFINES clause"
      *> data-name-1 whose entry contains a REDEFINES clause: refused.
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
       01  LK-B REDEFINES LK-A PIC X(4).
       PROCEDURE DIVISION USING LK-A LK-B.
       P2.
           DISPLAY LK-A.
           EXIT PROGRAM.
       END PROGRAM SUBP.
