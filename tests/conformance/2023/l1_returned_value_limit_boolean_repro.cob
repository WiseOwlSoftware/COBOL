      *> ISO 15.4 Returned values, at the SECOND enforcement site Annex A.1
      *> item 93 names - docs/CONFORMANCE.md row DOC-A.1-93 names both
      *> CobolIntrinsics.BaseConvert (witnessed by the sibling golden
      *> l1_returned_value_length_limit) and CobolIntrinsics.BooleanOfInteger,
      *> which is THIS golden. Landed with the kb/Work PB383 fix.
      *>
      *> THE RULE IS NOT AN ARGUMENT RULE. 15.13.3 r2 requires argument-2 to
      *> be "a positive nonzero integer" and 8 192 IS one, so no argument rule
      *> is violated here. What is violated is 15.4: "If the length of the
      *> returned value exceeds the maximum length specified by the
      *> implementor for a returned value, an EC-ARGUMENT-FUNCTION exception
      *> condition is set to exist." 15.13.4 r1 makes the returned value a
      *> boolean item of argument-2 positions, so 8 192 positions is one past
      *> the implementor's documented maximum of 8 191 (row DOC-A.1-93 - the
      *> 8.3.3.4.3 SR1 boolean-literal maximum reused).
      *>
      *> THE SUBSTITUTED RESULT. Checking is not enabled here, so 15.3 rule 14
      *> applies: "If the EC-ARGUMENT-FUNCTION exception condition is set to
      *> exist and checking for EC-ARGUMENT-FUNCTION is not enabled, the
      *> implementor defines the result of the function reference." Row
      *> DOC-A.1-93 defines it as A ZERO-LENGTH VALUE. Two independent
      *> observations pin that, because either alone is weak:
      *>   OVER    14.9.11.4 GR1 - "If an operand is a zero-length data item
      *>           or a zero-length literal, no data is transferred for that
      *>           operand" - so the brackets close on nothing. A returned
      *>           one-position boolean "0" would print OVER=[0]; that was the
      *>           PB383 defect this file was written to expose.
      *>   OVERLEN 15.50.4 r1 - LENGTH of a boolean argument "is an integer
      *>           equal to the length of argument-1 in boolean positions", so
      *>           the length is READ OUT as a number: 0, not 1. The argument
      *>           is legal per 15.50.3 r1 ("a data item of any class or
      *>           category") because 8.4.3.2.1 makes a function-identifier a
      *>           reference to "the unique data item that results from the
      *>           evaluation of a function", and 8.4.3.2.4 r2 permits it:
      *>           "An argument being evaluated may itself be a
      *>           function-identifier".
      *>
      *> ATMAXTAIL PUTS THE BOUNDARY INSIDE. 8 191 positions is EXACTLY the
      *> maximum and converts whole, so the guard is "> 8 191" and never
      *> ">= 8 191". Value 5 is 101 binary and 15.13.4 r1 puts "the rightmost
      *> boolean position" at the low-order digit, so positions 8 188-8 191
      *> are 0101. Reference modification of a boolean item is 8.4.3.3.3 r5's
      *> "anywhere an identifier referencing a data item of class
      *> alphanumeric, boolean, or national is permitted".
      *>
      *> NEG IS THE DISCRIMINATOR, and it is why OVER=[] cannot be dismissed
      *> as "the function returns nothing whenever anything is wrong".
      *> Argument-1 negative violates 15.13.3 r1, but argument-2 is VALID, so
      *> the returned length is fully determined and row DOC-A.1-90's general
      *> clause - "the zero value of the type the function returns" - gives
      *> eight real boolean positions, not a zero-length value. Three rules,
      *> three arms, two different documented answers; PB383 was one condition
      *> answering "0" for the first two.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RVLBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8191 PIC 1(8191) USAGE BIT.
       01 W-LEN PIC 9(5).
       01 W-NEG PIC S9(5) VALUE -3.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "INLIMIT=[" FUNCTION BOOLEAN-OF-INTEGER(5, 8) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8)) TO W-LEN
           DISPLAY "INLIMITLEN=" W-LEN
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8191) TO B8191
           DISPLAY "ATMAXTAIL=[" B8191(8188:4) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8191)) TO W-LEN
           DISPLAY "ATMAXLEN=" W-LEN
           DISPLAY "OVER=[" FUNCTION BOOLEAN-OF-INTEGER(5, 8192) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8192)) TO W-LEN
           DISPLAY "OVERLEN=" W-LEN
           DISPLAY "NEG=[" FUNCTION BOOLEAN-OF-INTEGER(W-NEG, 8) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(W-NEG, 8)) TO W-LEN
           DISPLAY "NEGLEN=" W-LEN
           STOP RUN.
