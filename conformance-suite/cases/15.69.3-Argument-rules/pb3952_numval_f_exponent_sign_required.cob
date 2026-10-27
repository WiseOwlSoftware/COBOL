*>--- conformance-case
*> id: pb3952_numval_f_exponent_sign_required
*> rules: [AR-15.69.3-1, RV-15.95.4-1, DOC-A.1-90]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3952 - ISO 15.69.3 rule 1: the exponent of a NUMVAL-F argument is `E [space-string] {+|-}
      *> [space-string] n`, the sign in BRACES (a required choice, verified on printed page 898), unlike the
      *> optional leading sign. "1.5E2" is therefore NOT a conforming argument. GnuCOBOL's NUMVAL-F is lenient
      *> and reads it as 150; that is not the standard (15.69.3 r1), and WiseOwl COBOL follows the standard
      *> here, because the two functions must agree: 15.95.1 says TEST-NUMVAL-F "verifies that the contents of
      *> argument-1 conform to the specification for argument-1 of the NUMVAL-F function", and its 15.95.4 r1
      *> names the first character in error - the digit where the sign belongs (position 5 of "1.5E2", position
      *> 3 of "1E2"). A NUMVAL-F reference with that argument sets EC-ARGUMENT-FUNCTION (15.3 r14), checking is
      *> off here, and the implementor defines the result: DOC-A.1-90's zero value of the numeric type, 0.
      *> Each leg can fail alone: R1/R2/R3 read 150, 150 and 100 if the scan admitted an unsigned exponent (the
      *> GnuCOBOL reading); T1/T2/T3 read 000 if TEST-NUMVAL-F admitted it; C1/C2 are the signed controls
      *> that must keep their values (+150 and +0.015) with a TEST-NUMVAL-F of 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3952NVFEXP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(8) VALUE "1.5E2".
       01 A2 PIC X(8) VALUE "1.5e2".
       01 A3 PIC X(8) VALUE "1E2".
       01 A4 PIC X(8) VALUE "1.5E+2".
       01 A5 PIC X(8) VALUE "1.5e-2".
       01 R  PIC S9(5)V9(3) SIGN LEADING SEPARATE.
       01 T  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION NUMVAL-F(A1)
           DISPLAY "R1=" R
           COMPUTE T = FUNCTION TEST-NUMVAL-F(A1)
           DISPLAY "T1=" T
           COMPUTE R = FUNCTION NUMVAL-F(A2)
           DISPLAY "R2=" R
           COMPUTE T = FUNCTION TEST-NUMVAL-F(A2)
           DISPLAY "T2=" T
           COMPUTE R = FUNCTION NUMVAL-F(A3)
           DISPLAY "R3=" R
           COMPUTE T = FUNCTION TEST-NUMVAL-F(A3)
           DISPLAY "T3=" T
           COMPUTE R = FUNCTION NUMVAL-F(A4)
           DISPLAY "C1=" R
           COMPUTE T = FUNCTION TEST-NUMVAL-F(A4)
           DISPLAY "U1=" T
           COMPUTE R = FUNCTION NUMVAL-F(A5)
           DISPLAY "C2=" R
           COMPUTE T = FUNCTION TEST-NUMVAL-F(A5)
           DISPLAY "U2=" T
           STOP RUN.
       END PROGRAM PB3952NVFEXP.
