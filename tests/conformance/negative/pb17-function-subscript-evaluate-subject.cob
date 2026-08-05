      *> reject-at: 2002 2014 2023
      *> ISO 14.9.13.4 GR3 evaluates an EVALUATE selection SUBJECT once per
      *> statement; this backend's chained-selection lowering re-binds subject
      *> expressions PER WHEN, so hoisting a function-bearing subscript into the
      *> subject would over-activate it (fix-queue PB17 / D18). The sibling of
      *> pb17-function-subscript-varying-by: the same narrowed COBOLNET1509, the
      *> other direction of the same cardinality mismatch (over- rather than
      *> under-activation), reached through the SAME statement-pending list the
      *> user-defined-function activations use.
      *>
      *> An EVALUATE *object* is NOT staged - it rides UdfAttachPerEvaluation and
      *> works; only the subject is affected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB17NEGEVAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO W-E (1)
           EVALUATE W-E (FUNCTION INTEGER(1))
               WHEN 1 DISPLAY "ONE"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
