      *>--- conformance-case
      *> id: 1985-SR-VII_3_4_3-2
      *> rules: [1985-SR-VII.3.4.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.3 SR2 (THE CODE-SET CLAUSE). A refusal: the rule
      *> forbids line 14 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1672 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CSETT.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SPECIAL-NAMES.
000600     ALPHABET A1 IS "A" THRU "Z".
000700 INPUT-OUTPUT SECTION.
000800 FILE-CONTROL.
000900     SELECT F1 ASSIGN TO OUTFILE.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  F1
001300     LABEL RECORDS ARE STANDARD
001400     CODE-SET IS A1.
001500 01  R1 PIC X(10).
001600 PROCEDURE DIVISION.
001700 P1.
001800     OPEN OUTPUT F1.
001900     CLOSE F1.
002000     STOP RUN.
