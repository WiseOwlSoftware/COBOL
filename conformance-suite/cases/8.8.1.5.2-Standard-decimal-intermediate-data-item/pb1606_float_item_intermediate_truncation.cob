*>--- conformance-case
*> id: pb1606_float_item_intermediate_truncation
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1606 - ISO 8.8.1.5.2 rule 1: a float operand whose value "cannot be expressed exactly in an SDIDI" is
      *> "rounded according to the rules in 11.9.11, INTERMEDIATE ROUNDING clause"; 11.9.11.2 rule 3 e): with
      *> TRUNCATION the value is rounded to the nearest value nearer to zero. WiseOwl COBOL converts a floating-point
      *> data item by its exact binary value (docs/CONFORMANCE.md, section 3), so the mode decides the 34th digit.
      *>
      *> HAND-DERIVED (exact rational arithmetic): F3 is the double nearest 0.3, 0.299999999999999988897769753748434595763...;
      *> truncated to 34 digits 0.2999999999999999888977697537484345 (NEAREST-AWAY would be ...346). Times 1000 that is
      *> 299.9999999999999888977697537484345; less the 31-digit literal 299.9999999999999888977697537484 it is
      *> .0000000000000000000000000000345.
      *> FU is 3 * 2**-48 = 1.0658141036401502788066864013671875E-14 exactly (35 digits): truncated to 34,
      *> 1.065814103640150278806686401367187E-14 (a tie rounded away or to even would be ...188). Times 1.0E+17 that is
      *> 1065.814103640150278806686401367187; less the literal 1065.814103640150278806686401367 it is
      *> .0000000000000000000000000001870.
      *> Each leg can fail on its own: a conversion that rounds the 34th digit gives ...346 and ...188.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1606T.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL
    INTERMEDIATE ROUNDING IS TRUNCATION.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F3   USAGE FLOAT-LONG VALUE 0.3.
01 FU   USAGE FLOAT-LONG VALUE 1.0658141036401502788066864013671875E-14.
01 R    PIC V9(31).
PROCEDURE DIVISION.
MAIN.
    COMPUTE R = F3 * 1000 - 299.9999999999999888977697537484
    DISPLAY "TRUNC-0.3=" R
    COMPUTE R = FU * 1.0E+17 - 1065.814103640150278806686401367
    DISPLAY "TRUNC-TIE=" R
    STOP RUN.
