*>--- conformance-case
*> id: pb3564_aligned_table_of_one_occurrence
*> rules: [GR-13.18.1.4-1, GR-13.18.1.4-2, GR-13.18.29.4-1, GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 (review round 1, R1-1) - AN ALIGNED TABLE OF ONE OCCURRENCE HAS NO PAD BETWEEN OCCURRENCES.
      *> ISO 13.18.1.4 GR2: "An ALIGNED clause specified for a multiple-occurrence data item applies to each occurrence of
      *> that item", so the occurrences of an ALIGNED table sit one byte-aligned stride apart and the bits between them are
      *> filler (8.5.1.6.3: "implicitly described as a filler elementary bit data item").  A table that is declared OCCURS 1
      *> has one occurrence and therefore no bits between occurrences: the only filler it owns is the one that completes
      *> its last byte before the next item (the bits that precede the next item's natural boundary), exactly as for the
      *> same item written without OCCURS.  The compiler once stored the between-occurrence pad for every ALIGNED table
      *> and asked for it again for a one-occurrence table that has none, so the program below stopped the compiler.
      *> ISO 13.18.29.4 GR1b: a bit group "is treated as though it were an elementary data item of usage bit and class
      *> and category boolean described with PICTURE 1(m), where m is the bit length of the group"; ISO 14.9.25.4 GR4
      *> makes a group move an alphanumeric move, so the bytes a group receives are the bytes it holds.
      *>
      *> HOW EACH LEG CAN FAIL.  The compile itself (the program must translate and run); L1 reads the filler next to T1(1)
      *> from the group move (a filler that lost its storage reads 0100 0000 = '@' where the move wrote 0100 0001 = 'A');
      *> L3 reads the six filler bits and the seven gap bits of a bit group whose first member is a one-occurrence table.
      *>
      *> DERIVATIONS (high-order bit first; ORD of a byte is its value + 1):
      *>   A1  T1 PIC 1(3) ALIGNED OCCURS 1 is bits 0-2 of byte 1 and five filler bits follow; X1 is byte 2.  X"4F42":
      *>       0x4F = 0100 1111 -> T1(1) = 010, filler 01111; X1 = "B".  ORD(0x4F) = 80.
      *>   L2  MOVE B"101" TO T1(1) stores the three bits and keeps the filler: 101 01111 = 0xAF -> ORD 176.
      *>   L3  BG GROUP-USAGE BIT: S(1) holds SA PIC 1 (bit 0), seven filler bits, TA PIC 1(5) ALIGNED (bits 8-12), so
      *>       S is 13 bits wide; Z PIC 1(2) follows at bit 13 (a bit group's next member continues the bit position), so
      *>       BG is 15 bits.  MOVE ALL B"1" TO BG sets every one of the 15 positions, the seven gap bits included.
      *>   L4  INITIALIZE A1 WITH FILLER (14.9.20.4 5) a) 2.: the implicit filler is a receiving operand when the FILLER
      *>       phrase is specified) zeroes T1(1), the filler and sets X1 to a space: 0x00 -> ORD 1, space -> ORD 33.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564O1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1.
          05 T1 PIC 1(3) USAGE BIT ALIGNED OCCURS 1.
          05 X1 PIC X.
       01 BG GROUP-USAGE BIT.
          05 S OCCURS 1.
             10 SA PIC 1.
             10 TA PIC 1(5) ALIGNED.
          05 Z PIC 1(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE X"4F42" TO A1
           DISPLAY "L1 T1=" T1(1) " X1=[" X1 "] ORD=" FUNCTION ORD(A1(1:1)) "," FUNCTION ORD(A1(2:1))
           MOVE B"101" TO T1(1)
           DISPLAY "L2 ORD=" FUNCTION ORD(A1(1:1))
           MOVE ALL B"1" TO BG
           DISPLAY "L3 SA=" SA(1) " TA=" TA(1) " Z=" Z " BG=[" BG "]"
           INITIALIZE A1 WITH FILLER
           DISPLAY "L4 ORD=" FUNCTION ORD(A1(1:1)) "," FUNCTION ORD(A1(2:1))
           STOP RUN.
