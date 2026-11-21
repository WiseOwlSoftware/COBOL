      *>--- conformance-case
      *> id: pb5924-proc-div-using-ok-85
      *> rules: [1985-SR-IV.6.4.1.4.1-p1, 1985-SR-X.5.1-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.5.1 (cite.py: OK): "Data-name-1 must be defined as a level 01 entry or a level 77 entry in the Linkage Section"
      *> PROCEDURE DIVISION USING an 01 and a 77 Linkage item, the 01 the object of a REDEFINES elsewhere
      *> in the Linkage Section: legal; prints the parameters and the redefinition's second half.
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
       01  LK-R REDEFINES LK-A.
           05  LK-R1 PIC X(2).
           05  LK-R2 PIC X(2).
       77  LK-B PIC X(4).
       PROCEDURE DIVISION USING LK-A LK-B.
       P2.
           DISPLAY LK-A " " LK-R2 " " LK-B.
           EXIT PROGRAM.
       END PROGRAM SUBP.
