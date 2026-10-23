*>--- conformance-case
*> id: pb3564_initialize_with_filler_resets_implicit_filler
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 (review round 1, R1-4) - INITIALIZE ... WITH FILLER RESETS THE IMPLICIT FILLER BITS.
      *> ISO 8.5.1.6.3 (Alignment of data items of usage bit): the bit positions skipped to reach the next item's natural
      *> boundary, and those that complete the last byte of a record, "are implicitly described as a filler elementary
      *> bit data item".  Such an item is data of its group (a group MOVE writes it, see the MOVE golden
      *> cases/14.9.25-MOVE/pb3564_group_move_keeps_bit_filler).
      *> ISO 14.9.20.4 5) a) 2.: "If the FILLER phrase is not specified, elementary data items with an explicit or
      *> implicit FILLER clause" are not receiving operands, so a bare INITIALIZE leaves the implicit filler bits and
      *> INITIALIZE ... WITH FILLER makes them receivers: category boolean, so the default sender is ZEROES (GR6 c) and
      *> a REPLACING BOOLEAN DATA sender (GR6 b) is moved to them like to any other boolean item.
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1), so a filler that INITIALIZE
      *> WITH FILLER did not reach keeps its group-move value where the rule requires zero (L2, L3, L4, L5, L6) and a filler
      *> that a bare INITIALIZE reset reads zero where the rule requires the group-move value (L1, L4, L5).
      *>
      *> DERIVATIONS (high-order bit first; ORD of a byte is its value + 1; "A" = 0x41, "B" = 0x42, space = 0x20):
      *>   L1  G1 = "AB": G1-B is the first bit of 0x41 = 0100 0001, so G1-B = 0 and the seven filler bits are 1000001.
      *>       INITIALIZE G1 stores 0 in G1-B and a space in G1-X and leaves the filler: 0 1000001 = 0x41 -> ORD 66; the
      *>       space is ORD 33.
      *>   L2  INITIALIZE G1 WITH FILLER also zeroes the seven filler bits: 0x00 -> ORD 1; space -> 33.
      *>   L3  G2 is a table of two (bit, character) elements, MOVE "ABCD": every element's filler is the seven low bits of
      *>       its first byte; WITH FILLER zeroes them in both elements: ORD 1, 33, 1, 33.
      *>   L4  G4's table T is PIC 1(3) ALIGNED OCCURS 3, so each occurrence sits at the start of a byte and five filler
      *>       bits follow it (13.18.1.4 GR2).  MOVE "ABCD" stores 0x41 0x42 0x43 0x44 (T(3) = 010 with filler 00011, G4-X =
      *>       "D").  A bare INITIALIZE zeroes the three bits of each T occurrence only: 0000 0001, 0000 0010, 0000 0011 ->
      *>       ORD 2, 3, 4; G4-X is a space, ORD 33.  WITH FILLER zeroes the five filler bits as well: ORD 1, 1, 1, 33.
      *>   L5  G5 is a bit group, B1 PIC 1(2) then B2 PIC 1(3) ALIGNED: B1 is bits 1-2, six filler bits advance to the byte
      *>       boundary, B2 is bits 9-11 (11 bits).  MOVE ALL B"1" sets all eleven.  A bare INITIALIZE zeroes B1 and B2 and
      *>       leaves the gap: 00 111111 000.  WITH FILLER zeroes the gap too: 00000000000.
      *>   L6  INITIALIZE ... WITH FILLER REPLACING BOOLEAN DATA BY B"1" sends B"1" to every boolean receiver: G6-B takes
      *>       1, and the seven implicit filler bits are a boolean item of seven positions, which takes B"1" as a MOVE of
      *>       B"1" to PIC 1(7) does (the explicit FILLER PIC 1(7) of H6 is the control, and both read the same):
      *>       1 1000000 = 0xC0 -> ORD 193; the character is not boolean, so it keeps its value "B" = ORD 67.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1-B PIC 1 USAGE BIT.
          05 G1-X PIC X.
       01 G2.
          05 G2-E OCCURS 2.
             10 G2-B PIC 1 USAGE BIT.
             10 G2-X PIC X.
       01 G4.
          05 G4-T PIC 1(3) USAGE BIT ALIGNED OCCURS 3.
          05 G4-X PIC X.
       01 G5 GROUP-USAGE BIT.
          05 G5-B1 PIC 1(2).
          05 G5-B2 PIC 1(3) ALIGNED.
       01 G6.
          05 G6-B PIC 1 USAGE BIT.
          05 G6-X PIC X.
       01 H6.
          05 H6-B PIC 1 USAGE BIT.
          05 FILLER PIC 1(7) USAGE BIT.
          05 H6-X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO G1
           INITIALIZE G1
           DISPLAY "L1 " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           MOVE "AB" TO G1
           INITIALIZE G1 WITH FILLER
           DISPLAY "L2 " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           MOVE "ABCD" TO G2
           INITIALIZE G2 WITH FILLER
           DISPLAY "L3 " FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1)) ","
               FUNCTION ORD(G2(3:1)) "," FUNCTION ORD(G2(4:1))
           MOVE "ABCD" TO G4
           INITIALIZE G4
           DISPLAY "L4 bare " FUNCTION ORD(G4(1:1)) "," FUNCTION ORD(G4(2:1)) ","
               FUNCTION ORD(G4(3:1)) "," FUNCTION ORD(G4(4:1))
           MOVE "ABCD" TO G4
           INITIALIZE G4 WITH FILLER
           DISPLAY "L4 with " FUNCTION ORD(G4(1:1)) "," FUNCTION ORD(G4(2:1)) ","
               FUNCTION ORD(G4(3:1)) "," FUNCTION ORD(G4(4:1))
           MOVE ALL B"1" TO G5
           INITIALIZE G5
           DISPLAY "L5 bare " G5
           MOVE ALL B"1" TO G5
           INITIALIZE G5 WITH FILLER
           DISPLAY "L5 with " G5
           MOVE "AB" TO G6
           MOVE X"FF42" TO H6
           INITIALIZE G6 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           INITIALIZE H6 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           DISPLAY "L6 G6 " FUNCTION ORD(G6(1:1)) "," FUNCTION ORD(G6(2:1))
               " H6 " FUNCTION ORD(H6(1:1)) "," FUNCTION ORD(H6(2:1))
           STOP RUN.
