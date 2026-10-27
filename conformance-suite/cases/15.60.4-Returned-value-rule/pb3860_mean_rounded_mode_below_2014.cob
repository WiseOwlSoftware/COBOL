*>--- conformance-case
*> id: pb3860_mean_rounded_mode_below_2014
*> rules: [RV-15.60.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3860 - THE EDITION EDGE of the 2014 positive
      *> conformance-suite/cases/15.60.4-Returned-value-rule/pb3860_mean_one_value.
      *> That positive tells MEAN's one value from its cut with the ROUNDED MODE IS phrase, a COBOL-2014 introduction
      *> (14.7.4.2): at --std 2002 a bare ROUNDED is all a receiving statement can write, so the phrase alone is refused,
      *> COBOL0803, and the program below is refused at 2002 whatever FUNCTION MEAN returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3860MEANNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2  PIC S9V99.
       01 A   PIC S9V9(8) VALUE 0.00000001.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R2 ROUNDED MODE IS AWAY-FROM-ZERO = FUNCTION MEAN(A 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0).
           STOP RUN.
