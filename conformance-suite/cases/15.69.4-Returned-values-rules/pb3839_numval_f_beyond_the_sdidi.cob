*>--- conformance-case
*> id: pb3839_numval_f_beyond_the_sdidi
*> rules: [RV-15.69.4-2, GR-14.7.5-5, GR-14.7.5-L2.1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3839 - the range of NUMVAL-F's representation. 15.69.3 r1 admits an exponent of up to four digits and sets no
      *> bound on the magnitude under native arithmetic (15.95.4 r1 b 6 reports a magnitude past the intermediate
      *> data item's capacity only for the STANDARD modes), and 15.69.4 r2 makes the native returned value an
      *> approximation whose characteristics and representation 15.4.1 leaves to the implementor. WiseOwl COBOL's
      *> representation is the exact parse on the standard-decimal intermediate (docs/CONFORMANCE.md, item 92), whose
      *> range ends at an adjusted exponent of 6144, so NUMVAL-F("1E+7000") is outside the intermediate data item the
      *> implementor chose. 14.7.5 case 5 makes that the size error condition: "if native arithmetic is in effect
      *> and the implementor defines that the range of values allowed for the intermediate data item is to be
      *> checked, when an arithmetic operation on the intermediate data item would cause the new value to be outside
      *> of the allowed range" - and with ON SIZE ERROR written, 14.7.5 rule 1 leaves the resultant UNCHANGED.
      *> S1 fails on a compiler that returns a saturated or binary64 value (a size error raised only by the
      *> receiver's capacity, or none at all for a floating-point receiver); S2 is the in-range control.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3839NVFRANGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R4   PIC S9(3)V9(4) SIGN LEADING SEPARATE VALUE 7.
       01 F1   COMP-2 VALUE 5.
       PROCEDURE DIVISION.
           COMPUTE R4 = FUNCTION NUMVAL-F("1E+7000")
               ON SIZE ERROR DISPLAY "S1=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "S1=NO-SIZE-ERROR"
           END-COMPUTE.
           DISPLAY "R1=" R4.
           COMPUTE F1 = FUNCTION NUMVAL-F("1E+7000")
               ON SIZE ERROR DISPLAY "S3=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "S3=NO-SIZE-ERROR"
           END-COMPUTE.
           DISPLAY "R3=" F1.
           COMPUTE R4 = FUNCTION NUMVAL-F("1E+2")
               ON SIZE ERROR DISPLAY "S2=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "S2=NO-SIZE-ERROR"
           END-COMPUTE.
           DISPLAY "R2=" R4.
           STOP RUN.
       END PROGRAM PB3839NVFRANGE.
