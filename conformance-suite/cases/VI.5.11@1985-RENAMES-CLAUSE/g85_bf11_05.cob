      *>--- conformance-case
      *> id: g85_bf11_05
      *> rules: [1985-GR-VI.5.11.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.11.4 GR2 - "When data-name-3 is not specified, all of
      *> the data attributes of data-name-2 become the data attributes for
      *> data-name-1." Each 66 entry below renames ONE item, so it must
      *> behave as that item when it is a receiving operand:
      *>   RN renames N, PIC 9(3)V9: MOVE 7.25 TO RN aligns on the
      *>      decimal point and truncates to 007.2 (VI.6.19.4 GR4b); the
      *>      V occupies no position, so the 4 characters of N, shown
      *>      through the alphanumeric view NX (DISPLAY's conversion of
      *>      a numeric item is the implementor's, VI.6.10.4 GR1), are
      *>      "0072"; an alphanumeric RN would give "7.25".
      *>   RJ renames J, PIC X(4) JUSTIFIED RIGHT: MOVE "AB" TO RJ puts
      *>      [  AB] (VI.5.6.4 GR1); without JUSTIFIED it would be [AB  ].
      *>   RE renames E, PIC ZZ9: MOVE 5 TO RE edits to [  5].
      *>   RG renames G, a group of 3 characters: MOVE "PQRST" TO RG is
      *>      an alphanumeric move truncated to [PQR] (G1 "P", G2 "QR").
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1105.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  REC.
000600     05  N     PIC 9(3)V9.
000700     05  J     PIC X(4) JUSTIFIED RIGHT.
000800     05  E     PIC ZZ9.
000900     05  G.
001000         10  G1    PIC X.
001100         10  G2    PIC XX.
001200 66  RN RENAMES N.
001300 66  RJ RENAMES J.
001400 66  RE RENAMES E.
001500 66  RG RENAMES G.
001600 01  RX REDEFINES REC.
001700     05  NX    PIC X(4).
001800     05  FILLER PIC X(10).
001900 PROCEDURE DIVISION.
002000 MAIN-PARA.
002100     MOVE 7.25 TO RN.
002200     MOVE "AB" TO RJ.
002300     MOVE 5 TO RE.
002400     MOVE "PQRST" TO RG.
002500     DISPLAY "N=" NX.
002600     DISPLAY "J=[" J "]".
002700     DISPLAY "E=[" E "]".
002800     DISPLAY "G1=" G1 " G2=" G2.
002900     STOP RUN.
