      *>--- conformance-case
      *> id: 1985-SR-IX_2_7_3-2
      *> rules: [1985-SR-IX.2.7.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.7.3 SR2 (THE RECORD KEY CLAUSE). A refusal: the rule
      *> forbids line 9 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0863 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RKTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IFILE ASSIGN TO DISK1
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS SEQUENTIAL
000900         RECORD KEY IS REC-KEY.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  IFILE
001300     LABEL RECORDS ARE STANDARD.
001400 01  IREC.
001500     05  REC-KEY  PIC 9(5).
001600     05  REC-DATA PIC X(20).
001700 PROCEDURE DIVISION.
001800 MAIN-PARA.
001900     DISPLAY "DONE".
002000     STOP RUN.
