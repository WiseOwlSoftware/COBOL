*>--- conformance-case
*> id: pb4622_float_item_function_below_2002
*> rules: [RV-15.4.1-1, RV-15.7.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622 - the refused twin of pb4622_float_item_function_value_exact. USAGE FLOAT-LONG is a COBOL-2002
      *> introduction, so below 2002 the data description is refused and no float item exists for ABS to return the
      *> content of.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4622BLW.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X5   PIC 9(5).
01 DN   USAGE FLOAT-LONG VALUE -2.5E+40.
PROCEDURE DIVISION.
    MOVE FUNCTION ABS (DN) TO X5
    STOP RUN.
END PROGRAM PB4622BLW.
