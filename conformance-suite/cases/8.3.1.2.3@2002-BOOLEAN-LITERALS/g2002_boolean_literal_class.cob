      *>--- conformance-case
      *> id: g2002_boolean_literal_class
      *> rules: [2002-GR-8.3.1.2.3.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.3.3 GR2 - "Boolean literals are of the class
      *>   and category boolean." Each leg is a rule that gives a
      *>   class-boolean operand an answer an alphanumeric one would
      *>   not get:
      *> §2002:8.8.4.1.1.7 - operands of class boolean of unequal
      *>   length compare "as though the shorter operand were
      *>   extended on the right by sufficient boolean zeros". BB is
      *>   B"1010", so BB = B"101" and BB = B"10100" are TRUE (lines
      *>   R1, R2); as alphanumeric operands they would be padded with
      *>   spaces and compare unequal. BB = B"1011" is FALSE (R3, the
      *>   control that the comparison looks at values).
      *> §2002:15.39.3 RV1 - for a boolean literal LENGTH counts
      *>   boolean positions: B"101" is 3 (L1) and BX"F" is 4 (L2,
      *>   §2002:8.3.1.2.3.3 GR4/GR5: F is B"1111"); an alphanumeric
      *>   reading of BX"F" would count 1 position.
      *> A VALUE B"1010" on a PIC 1(4) USAGE BIT item and a MOVE of
      *>   BX"C" to it display as boolean digits (V1, M1: C is
      *>   B"1100").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOOL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BB  PIC 1(4) USAGE BIT VALUE B"1010".
       01  BM  PIC 1(4) USAGE BIT VALUE B"0000".
       01  L   PIC 9(3).
       PROCEDURE DIVISION.
           IF BB = B"101"
               DISPLAY "R1 EQUAL"
           ELSE
               DISPLAY "R1 UNEQUAL"
           END-IF.
           IF BB = B"10100"
               DISPLAY "R2 EQUAL"
           ELSE
               DISPLAY "R2 UNEQUAL"
           END-IF.
           IF BB = B"1011"
               DISPLAY "R3 EQUAL"
           ELSE
               DISPLAY "R3 UNEQUAL"
           END-IF.
           MOVE FUNCTION LENGTH(B"101") TO L.
           DISPLAY "L1 " L.
           MOVE FUNCTION LENGTH(BX"F") TO L.
           DISPLAY "L2 " L.
           DISPLAY "V1 " BB.
           MOVE BX"C" TO BM.
           DISPLAY "M1 " BM.
           STOP RUN.
