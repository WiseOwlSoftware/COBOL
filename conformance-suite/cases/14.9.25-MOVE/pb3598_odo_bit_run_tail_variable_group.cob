*>--- conformance-case
*> id: pb3598_odo_bit_run_tail_variable_group
*> rules: [GR-14.9.25.4-9]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3598 - THE IMPLICIT FILLER AFTER AN OCCURS DEPENDING BIT RUN TRAVELS IN A VARIABLE-LENGTH GROUP MOVE AT A COUNT
      *> BELOW THE MAXIMUM.  Each group holds a dynamic-length item, so it is a variable-length group (8.5.1.12.1) and ISO 14.9.25.4
      *> GR9 a) governs the move: "The content of each character position that is not occupied by a corresponding table is moved to
      *> the corresponding character position in the receiving group".  No table here is a corresponding table (8.5.1.12.2: "Two
      *> tables correspond if at least one of them is a dynamic-capacity table"; these are OCCURS DEPENDING tables), so GR9 a) moves
      *> every character position, the filler included.  The count NS / NR is a data item OUTSIDE the groups, so 13.18.38.4 GR8 a)
      *> applies: "only that part of the table area that is specified by the value of the data item referenced by data-name-1 at the
      *> start of the operation will be used", and 13.18.38.4 7) says "The content of a data item whose occurrence number exceeds
      *> the value of the data item referenced by data-name-1 is undefined".  So the bits of an occurrence past the count are
      *> IMPLEMENTOR LATITUDE, and the standard does not decide them: this implementation keeps the receiver's (as it has since
      *> kb/Work PB3030), where the fixed-length lane (pb3598_odo_bit_run_tail_travels_with_the_group) moves whole characters.  The
      *> expected values below spell that choice for T(4) (V1, V4) and T(2) (V3), and they are not the standard's; only the
      *> filler bits are bound by the rule above.
      *>
      *> The group is B PIC 1(2) and T PIC 1(3) OCCURS 1 TO 4: its maximum extent is 2 + 12 = 14 bits, two characters, and the two
      *> bits past T(4) are 8.5.1.6.3's implicit filler ("as needed to increase the number of bits to fill an integral number of
      *> characters"), which no occurrence owns, so no count makes them an unused occurrence's: at a count whose last character
      *> reaches them they are moved with that character.  The receiving store used to keep the receiver's bits from the first
      *> unused occurrence to the END of the member, so it dropped them.
      *>
      *> DERIVATION.  The sender S holds B = 01, T(1..4) = 101 010 011 111 and the filler 10 (INITIALIZE ... WITH FILLER, then each
      *> item): at count 3 its two characters are 01 101 010 = 0x6A "j" and 011 111 10 = 0x7E "~".  The receiver R holds zeros but
      *> T(3) = 011: its second character is 011 000 00 = 0x60 "`".  Every case sets the receiver's count and reads R at count 3.
      *>   V1  NR = 3: B, T(1..3) come from S, T(4) stays R's (000), the filler is S's (10): "j" then 011 000 10 = 0x62 "b".  [ajb]
      *>   V2  NR = 2: the extent is 8 bits, one character, all used: "j"; the second character is R's own, "`".          [aj`]
      *>   V3  NR = 1: the extent is 5 bits; T(2) starts at bit 5 inside the first character and keeps R's bits (000): 01101 000
      *>       = 0x68 "h"; the second character is R's own.                                                                [ah`]
      *>   V4  the same as V1 into a CELL-BACKED group (ADDRESS OF taken), which stores through the same splice.            [ajb]
      *> The leading "a" is the dynamic-length item, moved whole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3598VLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NS PIC 9.
       01 NR PIC 9.
       01 NC PIC 9.
       01 S.
          05 DS PIC X DYNAMIC LENGTH LIMIT 5.
          05 BS PIC 1(2) USAGE BIT.
          05 TS PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NS.
       01 R.
          05 DR PIC X DYNAMIC LENGTH LIMIT 5.
          05 BR PIC 1(2) USAGE BIT.
          05 TR PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NR.
       01 GC.
          05 DC PIC X DYNAMIC LENGTH LIMIT 5.
          05 BC PIC 1(2) USAGE BIT.
          05 TC PIC 1(3) USAGE BIT OCCURS 1 TO 4 DEPENDING ON NC.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET P TO ADDRESS OF GC
           MOVE 4 TO NS
           MOVE "a" TO DS
           INITIALIZE S WITH FILLER REPLACING BOOLEAN DATA BY B"10"
           MOVE B"01" TO BS
           MOVE B"101" TO TS (1)
           MOVE B"010" TO TS (2)
           MOVE B"011" TO TS (3)
           MOVE B"111" TO TS (4)
           MOVE 3 TO NS
           PERFORM RESET-R
           MOVE 3 TO NR
           MOVE S TO R
           DISPLAY "V1 [" R "]"
           PERFORM RESET-R
           MOVE 2 TO NR
           MOVE S TO R
           MOVE 3 TO NR
           DISPLAY "V2 [" R "]"
           PERFORM RESET-R
           MOVE 1 TO NR
           MOVE S TO R
           MOVE 3 TO NR
           DISPLAY "V3 [" R "]"
           MOVE 4 TO NC
           MOVE "b" TO DC
           INITIALIZE GC WITH FILLER REPLACING BOOLEAN DATA BY B"00"
           MOVE B"00" TO BC
           MOVE B"000" TO TC (1) TC (2) TC (4)
           MOVE B"011" TO TC (3)
           MOVE 3 TO NC
           MOVE S TO GC
           DISPLAY "V4 [" GC "]"
           STOP RUN.
       RESET-R.
           MOVE 4 TO NR
           MOVE "b" TO DR
           INITIALIZE R WITH FILLER REPLACING BOOLEAN DATA BY B"00"
           MOVE B"00" TO BR
           MOVE B"000" TO TR (1) TR (2) TR (4)
           MOVE B"011" TO TR (3).
