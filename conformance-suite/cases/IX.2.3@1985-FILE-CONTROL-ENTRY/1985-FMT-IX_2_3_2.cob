      *>--- conformance-case
      *> id: 1985-FMT-IX_2_3_2
      *> rules: [1985-FMT-IX.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.2 (THE FILE CONTROL ENTRY). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL5701 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000010 IDENTIFICATION DIVISION.
000020 PROGRAM-ID. IXFC.
000030 ENVIRONMENT DIVISION.
000040 INPUT-OUTPUT SECTION.
000050 FILE-CONTROL.
000060     SELECT IX-FILE ASSIGN TO "IXFILE" INDEXED.
000070 DATA DIVISION.
000080 FILE SECTION.
000090 FD  IX-FILE LABEL RECORDS ARE STANDARD.
000100 01  IX-REC.
000110     05 IX-KEY  PIC X(8).
000120     05 IX-DATA PIC X(20).
000130 PROCEDURE DIVISION.
000140 MAIN-PARA.
000150     DISPLAY "OK".
000160     STOP RUN.
