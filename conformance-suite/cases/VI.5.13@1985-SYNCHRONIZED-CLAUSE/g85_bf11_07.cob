      *>--- conformance-case
      *> id: g85_bf11_07
      *> rules: [1985-GR-VI.5.13.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13.4 GR5 - "Whenever a SYNCHRONIZED item is referenced
      *> in the source program, the original size of the item, as shown
      *> in the PICTURE clause, the USAGE clause, and the SIGN clause, is
      *> used in determining any action that depends on size, such as
      *> justification, truncation, or overflow." Each SYNC item follows
      *> a one-character item, so alignment slack may exist around it;
      *> none of it may enter a size-dependent action.
      *>   justification: SJ PIC X(4) JUSTIFIED RIGHT SYNC, MOVE "AB"
      *>     = [  AB] (4 positions, VI.5.6.4 GR1).
      *>   truncation: SX PIC X(3) SYNC, MOVE "ABCDE" = [ABC]; SB PIC
      *>     9(3) SYNC, MOVE 12345 = 345 (high-order truncation
      *>     at the PICTURE's 3 digits, VI.6.19.4 GR4b); SS PIC S9(3)
      *>     SIGN LEADING SEPARATE SYNC RIGHT, MOVE -12345 = [-345].
      *>   overflow: SB = 999, ADD 1 ON SIZE ERROR: the result 1000
      *>     exceeds 3 digits, so the size error branch runs and SB
      *>     keeps 999 (VI.6.4.2 GR3), printing "OVF" and
      *>     then "SB=999".
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1107.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  R1.
000600     05  P1    PIC X.
000700     05  SJ    PIC X(4) JUSTIFIED RIGHT SYNCHRONIZED.
000800     05  P2    PIC X.
000900     05  SX    PIC X(3) SYNC.
001000     05  P3    PIC X.
001100     05  SB    PIC 9(3) SYNC.
001200     05  P4    PIC X.
001300     05  SS    PIC S9(3) SIGN IS LEADING SEPARATE SYNC RIGHT.
001400 01  D3        PIC 9(3).
001500 PROCEDURE DIVISION.
001600 MAIN-PARA.
001700     MOVE "AB" TO SJ.
001800     MOVE "ABCDE" TO SX.
001900     MOVE 12345 TO SB.
002000     MOVE -12345 TO SS.
002100     MOVE SB TO D3.
002200     DISPLAY "SJ=[" SJ "] SX=[" SX "] SB=" D3.
002300     DISPLAY "SS=[" SS "]".
002400     MOVE 999 TO SB.
002500     ADD 1 TO SB ON SIZE ERROR DISPLAY "OVF".
002600     MOVE SB TO D3.
002700     DISPLAY "SB=" D3.
002800     STOP RUN.
