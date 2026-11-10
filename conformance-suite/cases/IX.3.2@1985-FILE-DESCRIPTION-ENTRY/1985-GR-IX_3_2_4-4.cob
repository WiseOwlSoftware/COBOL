      *>--- conformance-case
      *> id: 1985-GR-IX_3_2_4-4
      *> rules: [1985-GR-IX.3.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.3.2.4 GR4 (THE FILE DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. LBLIDX.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IDX-FILE ASSIGN TO IDXFILE
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS SEQUENTIAL
000900         RECORD KEY IS IDX-KEY.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  IDX-FILE
001300     LABEL RECORDS ARE STANDARD.
001400 01  IDX-REC.
001500     05 IDX-KEY  PIC X(4).
001600     05 IDX-DATA PIC X(10).
001700 PROCEDURE DIVISION.
001800 MAIN-PARA.
001900     OPEN OUTPUT IDX-FILE.
002000     MOVE "0001" TO IDX-KEY.
002100     MOVE "LABEL TEST" TO IDX-DATA.
002200     WRITE IDX-REC INVALID KEY DISPLAY "WRITE FAILED".
002300     CLOSE IDX-FILE.
002400     DISPLAY "LABEL RECORDS OK".
002500     STOP RUN.
