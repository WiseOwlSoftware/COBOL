      *>--- conformance-case
      *> id: pb3830-compute-zero-binds-inward
      *> rules: [GR-14.5.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R2-1. ISO 8.8.1.1: "An arithmetic
      *> expression may be ... the figurative constant ZERO (ZEROS, ZEROES)"
      *> (cite.py --check 8.8.1.1 -> OK), so the inner COMPUTE X = ZERO is a
      *> Format 1 COMPUTE that may take NOT ON SIZE ERROR (14.9.8.2), exactly
      *> as COMPUTE X = 0 may. ISO 14.5.3.3: "Any phrase encountered is the
      *> next phrase of the most-recently preceding unterminated statement
      *> with which that phrase may be syntactically associated" (cite.py
      *> --check 14.5.3.3 -> OK), so the phrase is the COMPUTE's; with no
      *> END-COMPUTE the COMPUTE is then a conditional statement (14.5.1:
      *> "Any statement with a conditional phrase that is not terminated by
      *> its explicit scope terminator is a conditional statement", cite.py
      *> OK) inside the ADD's imperative-statement. Refused COBOL2796 at
      *> every edition. Before the fix the phrase silently went to the ADD
      *> and the program printed F=-, while COMPUTE X = 0 was refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F    PIC X VALUE "-".
       01  N    PIC 9 VALUE 9.
       01  X    PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N
               ON SIZE ERROR COMPUTE X = ZERO
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD
           DISPLAY "F=" F
           STOP RUN.
