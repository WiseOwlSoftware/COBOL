      *>--- conformance-case
      *> id: 1985-SR-VIII_4_6_3-1
      *> rules: [1985-SR-VIII.4.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.3 SR1 (THE REWRITE STATEMENT). A refusal: the
      *> rule forbids line 19 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL6827 there; a compiler that accepts it
      *> fails. Placed from written and run by the 1985 audit's drafts
      *> pass (a draft that mismatched on an older build, re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RWTEST.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT REL-FILE ASSIGN TO DISK1
000700         ORGANIZATION IS RELATIVE
000800         ACCESS MODE IS SEQUENTIAL.
000900 DATA DIVISION.
001000 FILE SECTION.
001100 FD  REL-FILE LABEL RECORDS ARE STANDARD.
001200 01  REL-REC PIC X(10).
001300 WORKING-STORAGE SECTION.
001400 01  WS-REC PIC X(10).
001500 PROCEDURE DIVISION.
001600 MAIN-PARA.
001700     OPEN I-O REL-FILE.
001800     READ REL-FILE AT END GO TO DONE-PARA.
001900     REWRITE REL-REC FROM REL-REC.
002000 DONE-PARA.
002100     CLOSE REL-FILE.
002200     STOP RUN.
