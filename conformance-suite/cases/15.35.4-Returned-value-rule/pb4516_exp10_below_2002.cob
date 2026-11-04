*>--- conformance-case
*> id: pb4516_exp10_below_2002
*> rules: [RV-15.35.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4516 - the refused twin of pb4516_function_value_lands_one_way. FUNCTION EXP10 is a COBOL-2002
      *> introduction (WiseOwl's catalog window, IntrinsicCatalog EXP10 IntroducedIn 2002), so below 2002 the
      *> reference is refused BY NAME with COBOL1502 and no returned value exists to land.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4516BLW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(3)V9(6) VALUE 0.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP10 (-6)
           STOP RUN.
