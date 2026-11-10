      *>--- conformance-case
      *> id: 1985-SR-VII_2_12_3-1
      *> rules: [1985-SR-VII.2.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.3 SR1 (THE RERUN CLAUSE). A refusal: the rule
      *> forbids line 11 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6529 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RERUNT.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT DATF ASSIGN TO DATAFILE.
000700     SELECT CHKF ASSIGN TO CHKFILE.
000800     SELECT RELF ASSIGN TO RELFILE
000900         ORGANIZATION IS RELATIVE.
001000 I-O-CONTROL.
001100     RERUN ON RELF EVERY END OF REEL OF DATF.
001200 DATA DIVISION.
001300 FILE SECTION.
001400 FD  DATF LABEL RECORDS ARE STANDARD.
001500 01  DAT-REC PIC X(80).
001600 FD  CHKF LABEL RECORDS ARE STANDARD.
001700 01  CHK-REC PIC X(80).
001800 FD  RELF LABEL RECORDS ARE STANDARD.
001900 01  REL-REC PIC X(80).
002000 PROCEDURE DIVISION.
002100 MAIN-PARA.
002200     STOP RUN.
