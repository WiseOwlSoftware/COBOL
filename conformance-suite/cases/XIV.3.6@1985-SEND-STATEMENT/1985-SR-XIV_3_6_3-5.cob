      *>--- conformance-case
      *> id: 1985-SR-XIV_3_6_3-5
      *> rules: [1985-SR-XIV.3.6.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.6.3 SR5 (THE SEND STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. SNDZERO.
000300 ENVIRONMENT DIVISION.
000400 DATA DIVISION.
000500 WORKING-STORAGE SECTION.
000600 01  WS-FLAG    PIC 9     VALUE 0.
000700 01  WS-MSG     PIC X(10) VALUE "HELLO".
000800 01  WS-LINES   PIC 9     VALUE 0.
000900 COMMUNICATION SECTION.
001000 CD  OUT-CD FOR OUTPUT
001100     TEXT LENGTH IS OUT-LEN
001200     SYMBOLIC DESTINATION IS OUT-DEST.
001300 PROCEDURE DIVISION.
001400 MAIN-PARA.
001500     IF WS-FLAG = 1
001600         MOVE "TERM1" TO OUT-DEST
001700         MOVE 5 TO OUT-LEN
001800         SEND OUT-CD FROM WS-MSG WITH EMI
001900             AFTER ADVANCING 0 LINES
002000         SEND OUT-CD FROM WS-MSG WITH EMI
002100             BEFORE ADVANCING WS-LINES LINES
002200     END-IF.
002300     DISPLAY "SEND ZERO OK".
002400     STOP RUN.
