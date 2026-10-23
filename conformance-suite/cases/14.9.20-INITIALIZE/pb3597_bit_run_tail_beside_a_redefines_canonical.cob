*>--- conformance-case
*> id: pb3597_bit_run_tail_beside_a_redefines_canonical
*> rules: [GR-14.9.20.4-5, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3597 - THE IMPLICIT FILLER AFTER A RUN OF BIT ITEMS THAT HOLDS THE CANONICAL OF A REDEFINES CLASS IS DATA OF
      *> THE GROUP.  ISO 8.5.1.6.3: the bit positions that complete the characters the bit items occupy, "as needed to
      *> increase the number of bits to fill an integral number of characters", are "implicitly described as a filler
      *> elementary bit data item".  A group MOVE writes them (14.9.25.4 GR4: the group is moved "exactly as if it were an
      *> alphanumeric to alphanumeric elementary move", so its bytes are stored as they are) and a read of the group gives
      *> them back; ISO 14.9.20.4 5) a) 2.: "If the FILLER phrase is not specified, elementary data items with an explicit or
      *> implicit FILLER clause" are not receiving operands, so a bare INITIALIZE leaves them and WITH FILLER resets them.
      *> The compiler lost the tail whenever one of the run's items is the subject of a REDEFINES (13.18.44.4 GR1 lays the
      *> redefining item over the first bits of it): the tail was stored nowhere, so the MOVE read back zero and WITH FILLER
      *> had nothing to reset.  The shapes: G1 the canonical continues the run, G2 it leads the run, G3 it sits inside the
      *> run with a bit item after it.  (The redefining items have no storage of their own and are not touched.)
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1).  A tail that is not stored
      *> reads zero after the MOVE (the "move" legs print 65 where the rule requires 66); a tail that a bare INITIALIZE reset
      *> reads zero where the rule requires the moved bits (the "bare" legs print 1 for 2); a tail that INITIALIZE WITH FILLER
      *> did not reach keeps its moved bits where the rule requires zero (the "with" legs print 2 for 1).
      *>
      *> DERIVATION (high-order bit first; "A" = 0x41 = 01000001, "B" = 0x42; a MOVE of "AB" to a two-character group writes
      *> its two bytes, 14.9.25.4 GR4):
      *>   G1  M1 PIC 1(2), M2 PIC 1(3) (redefined by M2R), then PIC X: the run is 5 bits, 3 filler bits complete the byte.
      *>       Byte 1 = 01 000 001: M1 = 01, M2 = 000, filler = 001.  ORD 66,67.  Bare INITIALIZE zeroes M1, M2 and the
      *>       character, the filler keeps 001: byte 1 = 00000001, the character is a space -> ORD 2,33.  WITH FILLER zeroes
      *>       the filler too: byte 1 = 0 -> ORD 1,33.
      *>   G2  K1 PIC 1(3) (redefined by K1R) then K2 PIC 1(2) then PIC X: the same 5 bits, the same bytes -> 66,67 / 2,33 / 1,33.
      *>   G3  N1 PIC 1(2), N2 PIC 1(3) (redefined by N2R), N3 PIC 1, then PIC X: 6 bits and 2 filler bits.  Byte 1 = 01 000 0 01:
      *>       N3 = 0, filler = 01.  Bare: 00000001 -> 2,33; with the filler: 0 -> 1,33.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3597TAIL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 M1 PIC 1(2) USAGE BIT.
          05 M2 PIC 1(3) USAGE BIT.
          05 M2R REDEFINES M2 PIC 1(2) USAGE BIT.
          05 X1 PIC X.
       01 G2.
          05 K1 PIC 1(3) USAGE BIT.
          05 K1R REDEFINES K1 PIC 1(3) USAGE BIT.
          05 K2 PIC 1(2) USAGE BIT.
          05 X2 PIC X.
       01 G3.
          05 N1 PIC 1(2) USAGE BIT.
          05 N2 PIC 1(3) USAGE BIT.
          05 N2R REDEFINES N2 PIC 1(2) USAGE BIT.
          05 N3 PIC 1 USAGE BIT.
          05 X3 PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO G1
           DISPLAY "G1 move " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           INITIALIZE G1
           DISPLAY "G1 bare " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           MOVE "AB" TO G1
           INITIALIZE G1 WITH FILLER
           DISPLAY "G1 with " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           MOVE "AB" TO G2
           DISPLAY "G2 move " FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1))
           INITIALIZE G2
           DISPLAY "G2 bare " FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1))
           MOVE "AB" TO G2
           INITIALIZE G2 WITH FILLER
           DISPLAY "G2 with " FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1))
           MOVE "AB" TO G3
           DISPLAY "G3 move " FUNCTION ORD(G3(1:1)) "," FUNCTION ORD(G3(2:1))
           INITIALIZE G3
           DISPLAY "G3 bare " FUNCTION ORD(G3(1:1)) "," FUNCTION ORD(G3(2:1))
           MOVE "AB" TO G3
           INITIALIZE G3 WITH FILLER
           DISPLAY "G3 with " FUNCTION ORD(G3(1:1)) "," FUNCTION ORD(G3(2:1))
           STOP RUN.
