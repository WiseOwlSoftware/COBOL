      *>--- conformance-case
      *> id: g2002_boolean_literal_forms
      *> rules: [2002-FMT-8.3.1.2.3.1, 2002-SR-8.3.1.2.3.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.3.1 - format 1 B"..." / B'...' and format 2
      *> BX"..." / BX'...', each with one or more characters, accepted.
      *> §2002:8.3.1.2.3.2 SR1 - "shall be greater than zero and less
      *> than or equal to 160 boolean character positions": B'1' (1) and
      *> the
      *> 40-digit BX literal (40 x 4 = 160 positions, §2002:8.3.1.2.3.3
      *> GR4)
      *> are accepted; the zero-length, 161-position (format 1) and
      *> 164-position (format 2) twins are refused.
      *> §2002:8.3.1.2.3.3 GR3 and GR5 give the values: B"1010" is 1010,
      *> B'1' is 1, BX"A5" is 10100101 and BX'0F' is 00001111; the
      *> 160-bit item holds 80 ones (20 x F), 76 zeros (19 x 0) and
      *> 4 ones (a final F), so position 1 is 1, positions 80-81 are 10
      *> and position 160 is 1. A boolean receiver is zero-filled on
      *> the right (§2002:14.5.8 rule 6), so position 160 is 1 only if
      *> all 160 positions of the literal reach the item.
      *> EXPECTED, DERIVED: 1[1010][1] 2[10100101][00001111]
      *> 3[1][10][1].
      *> A wrong digit expansion or a truncated literal changes a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 B1 PIC 1(4) VALUE B"1010".
           01 B2 PIC 1(1) VALUE B'1'.
           01 B3 PIC 1(8) VALUE BX"A5".
           01 B4 PIC 1(8) VALUE BX'0F'.
           01 B160 PIC 1(160).
       PROCEDURE DIVISION.
           DISPLAY "1[" B1 "][" B2 "]"
           DISPLAY "2[" B3 "][" B4 "]"
           MOVE BX"FFFFFFFFFFFFFFFFFFFF0000000000000000000F"
               TO B160
           DISPLAY "3[" B160(1:1) "][" B160(80:2) "][" B160(160:1) "]"
           STOP RUN.
