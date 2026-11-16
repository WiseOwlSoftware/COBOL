      *>--- conformance-case
      *> id: pb5781_argument_after_non_table_name
      *> rules: [GR-8.8.1.2-5, SR-8.4.2.3.3-2, SR-8.4.3.2.3-8]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5781 - a '(' after a name that cannot be
      *> subscripted begins the NEXT item of the list it stands in,
      *> in an argument list exactly as in a subscript list (kb/Work
      *> PB136). The lists separate their items by a space (8.3.5
      *> SR1); a subscript may be written only on a table element
      *> (8.4.2.3.3 SR2); and 8.8.1.2 GR5 puts a unary-led expression
      *> that "immediately follows an identifier" in its own left
      *> parenthesis. A, B and X are no tables: A = 7, B = 3, X = 3,
      *> T (1) = 10, T (2) = 20, T (3) = 30, D2 (3, 2) = 32.
      *> L1 FUNCTION MIN (A (- B))        arguments 7, -3       = -3
      *> L2 FUNCTION MAX (A (B) 1)        arguments 7, 3, 1     = 7
      *> L3 FUNCTION MIN (B + A (- B))    arguments 10, -3      = -3
      *> L4 FUNCTION SUM (A (- B) T (1))  7 - 3 + 10            = 14
      *> L5 FUNCTION MAX (T (X) 1)        T keeps its subscript = 30
      *> L6 MIN (A (- B))   keyword-omitted (8.4.3.2.3 SR2)     = -3
      *> L7 D2 (X / 1 (B - 1))            subscripts 3, 2       = 32
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5781A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 7.
       01 B PIC S9(4) VALUE 3.
       01 X PIC 9 VALUE 3.
       01 RE PIC -9999.
       01 TT.
          05 T PIC 99 OCCURS 3 TIMES.
       01 DD.
          05 D1 OCCURS 3 TIMES.
             10 D2 PIC 99 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P1.
           MOVE 10 TO T (1)
           MOVE 20 TO T (2)
           MOVE 30 TO T (3)
           MOVE 32 TO D2 (3, 2)
           COMPUTE RE = FUNCTION MIN (A (- B))
           DISPLAY "L1=" RE
           COMPUTE RE = FUNCTION MAX (A (B) 1)
           DISPLAY "L2=" RE
           COMPUTE RE = FUNCTION MIN (B + A (- B))
           DISPLAY "L3=" RE
           COMPUTE RE = FUNCTION SUM (A (- B) T (1))
           DISPLAY "L4=" RE
           COMPUTE RE = FUNCTION MAX (T (X) 1)
           DISPLAY "L5=" RE
           COMPUTE RE = MIN (A (- B))
           DISPLAY "L6=" RE
           DISPLAY "L7=" D2 (X / 1 (B - 1))
           STOP RUN.
