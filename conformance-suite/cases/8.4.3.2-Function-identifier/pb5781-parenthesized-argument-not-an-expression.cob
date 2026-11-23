      *>--- conformance-case
      *> id: pb5781-parenthesized-argument-not-an-expression
      *> rules: [SR-8.4.3.2.3-8,
      *>    GR-8.8.1.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5781 - the negative of
      *> pb5781_argument_after_non_table_name. A is no table, so the
      *> '(' after it begins the next argument (8.4.2.3.3 SR2), and
      *> an argument written in parentheses is a parenthesized
      *> expression; (B A) is two operands, which no expression is,
      *> so the argument list breaks 8.4.3.2.3 SR8 ("Argument-1 shall
      *> be an identifier, a literal, a boolean expression, or an
      *> arithmetic expression") at every edition: COBOL1543.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5781N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 7.
       01 B PIC S9(4) VALUE 3.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MIN (A (B A)).
           DISPLAY R.
           STOP RUN.
