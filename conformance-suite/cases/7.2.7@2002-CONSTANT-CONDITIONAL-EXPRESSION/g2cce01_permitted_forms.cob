      *>--- conformance-case
      *> id: g2cce01_permitted_forms
      *> rules: [2002-SR-7.2.7.1-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.1 SR1 - a constant conditional expression "shall be
      *>   one of the following": a) a relation condition of literals,
      *>   arithmetic or boolean expressions of literals, formed per
      *>   8.8.4.1.1, operands "of the same category", and for
      *>   non-numeric literals only IS [NOT] EQUAL TO / IS [NOT] =;
      *>   b) a boolean condition (8.8.4.1.2) "in which all operands are
      *>   boolean literals"; c) a defined condition; d) a complex
      *>   condition per 8.8.4.2 (AND, OR, NOT) without abbreviation.
      *>   Each form is written below; the refused forms (<>, a
      *>   non-numeric <, mixed category, abbreviation, XOR, a boolean
      *>   condition of length 2) are g2cce02..g2cce08 in this folder.
      *> EXPECTED OUTPUT, DERIVED (§2002:7.2.13.3 GR2 includes text-1
      *>   when TRUE, GR3 text-2 when FALSE):
      *>   R1-TRUE   a)2: "A" = "A" AND "A" IS NOT EQUAL TO "B".
      *>   R2-TRUE   d): NOT, AND, OR precedence (§2002:8.8.4.2.3):
      *>             1 = 1 OR (1 = 2 AND 1 = 2) is TRUE; left to right
      *>             it would be FALSE.
      *>   R3-TRUE   d): NOT 1 = 2.
      *>   R4-TRUE   b): B"1" is 1 (§2002:8.8.4.1.2.3 GR1).
      *>   R5-FALSE  b): B"0" is FALSE, text-2.
      *>   R6-TRUE   d) of b): NOT B"0".
      *>   R7-TRUE   c) in d): X1 IS DEFINED AND X9 IS NOT DEFINED.
      *>   R8-TRUE   a) boolean expressions: B"1100" B-AND B"1010" is
      *>             B"1000", category boolean, = permitted.
      *>   R9-TRUE   a) arithmetic: 2 * 3 + 1 >= 7 (numeric operands
      *>             take any 8.8.4.1.1 operator).
      *>   R10-FALSE a) arithmetic: 2 * 3 + 1 > 7 is FALSE, text-2.
      *>   R11-TRUE  a)2 numeric IS NOT =: 1 IS NOT = 2.
      *>   R12-TRUE  d) parentheses: (1 = 2 OR 1 = 1) AND 2 = 2.
       >>DEFINE X1 AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE09.
       PROCEDURE DIVISION.
       MAIN-PARA.
           >>IF "A" = "A" AND "A" IS NOT EQUAL TO "B"
           DISPLAY "R1-TRUE"
           >>ELSE
           DISPLAY "R1-FALSE"
           >>END-IF
           >>IF 1 = 1 OR 1 = 2 AND 1 = 2
           DISPLAY "R2-TRUE"
           >>ELSE
           DISPLAY "R2-FALSE"
           >>END-IF
           >>IF NOT 1 = 2
           DISPLAY "R3-TRUE"
           >>ELSE
           DISPLAY "R3-FALSE"
           >>END-IF
           >>IF B"1"
           DISPLAY "R4-TRUE"
           >>ELSE
           DISPLAY "R4-FALSE"
           >>END-IF
           >>IF B"0"
           DISPLAY "R5-TRUE"
           >>ELSE
           DISPLAY "R5-FALSE"
           >>END-IF
           >>IF NOT B"0"
           DISPLAY "R6-TRUE"
           >>ELSE
           DISPLAY "R6-FALSE"
           >>END-IF
           >>IF X1 IS DEFINED AND X9 IS NOT DEFINED
           DISPLAY "R7-TRUE"
           >>ELSE
           DISPLAY "R7-FALSE"
           >>END-IF
           >>IF B"1100" B-AND B"1010" = B"1000"
           DISPLAY "R8-TRUE"
           >>ELSE
           DISPLAY "R8-FALSE"
           >>END-IF
           >>IF 2 * 3 + 1 >= 7
           DISPLAY "R9-TRUE"
           >>ELSE
           DISPLAY "R9-FALSE"
           >>END-IF
           >>IF 2 * 3 + 1 > 7
           DISPLAY "R10-TRUE"
           >>ELSE
           DISPLAY "R10-FALSE"
           >>END-IF
           >>IF 1 IS NOT = 2
           DISPLAY "R11-TRUE"
           >>ELSE
           DISPLAY "R11-FALSE"
           >>END-IF
           >>IF (1 = 2 OR 1 = 1) AND 2 = 2
           DISPLAY "R12-TRUE"
           >>ELSE
           DISPLAY "R12-FALSE"
           >>END-IF
           STOP RUN.
