      *>--- conformance-case
      *> id: pb4623_function_overflow_below_2002
      *> rules: [GR-14.7.5-L2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4623 - the refused twin of
      *> pb4623_function_overflow_alone_and_beside_operator. FUNCTION
      *> EXP10 is a COBOL-2002 introduction, so below 2002 the reference
      *> is refused BY NAME (COBOL1502) and no returned value exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4623BLW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC S9(9) VALUE 7.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP10 (400)
               ON SIZE ERROR DISPLAY "SIZE ERROR"
           END-COMPUTE
           STOP RUN.
