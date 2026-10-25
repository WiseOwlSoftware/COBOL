*>--- conformance-case
*> id: pb3666_boolean_receiver_converts_characters
*> rules: [GR-14.9.25.4-6, GR-13.18.32.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3666, docs/CONFORMANCE.md D-B5: THE CHARACTERS A BOOLEAN ITEM RECEIVES FROM A CHARACTER SENDER.
*> Table 16 (14.9.25.3) lets an alphanumeric or a national operand be moved to a boolean receiver, and
*>   cite.py --check 14.6.8.6 "after any necessary conversion, into the corresponding boolean positions of the receiving data item"
*>   -> OK  14.6.8.6   (Receiving data items of category boolean)
*> but no rule converts a character that is not '0' or '1': a boolean character is "the value zero or one"
*>   cite.py --check 3.21 "unit of information that consists of the value zero or one"
*>   -> OK  3.21
*> and a later reference to non-boolean content is undefined (14.6.13.2 1)). The standard leaves the conversion to
*> the implementor, so WiseOwl COBOL converts EACH transferred character: '1' to boolean one, every other character
*> (a '0', a letter, a space) to boolean zero - the rule the boolean operators and ACCEPT already apply - and then
*> aligns with zero fill or truncation to the right (14.6.8.6), JUSTIFIED to the left (13.18.32). DISPLAY of the item
*> and the operators over it therefore agree for both usages. Before the fix MOVE "AB" TO a PIC 1(5) item displayed
*> AB000 while B-NOT of the same item gave 11111.
*>   cite.py --check 13.18.32.4 "the data is aligned at the rightmost character position or boolean position in the data item with zero fill for the leftmost boolean positions"
*>   -> OK  13.18.32.4 2)
*> The receivers: BD / BB are PIC 1(5) USAGE DISPLAY / USAGE BIT; DJ / BJ are the same with JUSTIFIED RIGHT.
*> EXPECTED OUTPUT (the determination, applied; each line prints BD BB DJ BJ):
*>   L1  "AB" converts to 00 and fills to 00000 everywhere; B-NOT of it is 11111 for both usages.
*>   L2  "1A0B1" converts to 10001 (full width, so JUSTIFIED aligns nothing); B-NOT BD = 01110; BD B-AND BB = 10001.
*>   L3  "101" is exact: left aligned 10100, JUSTIFIED 00101.
*>   L4  "B1" converts to 01: 01000, JUSTIFIED 00001.
*>   L5  "1 1" - the space is a character that is not '1', so boolean zero - converts to 101: 10100, JUSTIFIED 00101.
*>   L6  the national N"A1A" converts to 010 (14.9.25.4 GR6, the national character '1' is the boolean one): 01000, 00010.
*>   L7  a REFERENCE-MODIFIED receiver: "1A1" into positions 2:3 of 11111 gives 1 101 1 = 11011, for both usages
*>       (a boolean slice is a boolean item, 8.4.3.3.4 GR6).
*>   L8  STRING "1A1" into BD (11111): GR3 a) transfers the characters "in accordance with the MOVE statement rules for
*>       alphanumeric-to-alphanumeric moves" (cite.py --check 14.9.43.4 reads exactly that) and an alphanumeric move
*>       does not convert, so the conversion is D-B5's choice for undefined content, not GR3 a)'s: 101, then the two
*>       untouched positions (GR7) - 10111.
*>   L9  a GROUP sender (14.9.25.4 GR4: "an alphanumeric to alphanumeric elementary move"): the two characters 1A into the
*>       PIC 1(5) DISPLAY BD aligns them left (spaces to the right) and converts them: 10000; JUSTIFIED DJ right aligns
*>       them (spaces to the left): 00010. Into the USAGE BIT BB the same move is GR4's "no conversion of data from one
*>       form of internal representation to another", so the receiving area is the sender's BYTES fitted to the one byte
*>       BB occupies (x"31", the character '1') and its first five bits are the item: 00110 (every bit pattern is a
*>       valid boolean value, so DISPLAY and the operators agree here without a character conversion).
*>   L10 INITIALIZE ... REPLACING BOOLEAN DATA BY an alphanumeric item transfers it by the MOVE rules (14.9.20.4 GR6):
*>       "B1" converts to 01: 01000 for both usages.
*>   L11 MOVE CORRESPONDING from an alphanumeric group item CA (1Z1) to a boolean one: each corresponding pair is an
*>       elementary MOVE (14.7.6), so 1Z1 converts to 101.
*>   L12 a GROUP sender (the national group GN holding N"101") into a USAGE NATIONAL boolean item BN: GR4 treats the move
*>       as an alphanumeric to alphanumeric one with "no conversion of data from one form of internal representation to
*>       another", so the sending BYTES (UTF-16BE pairs) fill the receiving area and BN holds the national characters
*>       101, not the characters the bytes would convert to if they were read as 8-bit characters: BN is 101 and
*>       B-NOT BN is 010. The conversion applies to characters only, never to that byte image (review R1-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3666CC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SX-AB  PIC X(2) VALUE "AB".
       01 SX-MIX PIC X(5) VALUE "1A0B1".
       01 SX-3   PIC X(3) VALUE "101".
       01 SX-B1  PIC X(2) VALUE "B1".
       01 SX-SP  PIC X(3) VALUE "1 1".
       01 SN-3   PIC N(3) VALUE N"A1A".
       01 GA.
          05 GA1 PIC X VALUE "1".
          05 GA2 PIC X VALUE "A".
       01 GC.
          05 CA PIC X(3) VALUE "1Z1".
       01 GT.
          05 CA PIC 1(3).
       01 GN.
          05 GN1 PIC N(3) VALUE N"101".
       01 BN PIC 1(3) USAGE NATIONAL.
       01 RV PIC 1(3) USAGE NATIONAL.
       01 BD PIC 1(5).
       01 BB PIC 1(5) USAGE BIT.
       01 DJ PIC 1(5) JUSTIFIED RIGHT.
       01 BJ PIC 1(5) USAGE BIT JUSTIFIED RIGHT.
       01 RN PIC 1(5).
       01 RM PIC 1(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SX-AB TO BD BB DJ BJ
           DISPLAY "L1 " BD " " BB " " DJ " " BJ
           COMPUTE RN = B-NOT BD
           COMPUTE RM = B-NOT BB
           DISPLAY "L1 not " RN " " RM
           MOVE SX-MIX TO BD BB DJ BJ
           DISPLAY "L2 " BD " " BB " " DJ " " BJ
           COMPUTE RN = B-NOT BD
           COMPUTE RM = BD B-AND BB
           DISPLAY "L2 not " RN " and " RM
           MOVE SX-3 TO BD BB DJ BJ
           DISPLAY "L3 " BD " " BB " " DJ " " BJ
           MOVE SX-B1 TO BD BB DJ BJ
           DISPLAY "L4 " BD " " BB " " DJ " " BJ
           MOVE SX-SP TO BD BB DJ BJ
           DISPLAY "L5 " BD " " BB " " DJ " " BJ
           MOVE SN-3 TO BD BB DJ BJ
           DISPLAY "L6 " BD " " BB " " DJ " " BJ
           MOVE B"11111" TO BD BB
           MOVE "1A1" TO BD (2:3)
           MOVE "1A1" TO BB (2:3)
           DISPLAY "L7 " BD " " BB
           MOVE B"11111" TO BD
           STRING "1A1" DELIMITED BY SIZE INTO BD
           DISPLAY "L8 " BD
           MOVE GA TO BD
           MOVE GA TO DJ
           MOVE GA TO BB
           DISPLAY "L9 " BD " " DJ " " BB
           INITIALIZE BD BB REPLACING BOOLEAN DATA BY SX-B1
           DISPLAY "L10 " BD " " BB
           MOVE CORRESPONDING GC TO GT
           DISPLAY "L11 " CA OF GT
           MOVE GN TO BN
           COMPUTE RV = B-NOT BN
           DISPLAY "L12 " BN " not " RV
           STOP RUN.
