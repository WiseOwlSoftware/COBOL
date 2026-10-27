*>--- conformance-case
*> id: pb60_numvalf_native_channels
*> rules: [GR-14.6.8.2-1, RV-15.69.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (RV-15.69.4-2), re-derived by PB3839 - NUMVAL-F under NATIVE arithmetic: 15.69.4 r2 makes the
      *> native returned value "an approximation of the numeric value represented by argument-1" and 15.4.1
      *> leaves "the characteristics and representation of the returned value" to the implementor, so the
      *> digits are the implementor's. This compiler's determination (CONFORMANCE.md item 92) is the ONE value
      *> that is also r3's: the EXACT parse, the SDIDI NUMVAL already returns, in every channel - so a
      *> receiver can no longer change it. Before PB3839 the same reference was a binary64 in the receiver-less
      *> channels, an Int128 parse truncated at max(receiver scale, 9) for a fixed-point receiver (which then
      *> rounded the truncation AGAIN) and a binary64 for a COMP-2, so ROUNDED MODE AWAY-FROM-ZERO of
      *> NUMVAL-F("0.12340000000001") into 9V9(4) stored 0.1234 where NUMVAL stores 0.1235.
      *> RAW/RAWS/RAW1: receiver-less DISPLAY renders the exact value at its own scale, the item-92 text form
      *>        (5 x 10**30 is 31 digits; 15 x 10**-13 has 13 fraction digits). EQ/GT: a relation compares the
      *>        exact values (8.8.4.2.4): 5E+30 and 9E+30 differ.
      *> F8/F8S: a COMP-2 receiver converts the exact decimal ONCE, and COMPUTE with no ROUNDED phrase truncates
      *>        (14.7.4.3 r2, CONFORMANCE.md item 81): the nearest binary64 of 1.5E-12 lies above it, so the
      *>        truncated neighbour 1.4999999999999999E-12 is stored - exactly what COMPUTE F8 = 0.0000000000015
      *>        stores. CM: a FIXED arithmetic receiver gets the exact parse (1.5E-8 x 10**8 = 1.5, exact).
      *> MV/MVS/MV20: a MOVE sender lands the parsed decimal digit-for-digit: 1500, 0.000000015, and all twenty
      *>        digits of a 20-digit argument.
      *> ARG/SUB: as an argument to MAX and as a subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60NVFNATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9   PIC S9(9)V9(9).
       01 E9   PIC -(9)9.9(9).
       01 X5   PIC 9(5) VALUE 12345.
       01 F8   COMP-2.
       01 R20  PIC S9V9(20).
       01 E20  PIC -9.9(20).
       01 TBL.
          05 FILLER PIC X(5) VALUE "ABCDE".
       01 TBR REDEFINES TBL.
          05 EL PIC X OCCURS 5.
       PROCEDURE DIVISION.
           DISPLAY "RAW=" FUNCTION NUMVAL-F("5E+30").
           DISPLAY "RAWS=" FUNCTION NUMVAL-F("1.5E-12").
           DISPLAY "RAW1=" FUNCTION NUMVAL-F(" 1.5 ").
           IF FUNCTION NUMVAL-F("5E+30") = FUNCTION NUMVAL-F("9E+30")
               DISPLAY "EQ=TRUE"
           ELSE
               DISPLAY "EQ=FALSE"
           END-IF.
           IF FUNCTION NUMVAL-F("1.5E-12") > 0
               DISPLAY "GT=TRUE"
           ELSE
               DISPLAY "GT=FALSE"
           END-IF.
           COMPUTE F8 = FUNCTION NUMVAL-F("5E+30").
           DISPLAY "F8=" F8.
           COMPUTE F8 = FUNCTION NUMVAL-F("1.5E-12").
           DISPLAY "F8S=" F8.
           COMPUTE R9 = FUNCTION NUMVAL-F("1.5E-8") * 100000000.
           MOVE R9 TO E9.
           DISPLAY "CM=" E9.
           MOVE FUNCTION NUMVAL-F("1.5E+3") TO X5.
           DISPLAY "MV=" X5.
           MOVE FUNCTION NUMVAL-F("1.5E-8") TO R9.
           MOVE R9 TO E9.
           DISPLAY "MVS=" E9.
           MOVE FUNCTION NUMVAL-F("12345678901234567890E-20") TO R20.
           MOVE R20 TO E20.
           DISPLAY "MV20=" E20.
           COMPUTE R9 = FUNCTION MAX(FUNCTION NUMVAL-F("2.5E+1"), 3).
           MOVE R9 TO E9.
           DISPLAY "ARG=" E9.
           DISPLAY "SUB=" EL(FUNCTION NUMVAL-F("4E+0")).
           STOP RUN.
       END PROGRAM PB60NVFNATIVE.
