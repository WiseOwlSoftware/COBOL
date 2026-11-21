      *>--- conformance-case
      *> id: 1985-GR-IV_7_4-p3
      *> rules: [1985-GR-IV.7.4-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.4 p3 (REFERENCE FORMAT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.7.4-p3 / SR-IV.7.4-p4: entries that begin with the
      * level indicators FD, SD, CD, RD, each in area A followed by a
      * space and the subject name. Expect: FD SD CD RD
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVLIND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "LVLINDF".
           SELECT S ASSIGN TO "LVLINDS".
           SELECT P ASSIGN TO "LVLINDP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(2).
       SD S.
       01 SR PIC X(2).
       FD P
           REPORT IS RP.
       WORKING-STORAGE SECTION.
       01 W PIC X(2) VALUE "SD".
       COMMUNICATION SECTION.
       CD CI FOR INPUT.
       01 CI-R.
           02 CI-Q PIC X(2) VALUE "CD".
           02 FILLER PIC X(85).
       REPORT SECTION.
       RD RP.
       01 DL TYPE DETAIL LINE PLUS 1.
           02 COLUMN 1 PIC X(2) VALUE "RD".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "FD " W " " CI-Q " RD".
           STOP RUN.
