      *>--- conformance-case
      *> id: 1985-SR-VI_5_7_3-2
      *> rules: [1985-SR-VI.5.7.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.7.3 SR2 (LEVEL-NUMBER). A refusal: the rule forbids
      *> line 11 of the program below, and WiseOwl refuses it at 85 with
      *> COBOL1746 there; a compiler that accepts it fails. Placed from
      *> the audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LVLTST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT F1 ASSIGN TO DISK1.
000700 DATA DIVISION.
000800 FILE SECTION.
000900 FD  F1 LABEL RECORDS ARE STANDARD.
001000 01  R1 PIC X(10).
001100 77  R2 PIC X(10).
001200 PROCEDURE DIVISION.
001300 P1.
001400     DISPLAY "OK".
001500     STOP RUN.
