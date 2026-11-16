      *>--- conformance-case
      *> id: g2002_concat_class_figurative_pairs
      *> rules: [2002-GR-8.8.3.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.3 GR1 - the class of a concatenation is a) that of
      *> the other operand when one operand is a figurative constant, b)
      *> "when both of the operands are figurative constants, the class
      *> alphanumeric", or c) the class of the operands.
      *> VALUE legs: §2002:13.16.61.2 SR5 admits only national literals
      *> for a national item, so W-NQ and W-QN (N"X" & QUOTE, QUOTE &
      *> N"X") are accepted only as national (a) and W-NC (N"A" & N"B")
      *> as national (c); W-AC, W-AZ and the CD record fields F-FF
      *> (SPACE & QUOTE, b) and F-NC (c) are alphanumeric. Values per
      *> §2002:8.8.3.3 GR2.
      *> BYTE-LENGTH legs (§2002:15.12.3 RV1): a national character is
      *> one UTF-16 code unit of two bytes (NW=Y), so a two-character
      *> national result has the byte length of N"XY" and an
      *> alphanumeric one that of "XY": BA, BA2 (a) and BC (c) national;
      *> BB (b) and BC2 (c) alphanumeric.
      *> WHY EACH LEG CAN FAIL: a figurative keeping its own class
      *> refuses W-NQ/W-QN or answers ALN in BA/BA2; a figurative pair
      *> typed national answers NAT in BB; a wrong value shows in
      *> C1..B2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-AC  PIC X(6)  VALUE "AB" & "CD".
       01 W-NC  PIC N(2)  VALUE N"A" & N"B".
       01 W-NQ  PIC N(2)  VALUE N"X" & QUOTE.
       01 W-QN  PIC N(2)  VALUE QUOTE & N"X".
       01 W-AZ  PIC X(2)  VALUE "Q" & ZERO.
       01 W-X6  PIC X(6).
       COMMUNICATION SECTION.
       CD CIN FOR INPUT.
       01 CREC.
          05 F-FF    PIC X(2)  VALUE SPACE & QUOTE.
          05 F-NC    PIC X(4)  VALUE "AB" & "C" & SPACE.
          05 FILLER  PIC X(81).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "C1=[" W-AC "]".
           DISPLAY "C2=[" W-NC "]".
           DISPLAY "A1=[" W-NQ "]".
           DISPLAY "A2=[" W-QN "]".
           DISPLAY "A3=[" W-AZ "]".
           DISPLAY "B1=[" F-FF "]".
           DISPLAY "C3=[" F-NC "]".
           MOVE SPACE & QUOTE TO W-X6.
           DISPLAY "B2=[" W-X6 "]".
           IF FUNCTION BYTE-LENGTH(N"XY") > FUNCTION BYTE-LENGTH("XY")
              DISPLAY "NW=Y"
           ELSE
              DISPLAY "NW=N"
           END-IF.
           IF FUNCTION BYTE-LENGTH(N"X" & QUOTE)
              = FUNCTION BYTE-LENGTH(N"XY")
              DISPLAY "BA=NAT"
           ELSE
              DISPLAY "BA=ALN"
           END-IF.
           IF FUNCTION BYTE-LENGTH(QUOTE & N"X")
              = FUNCTION BYTE-LENGTH(N"XY")
              DISPLAY "BA2=NAT"
           ELSE
              DISPLAY "BA2=ALN"
           END-IF.
           IF FUNCTION BYTE-LENGTH(SPACE & QUOTE)
              = FUNCTION BYTE-LENGTH("XY")
              DISPLAY "BB=ALN"
           ELSE
              DISPLAY "BB=NAT"
           END-IF.
           IF FUNCTION BYTE-LENGTH(N"A" & N"B")
              = FUNCTION BYTE-LENGTH(N"XY")
              DISPLAY "BC=NAT"
           ELSE
              DISPLAY "BC=ALN"
           END-IF.
           IF FUNCTION BYTE-LENGTH("A" & "B")
              = FUNCTION BYTE-LENGTH("XY")
              DISPLAY "BC2=ALN"
           ELSE
              DISPLAY "BC2=NAT"
           END-IF.
           STOP RUN.
