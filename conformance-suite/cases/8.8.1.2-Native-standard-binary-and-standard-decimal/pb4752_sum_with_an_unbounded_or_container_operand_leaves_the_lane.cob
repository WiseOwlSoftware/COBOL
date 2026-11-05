      *>--- conformance-case
      *> id: pb4752_sum_with_an_unbounded_or_container_operand_leaves_the_lane
      *> rules: [DOC-A.1-123, GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4752: a native sum or difference whose ALIGNMENT to the common scale leaves the 38 digits of the Int128 lane is
      *> exact whatever the bound of its operands is known to be - an intrinsic function's value has no bound of its own and a
      *> 16-byte container is bounded only by the carrier's 39 digits - as it is for operands of known PICTURE bound (kb/Work PB4729).
      *>
      *> ISO 8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression" (cite.py: OK 8.8.1.3);
      *> 14.7.5 5): "if native arithmetic is in effect and the implementor defines that the range of values allowed for the intermediate
      *> data item is to be checked" (cite.py: OK 14.7.5 5)), and the range WiseOwl COBOL checks is the SDIDI's, |value| < 10^6145
      *> (docs/CONFORMANCE.md DOC-A.1-123). D = 1000000000000000001 beside F = 1.01 (V9(30)) aligns D to 30 fraction digits, a 49-digit
      *> integer, past any 128-bit lane although the sum is the plain 1000000000000000002.01. Python integers and Decimal give each value.
      *>   S1  FUNCTION INTEGER (D) + F is 1000000000000000002.01 (INTEGER has no digit bound of its own)
      *>   S2  F + FUNCTION ABS (D) is the same sum, the operands swapped
      *>   S3  FUNCTION ABS (D) - F is 999999999999999999.99
      *>   S4  C = 9999999999999999999 (PIC S9(19) COMP-5, a 16-byte container: bound 39) + G = 1.25 is
      *>       10000000000000000000.25, which aligned to two places is a 41-digit integer
      *>   S5  the same sum as a relation: C + G > C is TRUE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4752ADD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9(19) VALUE 1000000000000000001.
       01 F  PIC 9V9(30) VALUE 1.010000000000000000000000000000.
       01 C  PIC S9(19) COMP-5 VALUE 9999999999999999999.
       01 G  PIC 9V99 VALUE 1.25.
       01 R1 PIC 9(19)V99.
       01 R2 PIC 9(19)V99.
       01 R3 PIC 9(19)V99.
       01 R4 PIC 9(20)V99.
       PROCEDURE DIVISION.
           COMPUTE R1 = FUNCTION INTEGER (D) + F
           DISPLAY "S1 " R1
           COMPUTE R2 = F + FUNCTION ABS (D)
           DISPLAY "S2 " R2
           COMPUTE R3 = FUNCTION ABS (D) - F
           DISPLAY "S3 " R3
           COMPUTE R4 = C + G
           DISPLAY "S4 " R4
           IF C + G > C DISPLAY "S5 TRUE" ELSE DISPLAY "S5 FALSE" END-IF
           STOP RUN.
       END PROGRAM PB4752ADD.
