      *>--- conformance-case
      *> id: pb5924-linkage-77-subdivided-85
      *> rules: [1985-SR-X.4.1.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.4.1.2 (cite.py: OK): "must be grouped into records according to the rules for formation of record descriptions"
      *> Hierarchically related Linkage items not grouped into a record (a 05 under a 77): refused.
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
       77  LK-B PIC X(4).
           05  LK-B1 PIC X(2).
       PROCEDURE DIVISION USING LK-A LK-B.
       P2.
           DISPLAY LK-A.
           EXIT PROGRAM.
       END PROGRAM SUBP.
