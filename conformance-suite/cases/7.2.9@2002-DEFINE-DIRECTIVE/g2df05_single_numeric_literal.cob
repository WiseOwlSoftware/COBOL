      *>--- conformance-case
      *> id: g2df05_single_numeric_literal
      *> rules: [2002-GR-7.2.9.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.9.3 GR5 - "If the operand of the DEFINE directive
      *>   consists of a single numeric literal, that operand is treated
      *>   as a literal, not as an arithmetic-expression."
      *> Why it is observable: an arithmetic-expression-1 operand is
      *>   evaluated by 7.2.5 (§2002:7.2.9.3 GR6), whose "final result of
      *>   the arithmetic expression shall be truncated to the integer
      *>   part of the value" (§2002:7.2.5.2 GR2); a lone literal is not,
      *>   and the name references the literal (§2002:7.2.9.3 GR7).
      *> EXPECTED OUTPUT, DERIVED:
      *>   LITERAL-1.5           G2V AS 1.5 is a single numeric literal:
      *>                         G2V = 1.5 TRUE, G2V = 1 FALSE.
      *>   EXPRESSION-1          G2W AS 1.5 + 0 is an expression: 1.5,
      *>                         truncated to 1; G2W = 1 TRUE, = 1.5 FALSE.
      *>   SIGNED-LITERAL--2.75  -2.75 is ONE literal ("If a sign is used,
      *>                         it shall appear as the leftmost character
      *>                         of the literal", §2002:8.3.1.2.2.1), so
      *>                         G2Z = -2.75 TRUE, G2Z = -2 FALSE.
      *> How each leg can fail: a lone literal sent down the expression
      *> path prints LITERAL-BAD-TRUNCATED or SIGNED-BAD-TRUNCATED and
      *> drops its own line; an expression left untruncated prints
      *> EXPRESSION-BAD-NOT-TRUNCATED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DF05.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE G2V AS 1.5
       >>IF G2V = 1.5
           DISPLAY "LITERAL-1.5"
       >>END-IF
       >>IF G2V = 1
           DISPLAY "LITERAL-BAD-TRUNCATED"
       >>END-IF
       >>DEFINE G2W AS 1.5 + 0
       >>IF G2W = 1
           DISPLAY "EXPRESSION-1"
       >>END-IF
       >>IF G2W = 1.5
           DISPLAY "EXPRESSION-BAD-NOT-TRUNCATED"
       >>END-IF
       >>DEFINE G2Z AS -2.75
       >>IF G2Z = -2.75
           DISPLAY "SIGNED-LITERAL--2.75"
       >>END-IF
       >>IF G2Z = -2
           DISPLAY "SIGNED-BAD-TRUNCATED"
       >>END-IF
           STOP RUN.
