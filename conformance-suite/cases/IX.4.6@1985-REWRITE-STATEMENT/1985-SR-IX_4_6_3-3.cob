      *>--- conformance-case
      *> id: 1985-SR-IX_4_6_3-3
      *> rules: [1985-SR-IX.4.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.3 SR3 (THE REWRITE STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT F ASSIGN TO IXFILE
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS RANDOM
000900         RECORD KEY IS R-KEY.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  F LABEL RECORDS ARE STANDARD.
001300 01  R.
001400     05 R-KEY  PIC X(4).
001500     05 R-DATA PIC X(10).
001600 PROCEDURE DIVISION.
001700 P1.
001800     OPEN I-O F.
001900     MOVE "0001" TO R-KEY.
002000     REWRITE R.
002100     CLOSE F.
002200     STOP RUN.
