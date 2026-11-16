      *>--- conformance-case
      *> id: g85_bf40_05
      *> rules: [1989-RV-A.2.32.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.4 RV2 - ORD-MAX compares by the rules for simple
      *> conditions. §1985:VI.6.3.1.1.1 - numeric operands compare by
      *> algebraic value; the length of a literal is not significant.
      *> §1985:VI.6.3.1.1.2 - a shorter nonnumeric operand is extended
      *> on the right by spaces. OMAX1 10.5 is greatest (2nd), not 9 by
      *> digit count or -20 by magnitude; OMAX2 "B" outranks "AZZZ"
      *> (1st) regardless of length; OMAX3 "ABC" outranks the
      *> space-extended "AB" (2nd).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4005.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG20 PIC S9(2) VALUE -20.
       01 N1 PIC S9(3) VALUE -5.
       01 N2 PIC S9(3)V9 VALUE +3.5.
       01 N3 PIC 9(1) VALUE 2.
       01 R PIC -9(3).9(3).
       01 I PIC 9.
       01 A1 PIC X(4) VALUE "AZZZ".
       01 A2 PIC X(1) VALUE "B".
       PROCEDURE DIVISION.
       P1.
           COMPUTE I = FUNCTION ORD-MAX (9 10.5 NEG20).
           DISPLAY "OMAX1 " I.
           COMPUTE I = FUNCTION ORD-MAX (A2 A1).
           DISPLAY "OMAX2 " I.
           COMPUTE I = FUNCTION ORD-MAX ("AB" "ABC").
           DISPLAY "OMAX3 " I.
           STOP RUN.
