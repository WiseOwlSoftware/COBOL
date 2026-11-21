      *>--- conformance-case
      *> id: 1985-SR-IV_7_4-p4
      *> rules: [1985-SR-IV.7.4-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.4 p4 (REFERENCE FORMAT). A refusal: the rule forbids
      *> line 14 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL3734 there; a compiler that accepts it fails. Placed from
      *> written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-IV.7.4-p4 refusal: FD in area B (column 12).
      * CONTROL: t-level-indicators-85.cob, the one line moved.
      *
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
