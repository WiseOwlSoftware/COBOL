      *>--- conformance-case
      *> id: g2prf01_cd_operands
      *> rules: [2002-SR-14.8.27.2-2, 2002-SR-14.8.27.2-5,
      *>    2002-SR-14.8.27.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PERFORM operands that are communication-section items: the
      *> input CD's TEXT LENGTH TL and MESSAGE COUNT MC (integers of
      *> 4 and 6 digits, §2002:A.1.2.3 GR2 h, k) and the integer items
      *> IN4 9(4), TL1 9, TL2 9 of the record that redefines its area
      *> (§2002:A.1.2.2 SR5; INREC positions 13-18, apart from TL at
      *> 75-78 and MC at 82-87). The facility draws COBOL0903.
      *> §2002:14.8.27.2 SR2 - each identifier a numeric elementary
      *>   item, identifier-1 an integer: MC TIMES (N line) and
      *>   VARYING IN4 BY TL (V lines) are legal. §2002:14.8.27.3 GR7:
      *>   MC = 3 at the start -> N=0003. GR9 d: IN4 from 1 by 2 until
      *>   IN4 > 7 -> 1 3 5 7.
      *> §2002:14.8.27.2 SR5 a - an index-name in VARYING or AFTER
      *>   takes integer FROM/BY identifiers: VARYING TX FROM TL1 BY
      *>   TL (TL1 = 1, TL = 2, until TX > 5) -> occurrences 1 3 5;
      *>   AFTER TX FROM TL1 BY TL UNTIL TX > 3 under VARYING W-N 1..2
      *>   -> 1/1 1/3 2/1 2/3 (GR9 e resets TX for each W-N).
      *> §2002:14.8.27.2 SR6 a, b - an index-name in FROM takes an
      *>   integer VARYING and BY identifier: VARYING IN4 FROM TX BY
      *>   TL2 (TX at occurrence 2, TL2 = 1, until IN4 > 3) -> 2 3;
      *>   the initialization value is the occurrence number of TX.
      *>   The AFTER arm: AFTER IN4 FROM TX BY TL2 UNTIL IN4 > 3 under
      *>   VARYING W-N 1..1 (TX still at occurrence 2) -> 1/2 1/3;
      *>   GR9 e: IN4 = 4 resets IN4, W-N becomes 2, condition-1 true.
      *> Fails if a CD integer item is refused in any of these places
      *> or its value is read from the wrong positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N  PIC 9(4) VALUE 0.
       01 W-X  PIC 9(4).
       01 T.
          05 TE PIC X OCCURS 9 INDEXED BY TX.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL
           MESSAGE COUNT IS MC.
       01 INREC.
          05 FILLER PIC X(12).
          05 IN4    PIC 9(4).
          05 TL1    PIC 9.
          05 TL2    PIC 9.
          05 FILLER PIC X(69).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 3 TO MC.
           PERFORM MC TIMES
               ADD 1 TO W-N
           END-PERFORM.
           DISPLAY "N=" W-N.
           MOVE 2 TO TL.
           PERFORM VARYING IN4 FROM 1 BY TL UNTIL IN4 > 7
               DISPLAY "V=" IN4
           END-PERFORM.
           MOVE 1 TO TL1.
           PERFORM VARYING TX FROM TL1 BY TL UNTIL TX > 5
               SET W-X TO TX
               DISPLAY "X=" W-X
           END-PERFORM.
           PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 2
                   AFTER TX FROM TL1 BY TL UNTIL TX > 3
               SET W-X TO TX
               DISPLAY "A=" W-N "/" W-X
           END-PERFORM.
           MOVE 1 TO TL2.
           SET TX TO 2.
           PERFORM VARYING IN4 FROM TX BY TL2 UNTIL IN4 > 3
               DISPLAY "Y=" IN4
           END-PERFORM.
           PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 1
                   AFTER IN4 FROM TX BY TL2 UNTIL IN4 > 3
               DISPLAY "Z=" W-N "/" IN4
           END-PERFORM.
           STOP RUN.
