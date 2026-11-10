      *>--- conformance-case
      *> id: 1985-FMT-VII_3_4_2
      *> rules: [1985-FMT-VII.3.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.2 (THE CODE-SET CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CSET1.
000300 ENVIRONMENT DIVISION.
000400 CONFIGURATION SECTION.
000500 SPECIAL-NAMES.
000600     ALPHABET ASC IS STANDARD-1.
000700 INPUT-OUTPUT SECTION.
000800 FILE-CONTROL.
000900     SELECT F1 ASSIGN TO DISK1
001000         ORGANIZATION IS SEQUENTIAL.
001100 DATA DIVISION.
001200 FILE SECTION.
001300 FD  F1
001400     LABEL RECORDS ARE STANDARD
001500     CODE-SET IS ASC.
001600 01  F1-REC PIC X(5).
001700 PROCEDURE DIVISION.
001800 P1.
001900     OPEN OUTPUT F1.
002000     MOVE "HELLO" TO F1-REC.
002100     WRITE F1-REC.
002200     CLOSE F1.
002300     OPEN INPUT F1.
002400     READ F1 AT END DISPLAY "EOF".
002500     DISPLAY F1-REC.
002600     CLOSE F1.
002700     STOP RUN.
