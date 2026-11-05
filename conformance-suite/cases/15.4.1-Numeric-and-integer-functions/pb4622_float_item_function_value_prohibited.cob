*>--- conformance-case
*> id: pb4622_float_item_function_value_prohibited
*> rules: [RV-15.4.1-1, RV-15.7.4-1, GR-14.7.4.3-7]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4622 - ROUNDED MODE IS PROHIBITED asks whether the RETURNED VALUE is representable in the receiver, and the value
      *> ABS returns for a float item is the item's binary64, exactly.
      *>
      *> ISO 14.7.4.3 r7: "If the PROHIBITED phrase is specified, and the arithmetic value cannot be represented exactly in the
      *> resultant identifier, the EC-SIZE-TRUNCATION exception condition is set to exist, the size error condition exists, and the
      *> content of the resultant identifier is unchanged." A1 is FLOAT-LONG 0.1, the binary64 0.1000000000000000055511151231257827...,
      *> which PIC S9V9(19) cannot hold exactly: `COMPUTE R ROUNDED MODE IS PROHIBITED = A1` raises the size error. ABS (A1)
      *> returns the same binary64 (15.7.4 r1 a: the value of argument-1 when it is zero or positive), so it raises it too.
      *> Landed by the 17-digit shortest decimal 0.1 the value looked representable and NOT ON SIZE ERROR was taken (R stored .1).
      *> A function of an approximation only (ABS (FUNCTION EXP10 (-6))) has no exact binary value to be inexact about: its
      *> approximation is the decimal 0.000001, which S9V9(19) holds, so it is stored and NOT ON SIZE ERROR is taken.
      *>
      *> --check validated:
      *>   cite.py --check 14.7.4.3 "If the PROHIBITED phrase is specified, and the arithmetic value cannot be represented exactly in the resultant identifier" -> OK
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4622PROH.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R    PIC S9V9(19) VALUE 0.
01 ER   PIC +9.9(19).
01 A1   USAGE FLOAT-LONG VALUE 0.1.
PROCEDURE DIVISION.
    COMPUTE R ROUNDED MODE IS PROHIBITED = A1
        ON SIZE ERROR DISPLAY "P1 SIZE ERROR"
        NOT ON SIZE ERROR DISPLAY "P1 STORED"
    END-COMPUTE
    COMPUTE R ROUNDED MODE IS PROHIBITED = FUNCTION ABS (A1)
        ON SIZE ERROR DISPLAY "P2 SIZE ERROR"
        NOT ON SIZE ERROR DISPLAY "P2 STORED"
    END-COMPUTE
    MOVE R TO ER
    DISPLAY "P3 " ER
    COMPUTE R ROUNDED MODE IS PROHIBITED = FUNCTION ABS (FUNCTION EXP10 (-6))
        ON SIZE ERROR DISPLAY "P4 SIZE ERROR"
        NOT ON SIZE ERROR DISPLAY "P4 STORED"
    END-COMPUTE
    MOVE R TO ER
    DISPLAY "P5 " ER
    STOP RUN.
END PROGRAM PB4622PROH.
