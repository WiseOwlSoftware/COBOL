*>--- conformance-case
*> id: pb4635-convert-any-parenthesized-boolean-item
*> rules: [AR-15.19.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R2-1 - ISO §15.19.3 r7: "When the source-format is ANY, argument-1 shall be of any usage, except index,
      *> message-tag, object reference, pointer, function-pointer or program-pointer. It is not necessary for the contents to be
      *> valid according to the usage" (cite.py --check OK): CONVERT reads the argument's STORAGE. An item of class boolean
      *> written in grouping parentheses is "a boolean expression enclosed in parentheses" (§8.8.2), which §8.4.3.2.3 r8 admits
      *> as an argument; the parentheses leave the item's own bits or characters as they are, so each parenthesized spelling
      *> converts the same storage as the bare one.
      *>
      *> EXPECTED VALUES. BT is four bits 1010, packed high-order first into one byte padded with zero bits: 1010 0000 = A0.
      *> NB is usage national, so each of its four positions is one UTF-16BE character: "1" 0031, "0" 0030, "1" 0031,
      *> "0" 0030. BG is a group-usage-bit group of two two-bit leaves 10 and 10, four bits 1010 = A0. BE(1) is the bits
      *> 0011, packed 0011 0000 = 30. BT(1:2) is the first two bits, 10, packed 1000 0000 = 80.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4635CONVANYPAREN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BT    PIC 1(4) USAGE BIT VALUE B"1010".
       01 NB    PIC 1(4) USAGE NATIONAL VALUE B"1010".
       01 BG    GROUP-USAGE BIT.
          05 B1 PIC 1(2) VALUE B"10".
          05 B2 PIC 1(2) VALUE B"10".
       01 BEG.
          05 BE PIC 1(4) USAGE BIT OCCURS 2 VALUE B"0011".
       PROCEDURE DIVISION.
           DISPLAY "BT=" FUNCTION CONVERT(BT ANY ANUM HEX)
                   " " FUNCTION CONVERT((BT) ANY ANUM HEX).
           DISPLAY "NB=" FUNCTION CONVERT(NB ANY ANUM HEX)
                   " " FUNCTION CONVERT((NB) ANY ANUM HEX).
           DISPLAY "BG=" FUNCTION CONVERT(BG ANY ANUM HEX)
                   " " FUNCTION CONVERT((BG) ANY ANUM HEX).
           DISPLAY "BE=" FUNCTION CONVERT(BE(1) ANY ANUM HEX)
                   " " FUNCTION CONVERT((BE(1)) ANY ANUM HEX).
           DISPLAY "RM=" FUNCTION CONVERT(BT(1:2) ANY ANUM HEX)
                   " " FUNCTION CONVERT((BT(1:2)) ANY ANUM HEX).
           STOP RUN.
