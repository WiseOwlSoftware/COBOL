*>--- conformance-case
*> id: pb5575-rt-std-arith-prohibited-exact-32-digits
*> rules: [GR-8.8.1.5.2-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5575 review finding R2-1. Under INTERMEDIATE ROUNDING IS PROHIBITED (§2014:8.8.1.3.1.3.3) no 31-digit rounding
*> situation exists, and a standard intermediate data item holds 32 digits: B + C is 1.0000000000000000000000000000005
*> (32 digits), EXACT, so it is no size error. The relation B + C > B is true, and the store into PIC 9V9(30) without
*> ROUNDED truncates it to 1.000000000000000000000000000000 with no size error.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5575P.
OPTIONS.
    ARITHMETIC IS STANDARD
    INTERMEDIATE ROUNDING IS PROHIBITED.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B PIC 9V9(30) VALUE 1.
01 C PIC V9(31) VALUE .0000000000000000000000000000005.
01 R PIC 9V9(30) VALUE 0.
01 F PIC X VALUE "N".
PROCEDURE DIVISION.
    COMPUTE R = B + C ON SIZE ERROR MOVE "Y" TO F END-COMPUTE.
    DISPLAY "R=" R " SIZE=" F.
    IF B + C > B DISPLAY "GT" ELSE DISPLAY "NOT-GT" END-IF.
    STOP RUN.
