*>--- conformance-case
*> id: pb4635-abs-boolean-arithmetic-expression
*> rules: [AR-15.7.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4635 review R2-1 (the refusal that bounds the admission) - ISO §2023:15.7.3 r1: "Argument-1 shall be of class numeric." (cite.py
      *> --check 15.7.3 OK). A parenthesized class-BOOLEAN item is admitted at a boolean position as "a boolean expression enclosed
      *> in parentheses" (§2023:8.8.2; pb4635_parenthesized_boolean_argument), but (BB) + 1 is no boolean expression: it is an arithmetic
      *> expression (§2023:8.4.3.2.3 SR8), and §2023:8.8.1.1 admits "an identifier referencing a numeric data item" in one, so the boolean item
      *> is refused as it is in COMPUTE T = (BB) + 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGABSBOOLEXPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BB    PIC 1(4) VALUE B"1010".
       01 T     PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION ABS((BB) + 1).
           STOP RUN.
