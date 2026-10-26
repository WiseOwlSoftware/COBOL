      *>--- conformance-case
      *> id: pb3830_compute_zero_size_error
      *> rules: [GR-14.5.3.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R2-1. ISO 8.8.1.1: "An arithmetic
      *> expression may be ... the figurative constant ZERO (ZEROS, ZEROES)"
      *> (cite.py --check 8.8.1.1 -> OK), so COMPUTE X = ZERO is a Format 1
      *> (arithmetic-compute) COMPUTE, whose general format prints the SIZE
      *> ERROR pair (14.9.8.2). ISO 14.5.3.3: "Any phrase encountered is the
      *> next phrase of the most-recently preceding unterminated statement
      *> with which that phrase may be syntactically associated" (cite.py
      *> --check 14.5.3.3 -> OK): each phrase after COMPUTE X = ZERO is the
      *> COMPUTE's. The compiler parses a bare ZERO through COMPUTE's
      *> boolean-expression alternative, and refused legs 1-3 (COBOL2269)
      *> until the phrase association asked what the source wrote.
      *> Expected values: ZERO is the numeric value 0 here (8.3.3.6.4 4),
      *> cite.py OK), which fits PIC 9(4): no size error, so NOT ON SIZE
      *> ERROR runs and X becomes 0. Leg 4: 9 + 1 is "further from zero than
      *> permitted for the associated resultant data item" PIC 9 (14.7.5
      *> 3), cite.py OK): the ADD takes ON SIZE ERROR, whose COMPUTE, closed
      *> by END-COMPUTE, takes its own NOT ON SIZE ERROR; the ADD's NOT ON
      *> SIZE ERROR follows the END-COMPUTE and is the ADD's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830CZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F    PIC X VALUE "-".
       01  N    PIC 9 VALUE 9.
       01  X    PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
      *> Leg 1: both phrases, in the standard's order.
           COMPUTE X = ZERO
               ON SIZE ERROR MOVE "S" TO F
               NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE
           DISPLAY "1 F=" F " X=" X.
      *> Leg 2: NOT ON SIZE ERROR alone, spelled ZEROES.
           MOVE "-" TO F
           MOVE 7 TO X
           COMPUTE X = ZEROES
               NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE
           DISPLAY "2 F=" F " X=" X.
      *> Leg 3: the two phrases reversed, spelled ZEROS.
           MOVE "-" TO F
           MOVE 7 TO X
           COMPUTE X = ZEROS
               NOT ON SIZE ERROR MOVE "N" TO F
               ON SIZE ERROR MOVE "S" TO F
           END-COMPUTE
           DISPLAY "3 F=" F " X=" X.
      *> Leg 4: nested in the ADD's ON SIZE ERROR, closed by END-COMPUTE.
           MOVE "-" TO F
           MOVE 7 TO X
           ADD 1 TO N
               ON SIZE ERROR
                   COMPUTE X = ZERO
                       ON SIZE ERROR MOVE "S" TO F
                       NOT ON SIZE ERROR MOVE "Z" TO F
                   END-COMPUTE
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD
           DISPLAY "4 F=" F " X=" X.
           STOP RUN.
