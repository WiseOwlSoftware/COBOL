*>--- conformance-case
*> id: pb3954_exact_quotient_places_below_2014
*> rules: [RV-15.60.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3954 - THE EDITION EDGE of the 2014 positive
      *> conformance-suite/cases/15.60.4-Returned-value-rule/pb3954_exact_quotient_places.
      *> That positive carries a leg under the ROUNDED MODE IS phrase, a COBOL-2014 introduction (14.7.4.2): at --std 2002 a
      *> bare ROUNDED is all a receiving statement can write, so the phrase alone is refused, COBOL0803, and the program
      *> below is refused at 2002 whatever FUNCTION MEAN returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3954QUONEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2  PIC S9(3)V9(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R2 ROUNDED MODE IS TRUNCATION = FUNCTION MEAN(1 2 3) / 3 * 3.
           STOP RUN.
