*>--- conformance-case
*> id: pb3564_initialize_with_filler_in_shared_storage
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3564 (review round 2, R2-1) - INITIALIZE ... WITH FILLER RESETS THE IMPLICIT FILLER BITS OF A GROUP THAT
      *> LIVES IN SHARED STORAGE: an EXTERNAL record, a BASED record and a REDEFINES area.  The record-struct lane
      *> (cases/14.9.20-INITIALIZE/pb3564_initialize_with_filler_resets_implicit_filler) holds the filler in a field of its
      *> own; here the group's bits, filler included, are bytes of one shared backing (13.18.44.4 GR1: "Storage association
      *> for the subject of the entry starts at the first bit of the data item referenced by data-name-2"), so each gap of
      *> filler is addressed as the elementary bit item 8.5.1.6.3 says it is ("implicitly described as a filler elementary
      *> bit data item") at its own bit position of that backing.
      *> ISO 14.9.20.4 5) a) 2.: "If the FILLER phrase is not specified, elementary data items with an explicit or
      *> implicit FILLER clause" are not receiving operands, so a bare INITIALIZE leaves the filler and INITIALIZE ...
      *> WITH FILLER makes it a receiver: category boolean, so the default sender is ZEROES (GR6 c) and a REPLACING
      *> BOOLEAN DATA sender (GR6 b) is moved to EACH gap as to any other boolean item.
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1).  A filler that INITIALIZE
      *> WITH FILLER did not reach keeps its group-move value where the rule requires zero (the "with" legs of E1, E2, B3
      *> and R4, and E5), a filler that a bare INITIALIZE reset reads zero where the rule requires the group-move value
      *> (the "bare" legs of E1, E2, B3 and R4), and a compiler that cannot address the filler in these lanes aborts the
      *> run instead of printing.
      *>
      *> DERIVATIONS (high-order bit first; "A" = 0x41, "B" = 0x42, space = 0x20; a group MOVE of an alphanumeric literal
      *> writes the group's bytes as they are, 14.9.25.4 GR4, and pads with spaces):
      *>   E1  PIC 1(3) ALIGNED OCCURS 2 then PIC X: each occurrence sits at the start of a byte (13.18.1.4 GR2), so byte 1
      *>       = T(1) 3 bits + 5 filler bits, byte 2 = T(2) 3 bits + 5 tail bits, byte 3 = the character.  MOVE "AB":
      *>       0x41, 0x42, space.  Bare INITIALIZE zeroes T(1), T(2) and the character: 0x01, 0x02, space -> ORD 2,3,33.
      *>       WITH FILLER zeroes the five-bit gap between the occurrences and the five-bit tail too: ORD 1,1,33.
      *>   E2  PIC 1 then two PIC X: the bit is the first bit of byte 1 and seven filler bits complete the byte.  MOVE "AB":
      *>       byte 1 = 0x41.  Bare: bit zeroed, filler 1000001 kept: 0x41 -> ORD 66; both characters spaces: 33,33.
      *>       WITH FILLER: 0x00 -> ORD 1,33,33.
      *>   B3  the BASED twin of E1: ORD 2,3,33 bare, 1,1,33 with the filler.
      *>   R4  a REDEFINES area (a Tier-B class): B is a view of A (three characters) with the shape of E1; the same
      *>       bytes: ORD 2,3,33 bare, 1,1,33 with the filler.
      *>   E5  PIC 1(3) ALIGNED OCCURS 3 then PIC X, MOVE "ABCD" -> 0x41 0x42 0x43 "D".  INITIALIZE ... WITH FILLER
      *>       REPLACING BOOLEAN DATA BY B"1" sends B"1" to every boolean receiver: each T(i) takes 1 left-justified in
      *>       its three positions (100), and each gap (two between the occurrences, one tail) is its OWN five-position
      *>       boolean item that takes it too (10000): 100 10000 = 0x90 per byte -> ORD 145,145,145; the character is not
      *>       boolean and keeps "D" -> ORD 69.  The explicit PIC 1(3) / FILLER PIC 1(5) record H5 is the control and reads
      *>       the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3564SH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 EXTERNAL.
          05 E1-T PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
          05 E1-X PIC X.
       01 E2 EXTERNAL.
          05 E2-B PIC 1 USAGE BIT.
          05 E2-X PIC X.
          05 E2-Y PIC X.
       01 B3 BASED.
          05 B3-T PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
          05 B3-X PIC X.
       01 R4.
          05 R4-A PIC X(3).
          05 R4-B REDEFINES R4-A.
             10 R4-T PIC 1(3) USAGE BIT ALIGNED OCCURS 2.
             10 R4-X PIC X.
       01 E5 EXTERNAL.
          05 E5-T PIC 1(3) USAGE BIT ALIGNED OCCURS 3.
          05 E5-X PIC X.
       01 H5.
          05 H5-T1 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H5-T2 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H5-T3 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H5-X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AB" TO E1
           INITIALIZE E1
           DISPLAY "E1 bare " FUNCTION ORD(E1(1:1)) "," FUNCTION ORD(E1(2:1)) "," FUNCTION ORD(E1(3:1))
           MOVE "AB" TO E1
           INITIALIZE E1 WITH FILLER
           DISPLAY "E1 with " FUNCTION ORD(E1(1:1)) "," FUNCTION ORD(E1(2:1)) "," FUNCTION ORD(E1(3:1))
           MOVE "AB" TO E2
           INITIALIZE E2
           DISPLAY "E2 bare " FUNCTION ORD(E2(1:1)) "," FUNCTION ORD(E2(2:1)) "," FUNCTION ORD(E2(3:1))
           MOVE "AB" TO E2
           INITIALIZE E2 WITH FILLER
           DISPLAY "E2 with " FUNCTION ORD(E2(1:1)) "," FUNCTION ORD(E2(2:1)) "," FUNCTION ORD(E2(3:1))
           ALLOCATE B3
           MOVE "AB" TO B3
           INITIALIZE B3
           DISPLAY "B3 bare " FUNCTION ORD(B3(1:1)) "," FUNCTION ORD(B3(2:1)) "," FUNCTION ORD(B3(3:1))
           MOVE "AB" TO B3
           INITIALIZE B3 WITH FILLER
           DISPLAY "B3 with " FUNCTION ORD(B3(1:1)) "," FUNCTION ORD(B3(2:1)) "," FUNCTION ORD(B3(3:1))
           MOVE "AB" TO R4
           INITIALIZE R4-B
           DISPLAY "R4 bare " FUNCTION ORD(R4(1:1)) "," FUNCTION ORD(R4(2:1)) "," FUNCTION ORD(R4(3:1))
           MOVE "AB" TO R4
           INITIALIZE R4-B WITH FILLER
           DISPLAY "R4 with " FUNCTION ORD(R4(1:1)) "," FUNCTION ORD(R4(2:1)) "," FUNCTION ORD(R4(3:1))
           MOVE "ABCD" TO E5
           MOVE X"FFFFFF44" TO H5
           INITIALIZE E5 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           INITIALIZE H5 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           DISPLAY "E5 " FUNCTION ORD(E5(1:1)) "," FUNCTION ORD(E5(2:1)) "," FUNCTION ORD(E5(3:1)) "," FUNCTION ORD(E5(4:1))
               " H5 " FUNCTION ORD(H5(1:1)) "," FUNCTION ORD(H5(2:1)) "," FUNCTION ORD(H5(3:1)) "," FUNCTION ORD(H5(4:1))
           STOP RUN.
