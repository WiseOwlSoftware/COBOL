*>--- conformance-case
*> id: pb4511_test_numval_incomplete_cr_db
*> rules: [RV-15.93.4-1]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4511 - ISO 15.93.4 r1 b): "Otherwise, if one or more characters are in error, the position of
      *> the first character in error"; r1 c) (LENGTH + 1) is the "Otherwise" of no character in error, and the
      *> NOTE names "argument-1 contains valid characters but is incomplete". The trailing sign of a NUMVAL
      *> argument is + - CR or DB (15.67.3 r1), so a C or D that opens the pair is a VALID prefix: the first
      *> character in error is the one that fails to complete it, and text that ENDS after the C or D is
      *> incomplete (LENGTH + 1). The defect returned the position of the C or D itself.
      *> Hand-derived positions (1-based):
      *>   "4.5 D"      4 . 5 sp D            -> 6  ends after the D: r1 c, LENGTH + 1
      *>   "4.5 D   "   4 . 5 sp D sp sp sp   -> 6  the space after the D cannot complete DB
      *>   "1 D B"      1 sp D sp B           -> 4  the space after the D
      *>   "12CX"       1 2 C X               -> 4  the X
      *>   "12CR"       conforming            -> 0
      *>   "-12C"       1 2 with a leading sign admits no CR/DB, so the C itself is in error -> 4
      *> TEST-NUMVAL-C rides the same scan, so its twin rows must agree. Each is tested through a literal
      *> argument AND a data item (a literal may be folded at compile time, a data item reaches the runtime scan).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4511CRDB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(5)  VALUE "4.5 D".
       01 A2 PIC X(8)  VALUE "4.5 D   ".
       01 A3 PIC X(5)  VALUE "1 D B".
       01 A4 PIC X(4)  VALUE "12CX".
       01 A5 PIC X(4)  VALUE "12CR".
       01 A6 PIC X(4)  VALUE "-12C".
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL("4.5 D")
           DISPLAY "L1=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A1)
           DISPLAY "D1=" R
           COMPUTE R = FUNCTION TEST-NUMVAL("4.5 D   ")
           DISPLAY "L2=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A2)
           DISPLAY "D2=" R
           COMPUTE R = FUNCTION TEST-NUMVAL("1 D B")
           DISPLAY "L3=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A3)
           DISPLAY "D3=" R
           COMPUTE R = FUNCTION TEST-NUMVAL("12CX")
           DISPLAY "L4=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A4)
           DISPLAY "D4=" R
           COMPUTE R = FUNCTION TEST-NUMVAL("12CR")
           DISPLAY "L5=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A5)
           DISPLAY "D5=" R
           COMPUTE R = FUNCTION TEST-NUMVAL("-12C")
           DISPLAY "L6=" R
           COMPUTE R = FUNCTION TEST-NUMVAL(A6)
           DISPLAY "D6=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C("4.5 D")
           DISPLAY "CL1=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C(A2)
           DISPLAY "CD2=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C(A3)
           DISPLAY "CD3=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C(A4)
           DISPLAY "CD4=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C(A5)
           DISPLAY "CD5=" R
           COMPUTE R = FUNCTION TEST-NUMVAL-C(A6)
           DISPLAY "CD6=" R
           STOP RUN.
       END PROGRAM PB4511CRDB.
