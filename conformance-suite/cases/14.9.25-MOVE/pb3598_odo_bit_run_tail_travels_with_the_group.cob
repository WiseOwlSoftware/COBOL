*>--- conformance-case
*> id: pb3598_odo_bit_run_tail_travels_with_the_group
*> rules: [GR-14.9.25.4-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3598 - THE IMPLICIT FILLER AFTER A RUN THAT HOLDS AN OCCURS DEPENDING BIT TABLE IS DATA OF THE GROUP AT EVERY
      *> COUNT.  ISO 8.5.1.6.3: the bit positions that complete the characters the bit items occupy ("as needed to increase the
      *> number of bits to fill an integral number of characters") are "implicitly described as a filler elementary bit data
      *> item", so a group MOVE writes them (14.9.25.4 GR4: the group is moved "exactly as if it were an alphanumeric to
      *> alphanumeric elementary move", whole characters) and the receiver reads them back.  The table is counted by NN, a
      *> data item OUTSIDE the groups, so ISO 13.18.38.4 GR8 a) applies to the receiving group: "only that part of the table area
      *> that is specified by the value of the data item referenced by data-name-1 at the start of the operation will be
      *> used" - the occurrences past the count keep their content.  The group's current extent is a whole number of
      *> characters, and the filler positions that end its last character are inside it.  At a count whose last character
      *> reaches past the maximum extent they lie beyond the last occurrence, in storage no occurrence owns, and the compiler
      *> kept them nowhere.  (Where the last character of a smaller count's extent reaches the first unused occurrence's storage,
      *> those bits are NOT filler: 8.5.1.6.3 puts the filler after "the last data item in a record", the table at its maximum, so
      *> they are an occurrence 13.18.38.4 GR8 a) leaves unused and 13.18.38.4 7) calls undefined.  These groups hold no dynamic-length
      *> item, so they are fixed-length groups (8.5.1.12.1) and the move is 14.9.25.4 GR4's alphanumeric move of whole characters; the
      *> variable-length group of pb3598_odo_bit_run_tail_variable_group moves by GR9 and keeps those bits.  Both conform.)
      *>
      *> HOW EACH LEG CAN FAIL.  Every value is read as a byte through FUNCTION ORD (byte + 1).  "~" is 0x7E = 01111110 and
      *> "z" is 0x7A = 01111010, so the senders and the receivers hold different filler.  A receiver with no storage for the
      *> positions past the maximum extent reads 125 where the rule requires 127 (leg C3: the last two positions of the
      *> second character).  Legs C1 and C2 are the controls that pass today (the groups here are ordinary ones, whose receiving
      *> store takes the last used character whole from the sender, and a count with no filler has nothing to lose).
      *>
      *> DERIVATION.  B PIC 1(2), T PIC 1(3) OCCURS 1 TO 4 DEPENDING ON NN: the maximum extent is 2 + 12 = 14 bits = 2 characters
      *> with 2 filler bits past T(4).
      *>   C1  NN = 1: the extent is 2 + 3 = 5 bits = one character, 3 filler bits.  The sender holds "~": B = 01, T(1) = 111,
      *>       filler = 110.  The receiver holds "z" (filler 010).  MOVE: the receiver's one character is "~" -> ORD 127.
      *>   C2  NN = 2: 2 + 6 = 8 bits, one character, no filler.  "~" -> ORD 127 in both.
      *>   C3  NN = 3: 2 + 9 = 11 bits = two characters, 5 filler bits: T(4)'s three positions and the two past the maximum.
      *>       The sender holds "~~": character 2 = 01111110 = T(3) 011, filler 11110.  The receiver reads "~~" -> ORD 127,127.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3598ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN PIC 9.
       01 G1.
          05 B1 PIC 1(2) USAGE BIT.
          05 T1 PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NN.
       01 G2.
          05 B2 PIC 1(2) USAGE BIT.
          05 T2 PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO NN
           MOVE "~" TO G1
           MOVE "z" TO G2
           MOVE G1 TO G2
           DISPLAY "C1 " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G2(1:1))
           MOVE 2 TO NN
           MOVE "~" TO G1
           MOVE "z" TO G2
           MOVE G1 TO G2
           DISPLAY "C2 " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G2(1:1))
           MOVE 3 TO NN
           MOVE "~~" TO G1
           MOVE "zz" TO G2
           MOVE G1 TO G2
           DISPLAY "C3 " FUNCTION ORD(G1(1:1)) "," FUNCTION ORD(G1(2:1)) "," FUNCTION ORD(G2(1:1)) "," FUNCTION ORD(G2(2:1))
           STOP RUN.
