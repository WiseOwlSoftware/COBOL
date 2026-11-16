      *>--- conformance-case
      *> id: g85_bf40_06
      *> rules: [1989-RV-A.2.33.4-2, 1989-RV-A.2.33.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.33.4 RV2 - ORD-MIN compares by the rules for simple
      *> conditions; §1989:A.2.33.4 RV3 - of equal least values the
      *> position of the leftmost is returned. §1985:VI.6.3.1.1.1 -
      *> numeric operands compare by algebraic value; the length of a
      *> literal is not significant. §1985:VI.6.3.1.1.2 - a shorter
      *> nonnumeric operand is extended on the right by spaces. OMIN1
      *> -20 is least (3rd) only by algebraic value; OMIN2 "AZZZ" is
      *> least (2nd) by collating order, not length; OMIN3 2 appears 2nd
      *> and 4th, leftmost is 2; OMIN4 "AB " equals the space-extended
      *> "AB" so the leftmost (1) is returned, and a length compare
      *> gives 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4006.
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
           COMPUTE I = FUNCTION ORD-MIN (9 10.5 NEG20).
           DISPLAY "OMIN1 " I.
           COMPUTE I = FUNCTION ORD-MIN (A2 A1 A2).
           DISPLAY "OMIN2 " I.
           COMPUTE I = FUNCTION ORD-MIN (5 2 7 2).
           DISPLAY "OMIN3 " I.
           COMPUTE I = FUNCTION ORD-MIN ("AB " "AB" "B").
           DISPLAY "OMIN4 " I.
           STOP RUN.
