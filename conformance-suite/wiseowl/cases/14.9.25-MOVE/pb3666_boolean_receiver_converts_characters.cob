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
*>   L13 kb/Work PB3697: a GROUP sender GA4 holding the alphanumeric "A1Z1" into the USAGE NATIONAL boolean item BQ
*>       (PIC 1(4)). GR4 fills BQ's storage with the sending BYTES, unconverted (x41 x31 x5A x31 and four spaces fit its
*>       eight bytes); BQ's characters are those bytes read as UTF-16BE pairs (x4131 x5A31 x2020 x2020), none of which is
*>       the national '1', so D-B5 converts each to boolean zero: BQ is 0000 and B-NOT BQ is 1111. Left unconverted
*>       the four CJK characters would sit in the item, and DISPLAY, a comparison and the operators would disagree.
*>   L14 a READ of the eight characters A1Z1B101 into an ELEMENTARY boolean record area RB (PIC 1(8)): 14.9.30.4 GR13 c)
*>       makes the record "available in the record area", the same storage-area store as the group move, so the
*>       characters convert: A 1 Z 1 B 1 0 1 give 01010101 and B-NOT RB is 10101010. A second READ finds the file at
*>       its end, so the AT END phrase runs and no store is made.
*>   L15 the same record READ into the USAGE NATIONAL boolean record RQ (PIC 1(4), eight bytes): its four pairs are
*>       x4131 x5A31 x4231 x3031, none the national '1', so RQ is 0000 and B-NOT RQ is 1111.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3666CC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "PB3666L14.DAT".
           SELECT FB ASSIGN TO "PB3666L14.DAT".
           SELECT FQ ASSIGN TO "PB3666L14.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 RA PIC X(8).
       FD FB.
       01 RB PIC 1(8).
       FD FQ.
       01 RQ PIC 1(4) USAGE NATIONAL.
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
       01 GA4.
          05 GA4-1 PIC X(4) VALUE "A1Z1".
       01 BQ PIC 1(4) USAGE NATIONAL.
       01 RB8 PIC 1(8).
       01 RQ4 PIC 1(4) USAGE NATIONAL.
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
           MOVE GA4 TO BQ
           COMPUTE RQ4 = B-NOT BQ
           DISPLAY "L13 " BQ " not " RQ4
           OPEN OUTPUT FA
           MOVE "A1Z1B101" TO RA
           WRITE RA
           CLOSE FA
           OPEN INPUT FB
           READ FB
           DISPLAY "L14 " RB
           COMPUTE RB8 = B-NOT RB
           DISPLAY "L14 not " RB8
           READ FB AT END DISPLAY "L14 at end"
           CLOSE FB
           OPEN INPUT FQ
           READ FQ
           DISPLAY "L15 " RQ
           COMPUTE RQ4 = B-NOT RQ
           DISPLAY "L15 not " RQ4
           CLOSE FQ
           STOP RUN.
