*>--- conformance-case
*> id: pb1606_float_item_intermediate_even
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 - ISO 8.8.1.5.2 rule 1: a float operand whose value "cannot be expressed exactly in an SDIDI" is
      *> "rounded according to the rules in 11.9.11, INTERMEDIATE ROUNDING clause"; 11.9.11.2 rule 3 c): with
      *> NEAREST-EVEN a tie goes to the value whose rightmost digit is even. WiseOwl COBOL converts a floating-point
      *> data item by its exact binary value (docs/CONFORMANCE.md, section 3), so the mode decides the 34th digit.
      *>
      *> HAND-DERIVED (exact rational arithmetic): FT is 2**-50 = 8.8817841970012523233890533447265625E-16 exactly - 35
      *> digits, the 35th a 5: an exact TIE at the 34th digit, whose kept digit 2 is EVEN, so NEAREST-EVEN keeps it,
      *> 8.881784197001252323389053344726562E-16 (NEAREST-AWAY-FROM-ZERO would round up to ...563). Times 1.0E+18 that
      *> is 888.1784197001252323389053344726562; less the 31-digit literal 888.1784197001252323389053344726 it is
      *> .0000000000000000000000000000562.
      *> FU is 3 * 2**-48 = 1.0658141036401502788066864013671875E-14 exactly - again a tie at the 34th digit, whose kept
      *> digit 7 is ODD, so NEAREST-EVEN rounds up, 1.065814103640150278806686401367188E-14. Times 1.0E+17 that is
      *> 1065.814103640150278806686401367188; less the literal 1065.814103640150278806686401367 it is
      *> .0000000000000000000000000001880.
      *> Each leg can fail on its own: the first if the tie is taken away from zero, the second if it is truncated.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606E.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL
    INTERMEDIATE ROUNDING IS NEAREST-EVEN.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FT   USAGE FLOAT-LONG VALUE 8.8817841970012523233890533447265625E-16.
01 FU   USAGE FLOAT-LONG VALUE 1.0658141036401502788066864013671875E-14.
01 R    PIC V9(31).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = FT * 1.0E+18 - 888.1784197001252323389053344726
    DISPLAY "EVEN-KEPT=" R
    COMPUTE R = FU * 1.0E+17 - 1065.814103640150278806686401367
    DISPLAY "EVEN-ODD=" R
    STOP RUN.
