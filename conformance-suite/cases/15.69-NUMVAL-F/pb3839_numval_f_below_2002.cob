*>--- conformance-case
*> id: pb3839_numval_f_below_2002
*> rules: [RV-15.69.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> PB3839 - the negative twin of pb3839_numval_f_one_value. FUNCTION NUMVAL-F is a COBOL-2002 introduction (the
      *> catalog's edition window, the R28 research recorded beside the NUMVAL-F row), so below 2002 the D8 catalog
      *> window rejects the reference BY NAME - COBOL1502 - and no returned-value rule of 15.69.4 applies to the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3839NVF85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R4 PIC S9V9(4).
       PROCEDURE DIVISION.
           COMPUTE R4 ROUNDED = FUNCTION NUMVAL-F("0.12340000000001").
           STOP RUN.
       END PROGRAM PB3839NVF85.
