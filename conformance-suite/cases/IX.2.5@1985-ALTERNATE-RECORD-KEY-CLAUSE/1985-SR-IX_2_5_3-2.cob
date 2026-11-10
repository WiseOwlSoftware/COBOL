      *>--- conformance-case
      *> id: 1985-SR-IX_2_5_3-2
      *> rules: [1985-SR-IX.2.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.3 SR2 (THE ALTERNATE RECORD KEY CLAUSE). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL0863 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. ALTKEY.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IDX-FILE ASSIGN TO "IDXF"
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS DYNAMIC
000900         RECORD KEY IS REC-KEY
001000         ALTERNATE RECORD KEY IS ALT-KEY.
001100 DATA DIVISION.
001200 FILE SECTION.
001300 FD  IDX-FILE.
001400 01  IDX-REC.
001500     05  REC-KEY  PIC X(5).
001600     05  ALT-KEY  PIC 9(5).
001700     05  REC-DATA PIC X(10).
001800 PROCEDURE DIVISION.
001900 MAIN-PARA.
002000     STOP RUN.
