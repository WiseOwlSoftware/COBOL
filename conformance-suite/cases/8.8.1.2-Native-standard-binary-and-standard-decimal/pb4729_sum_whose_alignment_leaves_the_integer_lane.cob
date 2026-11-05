      *>--- conformance-case
      *> id: pb4729_sum_whose_alignment_leaves_the_integer_lane
      *> rules: [DOC-A.1-123, GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4729 (review round 1, R1-7): a native sum or difference of operands whose PICTURE digit bounds are known, and whose
      *> alignment to the common scale needs more than 38 digits, is exact.
      *>
      *> ISO 8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression" (cite.py: OK 8.8.1.3);
      *> 14.7.5 5): "if native arithmetic is in effect and the implementor defines that the range of values allowed for the intermediate
      *> data item is to be checked" (cite.py: OK 14.7.5 5)), and the range WiseOwl COBOL checks is the SDIDI's, |value| < 10^6145
      *> (docs/CONFORMANCE.md DOC-A.1-123, owner 2026-11-04). D = 1000000000000000001 (19 digits) beside F = 1.01 (V9(30), 31 digits) aligns D to 30 fraction digits - a
      *> 49-digit integer - which is past any 128-bit lane although D + F is the plain 1000000000000000002.01.
      *> Python integers and Decimal give every value below.
      *>   S1  R2 = D + F stores the sum's low-order nine integer digits, 000000002 (an unchecked store keeps the low-order digits)
      *>   S2  R3 = D - F = 999999999999999999.99 stores 999999999.99
      *>   S3  R1 = (D * E) + (E / H), E = 123.4567, H = 7: D * E = 123456700000000000123.4567, E / H = 17.63667142857142857...;
      *>       the sum 123456700000000000141.0933714285714... truncated at the receiver's ten places keeps its low-order 15 integer digits:
      *>       +700000000000141.0933714285
      *>   S4  the same sum as a relation: D + F > D is TRUE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4729ADD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9(19) VALUE 1000000000000000001.
       01 F  PIC 9V9(30) VALUE 1.010000000000000000000000000000.
       01 E  PIC 9(3)V9(4) VALUE 123.4567.
       01 H  PIC 9(5) VALUE 7.
       01 R2 PIC 9(9).
       01 R3 PIC 9(9)V99.
       01 R1 PIC S9(15)V9(10).
       PROCEDURE DIVISION.
           COMPUTE R2 = D + F
           DISPLAY "S1 " R2
           COMPUTE R3 = D - F
           DISPLAY "S2 " R3
           COMPUTE R1 = (D * E) + (E / H)
           DISPLAY "S3 " R1
           IF D + F > D DISPLAY "S4 TRUE" ELSE DISPLAY "S4 FALSE" END-IF
           STOP RUN.
       END PROGRAM PB4729ADD.
