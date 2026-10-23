*>--- conformance-case
*> id: pb3564_aligned_filler_keeps_group_bits
*> rules: [GR-13.18.1.4-1, GR-13.18.1.4-2, GR-13.18.29.4-1, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 - THE IMPLICIT FILLER AN ALIGNED CLAUSE GENERATES IS STORAGE OF ITS GROUP.
      *> ISO 13.18.1.4 GR1: ALIGNED "causes the subject of the entry to be aligned on the first bit of the first
      *> available byte boundary.  Implicit filler bits may be generated to complete the assignment of bits as
      *> described in 8.5.1.6.3."  GR2: "An ALIGNED clause specified for a multiple-occurrence data item applies to
      *> each occurrence of that item", so every occurrence of an ALIGNED table starts a byte and the bits between
      *> the occurrences are filler.  8.5.1.6.3: those filler bit positions "are implicitly described as a filler
      *> elementary bit data item", so a group move writes them and the group reads them back (14.9.25.4 GR4: a
      *> group move is an alphanumeric move, the bytes round-trip).
      *> ISO 13.18.29.4 GR1b: a bit group "is treated as though it were an elementary data item of usage bit and class
      *> and category boolean described with PICTURE 1(m), where m is the bit length of the group" - its filler bits are
      *> part of m, so a MOVE of m boolean positions to the bit group stores all of them, and DISPLAY shows all of them.
      *> 13.18.63.4 GR5: a VALUE on a group initializes "the group area ... without consideration for the individual
      *> elementary or group items contained within this group", so the filler positions take their share of the value.
      *>
      *> HOW EACH LEG CAN FAIL.  A filler without storage reads zero where the move put a one: L1 loses the pad and
      *> the tail of the ALIGNED table, L4 and L5 lose the six filler positions of the bit group, L6 loses them in the
      *> bytes the alias reads, and L3 proves an elementary store to a member leaves the filler next to it alone.
      *>
      *> DERIVATIONS (high-order bit first; ORD of a byte is its value + 1):
      *>   G   T PIC 1(3) ALIGNED OCCURS 2: T(1) = bits 0-2, filler bits 3-7; T(2) = bits 8-10, filler bits 11-15 (the
      *>       end of the record, 8.5.1.6.3); X = byte 3.   X"4F6E43": 0x4F = 0100 1111 -> T(1) = 010, pad 01111;
      *>       0x6E = 0110 1110 -> T(2) = 011, tail 01110; "C".   ORD: 80, 111, 68.
      *>   L3  MOVE B"101" TO T(1): 101 + pad 01111 = 0xAF -> ORD 176 (the pad is kept); MOVE B"000" TO T(2):
      *>       000 + tail 01110 = 0x0E -> ORD 15.
      *>   BG  GROUP-USAGE BIT, BA PIC 1(2), BB PIC 1(2) ALIGNED: BA = bits 0-1, filler bits 2-7, BB = bits 8-9, m = 10.
      *>       B"1011010110" = BA 10, filler 110101, BB 10.
      *>   BV  VALUE B"1111111111" over the same shape: every position of the area, filler included, is 1.
      *>   BG2/BW  the same group with a REDEFINES alias reading its bytes: 10110101 = 0xB5 -> ORD 182; the last two
      *>       positions 10 are bits 8-9 of the second byte: 10 000000 = 0x80 -> ORD 129.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
          05 X PIC X.
       01 G2 PIC X(3).
       01 BG GROUP-USAGE BIT.
          05 BA PIC 1(2).
          05 BB PIC 1(2) ALIGNED.
       01 BV GROUP-USAGE BIT VALUE B"1111111111".
          05 VA PIC 1(2).
          05 VB PIC 1(2) ALIGNED.
       01 BG2 GROUP-USAGE BIT.
          05 CA PIC 1(2).
          05 CB PIC 1(2) ALIGNED.
       01 BW REDEFINES BG2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE X"4F6E43" TO G
           DISPLAY "L1 T=" T(1) "/" T(2) " X=[" X "] ORD=" FUNCTION ORD(G(1:1)) ","
               FUNCTION ORD(G(2:1)) "," FUNCTION ORD(G(3:1))
           MOVE G TO G2
           DISPLAY "L2 G2=" FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1)) "," FUNCTION ORD(G2(3:1))
           MOVE B"101" TO T(1)
           DISPLAY "L3 ORD=" FUNCTION ORD(G(1:1)) "," FUNCTION ORD(G(2:1))
           MOVE B"000" TO T(2)
           DISPLAY "L3 ORD=" FUNCTION ORD(G(1:1)) "," FUNCTION ORD(G(2:1))
           MOVE B"1011010110" TO BG
           DISPLAY "L4 BG=[" BG "] BA=[" BA "] BB=[" BB "]"
           DISPLAY "L5 BV=[" BV "] VA=[" VA "] VB=[" VB "]"
           MOVE B"1011010110" TO BG2
           DISPLAY "L6 BG2=[" BG2 "] CA=[" CA "] CB=[" CB "] BW=" FUNCTION ORD(BW(1:1)) ","
               FUNCTION ORD(BW(2:1))
           STOP RUN.
