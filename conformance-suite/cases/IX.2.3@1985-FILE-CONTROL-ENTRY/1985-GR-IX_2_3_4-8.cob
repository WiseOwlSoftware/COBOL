      *>--- conformance-case
      *> id: 1985-GR-IX_2_3_4-8
      *> rules: [1985-GR-IX.2.3.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR8 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. IXGR8.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT IX-FILE ASSIGN TO "IXGR8"
000700         ORGANIZATION IS INDEXED
000800         ACCESS MODE IS SEQUENTIAL
000900         RECORD KEY IS IX-KEY.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  IX-FILE
001300     LABEL RECORDS ARE STANDARD.
001400 01  IX-REC.
001500     05  IX-KEY  PIC X(4).
001600     05  IX-DATA PIC X(5).
001700 PROCEDURE DIVISION.
001800 MAIN-PARA.
001900     OPEN OUTPUT IX-FILE.
002000     MOVE "0001" TO IX-KEY.
002100     MOVE "ALPHA" TO IX-DATA.
002200     WRITE IX-REC
002300         INVALID KEY DISPLAY "WRITE FAILED".
002400     CLOSE IX-FILE.
002500     OPEN INPUT IX-FILE.
002600     READ IX-FILE
002700         AT END DISPLAY "NO RECORD".
002800     DISPLAY IX-REC.
002900     CLOSE IX-FILE.
003000     STOP RUN.
