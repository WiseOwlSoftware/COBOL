*>--- conformance-case
*> id: pb4592_function_value_rounded_in_the_intermediate_mode_85
*> rules: [GR-8.8.1.2-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4592 (review round 1, R1-1): the refused twin of pb4592_function_value_rounded_in_the_intermediate_mode. The program is
      *> witnessed with floating-point numeric literals (1.0E0), which COBOL-2002 introduced (ISO 8.3.3.3.3 1)), so under --std 85 it is
      *> refused at the literal and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4592RN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 3333333333333333333333333333333.
       01 R PIC 9(31).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION SUM (A * A * A, 1.0E0) / 10 ** 58
           DISPLAY "SUMP   " R
           STOP RUN.
       END PROGRAM PB4592RN85.
