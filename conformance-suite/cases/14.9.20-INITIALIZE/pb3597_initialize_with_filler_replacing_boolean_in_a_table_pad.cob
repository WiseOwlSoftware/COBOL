*>--- conformance-case
*> id: pb3597_initialize_with_filler_replacing_boolean_in_a_table_pad
*> rules: [GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3597 - INITIALIZE ... WITH FILLER ... REPLACING BOOLEAN DATA RECEIVES EACH GAP BETWEEN THE OCCURRENCES OF AN
      *> ALIGNED BIT TABLE AS THE ELEMENTARY ITEM IT IS, in an ordinary (record) group.  ISO 8.5.1.6.3: the bit positions
      *> generated "as needed to advance alignment to a required natural boundary for the next item within that group"
      *> are "implicitly described as a filler elementary bit data item", so a table of n occurrences has n - 1 filler
      *> items between its occurrences (13.18.1.4 GR2: ALIGNED "applies to each occurrence of that item") and one more
      *> after the last when a byte must be completed.  ISO 14.9.20.4 5) a) 2.: "If the FILLER phrase is not specified,
      *> elementary data items with an explicit or implicit FILLER clause" are not receiving operands, so WITH FILLER makes
      *> every one a receiver, and a REPLACING BOOLEAN DATA sender is moved to EACH of them as to any other boolean item.
      *> The compiler stored the gaps of such a table as ONE string and refused this statement at run time by name; the
      *> sender has to reach each gap on its own (B"1" is one position, a gap is five: B"1" moved to ONE string of ten
      *> positions would set the first gap's first position and leave the second gap zero).
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1).  A compiler that refuses the
      *> statement aborts the run instead of printing; one that smears the sender across the stored pad prints 145 for the
      *> first byte and 1 or 129 for the second; one that leaves the gaps alone prints 129 for every byte; the explicit
      *> PIC 1(3) / FILLER PIC 1(5) record H is the control and reads the same as the table.
      *>
      *> DERIVATION (high-order bit first; "A" = 0x41, "B" = 0x42, "C" = 0x43, "D" = 0x44; a group MOVE of an alphanumeric
      *> literal writes the group's bytes as they are, 14.9.25.4 GR4):
      *>   T PIC 1(3) ALIGNED OCCURS 3 then PIC X: each occurrence starts a byte, so byte 1 = T(1) + 5 filler bits, byte 2 =
      *>   T(2) + 5 filler bits, byte 3 = T(3) + 5 tail bits, byte 4 = the character.  MOVE "ABCD" -> 0x41 0x42 0x43 "D".
      *>   REPLACING ALPHANUMERIC DATA BY "Z" reaches the character only: ORD 66,67,68,91 (the boolean bits are untouched).
      *>   REPLACING BOOLEAN DATA BY B"1" sends B"1" to every boolean receiver: each T(i) takes 1 left-justified in its
      *>   three positions (100) and each of the two gaps and the tail is its OWN five-position boolean item that takes it
      *>   too (10000): 100 10000 = 0x90 per byte -> ORD 145,145,145; the character is not boolean and keeps "Z" -> ORD 91.
      *>   WITH ZEROES default (no REPLACING): every gap and the tail become zero -> bytes 0x00 -> ORD 1,1,1; the
      *>   character is spaces by the default -> ORD 33.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3597PAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-T PIC 1(3) USAGE BIT ALIGNED OCCURS 3.
          05 G-X PIC X.
       01 H.
          05 H-T1 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H-T2 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H-T3 PIC 1(3) USAGE BIT.
          05 FILLER PIC 1(5) USAGE BIT.
          05 H-X PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO G
           INITIALIZE G WITH FILLER REPLACING ALPHANUMERIC DATA BY "Z"
           DISPLAY "G alnum " FUNCTION ORD(G(1:1)) "," FUNCTION ORD(G(2:1)) "," FUNCTION ORD(G(3:1)) "," FUNCTION ORD(G(4:1))
           INITIALIZE G WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           DISPLAY "G bool " FUNCTION ORD(G(1:1)) "," FUNCTION ORD(G(2:1)) "," FUNCTION ORD(G(3:1)) "," FUNCTION ORD(G(4:1))
           MOVE "ABCD" TO H
           INITIALIZE H WITH FILLER REPLACING ALPHANUMERIC DATA BY "Z"
           INITIALIZE H WITH FILLER REPLACING BOOLEAN DATA BY B"1"
           DISPLAY "H bool " FUNCTION ORD(H(1:1)) "," FUNCTION ORD(H(2:1)) "," FUNCTION ORD(H(3:1)) "," FUNCTION ORD(H(4:1))
           MOVE "ABCD" TO G
           INITIALIZE G WITH FILLER
           DISPLAY "G zero " FUNCTION ORD(G(1:1)) "," FUNCTION ORD(G(2:1)) "," FUNCTION ORD(G(3:1)) "," FUNCTION ORD(G(4:1))
           STOP RUN.
