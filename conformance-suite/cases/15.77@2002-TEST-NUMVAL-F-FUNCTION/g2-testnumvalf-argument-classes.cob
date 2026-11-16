      *>--- conformance-case
      *> id: g2-testnumvalf-argument-classes
      *> rules: [2002-AR-15.77.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.77.2 AR1 - "Argument-1 shall be an alphanumeric or
      *>   national literal or a data item of class alphanumeric or
      *>   national." Each admitted form is written once and must be
      *>   ACCEPTED and evaluated; numeric-edited W-E is class
      *>   alphanumeric (§2002:8.5.2 Table 3). RV1 (§2002:15.77.3):
      *>   0 when the content conforms to NUMVAL-F (§2002:15.56.2 AR1),
      *>   else the position of the first character in error:
      *>   LIT    0  alphanumeric literal with exponent
      *>   NATLIT 0  national literal with exponent
      *>   ITEM   0  PIC X item " 12.5", no exponent
      *>   EDIT   0  numeric-edited " 12.34": class alnum
      *>   NAT    0  PIC N item "1.5E+3"
      *>   BAD    4  "1.5X": the X at position 4 is in error
      *> Refusals beside it: a PIC A item (class alphabetic), a PIC 9
      *> item and a PIC S9(3) USAGE NATIONAL item (class numeric in
      *> 2002, §2002:8.5.2.9 item 1, kb/Work PB5625 reading B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TNF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-X   PIC X(8) VALUE " 12.5".
       01  W-E   PIC ZZ9.99.
       01  W-N   PIC N(8) VALUE N"1.5E+3".
       01  W-B   PIC X(4) VALUE "1.5X".
       01  R     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 12.34 TO W-E
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1.5E+3")
           DISPLAY "LIT=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(N"-2.5E-1")
           DISPLAY "NATLIT=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W-X)
           DISPLAY "ITEM=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W-E)
           DISPLAY "EDIT=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W-N)
           DISPLAY "NAT=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W-B)
           DISPLAY "BAD=" R
           STOP RUN.
