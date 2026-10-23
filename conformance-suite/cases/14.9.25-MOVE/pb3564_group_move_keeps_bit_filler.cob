*>--- conformance-case
*> id: pb3564_group_move_keeps_bit_filler
*> rules: [GR-14.9.25.4-4, GR-14.9.20.4-5]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 - THE IMPLICIT FILLER BITS AFTER A USAGE BIT ITEM ARE DATA OF THEIR GROUP.
      *> ISO 8.5.1.6.3 (Alignment of data items of usage bit): after a bit item in an alphanumeric group the item that
      *> follows "is at the first bit position of the first available byte", and the bits skipped are generated "as
      *> needed to advance alignment to a required natural boundary for the next item within that group.  The filler
      *> bit positions are implicitly described as a filler elementary bit data item".  At the end of a record "as
      *> needed to increase the number of bits to fill an integral number of characters", described the same way.
      *> A filler item is storage of the group, so a group MOVE writes it and a DISPLAY, a comparison or a MOVE of the
      *> group reads it back.
      *> ISO 14.9.25.4 GR4: a group move "is treated exactly as if it were an alphanumeric to alphanumeric elementary
      *> move, except that there is no conversion of data from one form of internal representation to another", so the
      *> bytes the group receives are the bytes it holds: MOVE "AB" TO G1 then DISPLAY G1 shows AB.  Before the fix
      *> the filler had no storage, MOVE "AB" TO G1 stored 'A' as its first bit and zeros, and G1 read NUL,B.
      *> ISO 14.9.20.4 GR5 a) 2: INITIALIZE without the FILLER phrase leaves "elementary data items with an explicit or
      *> implicit FILLER clause" unchanged, and the implicit filler bits are such items.
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1) or through the bit item,
      *> so a filler that was dropped reads zero where the move put a one, and a filler that was reset by a neighbour's
      *> store reads wrong in L5.
      *>
      *> DERIVATIONS (high-order bit first; ORD of a byte is its value + 1):
      *>   L1  "A" = 0x41 = 0100 0001: G1-B is the first bit = 0, the other seven are the filler = 1000001.  "B" is G1-X.
      *>   L2  X"C1" = 1100 0001: G1-B = 1, filler 1000001.  ORD(0xC1) = 194; "B" = 0x42 -> ORD 67.
      *>   L3  MOVE G1 TO W2 copies the two bytes: 194, 67.  MOVE G1 TO G3 (a group of the same shape) the same; G3 = G1
      *>       holds, G1 = X"4142" does not (the first byte differs in its first bit and its filler).
      *>   L4  X"7E41FE42" in H (two bit runs with a character between): 0x7E = 0111 1110, so H-B1 = 01 then filler
      *>       111110; H-Y = "A"; 0xFE = 1111 1110, so H-B2 = 11 then filler 111110; H-Z = "B".
      *>       ORD: 0x7E = 127, "A" = 66, 0xFE = 255, "B" = 67.
      *>   L5  INITIALIZE H stores zeros in H-B1 and H-B2 and spaces in H-Y and H-Z and leaves the filler (GR5 a) 2):
      *>       00 111110 = 0x3E -> ORD 63; space = 0x20 -> ORD 33; again 63 and 33.
      *>   L6  G4 ends in a bit item: X"41FE" -> G4-X = "A", G4-B = 111 (the first three bits of 0xFE) and the five
      *>       trailing filler bits 11110 are the last item of the record (8.5.1.6.3), so the byte is 0xFE -> ORD 255.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564MV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1-B PIC 1 USAGE BIT.
          05 G1-X PIC X.
       01 G3.
          05 G3-B PIC 1 USAGE BIT.
          05 G3-X PIC X.
       01 H.
          05 H-B1 PIC 1(2) USAGE BIT.
          05 H-Y PIC X.
          05 H-B2 PIC 1(2) USAGE BIT.
          05 H-Z PIC X.
       01 G4.
          05 G4-X PIC X.
          05 G4-B PIC 1(3) USAGE BIT.
       01 W2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO G1
           DISPLAY "L1 G1=[" G1 "] X=[" G1-X "] B=[" G1-B "]"
           MOVE X"C142" TO G1
           DISPLAY "L2 B=[" G1-B "] X=[" G1-X "] ORD=" FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1))
           MOVE G1 TO W2
           MOVE G1 TO G3
           DISPLAY "L3 W2=" FUNCTION ORD(W2(1:1)) "," FUNCTION ORD(W2(2:1))
               " G3=" FUNCTION ORD(G3(1:1)) "," FUNCTION ORD(G3(2:1)) " B=[" G3-B "]"
           IF G3 = G1 DISPLAY "L3 group equal" ELSE DISPLAY "L3 group DIFFERENT" END-IF
           IF G1 = X"4142" DISPLAY "L3 literal equal" ELSE DISPLAY "L3 literal different" END-IF
           MOVE X"7E41FE42" TO H
           DISPLAY "L4 B1=[" H-B1 "] Y=[" H-Y "] B2=[" H-B2 "] Z=[" H-Z "] ORD="
               FUNCTION ORD(H(1:1)) "," FUNCTION ORD(H(2:1)) ","
               FUNCTION ORD(H(3:1)) "," FUNCTION ORD(H(4:1))
           INITIALIZE H
           DISPLAY "L5 ORD=" FUNCTION ORD(H(1:1)) "," FUNCTION ORD(H(2:1)) ","
               FUNCTION ORD(H(3:1)) "," FUNCTION ORD(H(4:1))
           MOVE X"41FE" TO G4
           DISPLAY "L6 X=[" G4-X "] B=[" G4-B "] ORD=" FUNCTION ORD(G4(2:1))
           STOP RUN.
