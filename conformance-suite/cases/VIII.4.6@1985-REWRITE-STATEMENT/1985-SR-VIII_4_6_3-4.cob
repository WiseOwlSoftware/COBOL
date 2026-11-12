      *>--- conformance-case
      *> id: 1985-SR-VIII_4_6_3-4
      *> rules: [1985-SR-VIII.4.6.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.3 SR4 (THE REWRITE STATEMENT). A refusal: the
      *> rule forbids line 21 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6826 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT REL-FILE ASSIGN TO RELFILE
000700         ORGANIZATION IS RELATIVE
000800         ACCESS MODE IS RANDOM
000900         RELATIVE KEY IS REL-KEY.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  REL-FILE LABEL RECORDS ARE STANDARD.
001300 01  REL-REC PIC X(10).
001400 WORKING-STORAGE SECTION.
001500 01  REL-KEY PIC 9(4).
001600 PROCEDURE DIVISION.
001700 MAIN-PARA.
001800     OPEN I-O REL-FILE.
001900     MOVE 1 TO REL-KEY.
002000     MOVE "ABC" TO REL-REC.
002100     REWRITE REL-REC.
002200     CLOSE REL-FILE.
002300     STOP RUN.
