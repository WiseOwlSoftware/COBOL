*>--- conformance-case
*> id: pb3953_function_argument_int128_window
*> rules: [RV-15.4.1-1, DOC-A.1-123]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3953 (docs/CONFORMANCE.md DOC-A.1-123) - an arithmetic expression used as a FUNCTION ARGUMENT is exact
      *> inside the 38-digit Int128 window and PAST it, up to the 77 digits of the exact wide intermediate a nested product
      *> keeps in a COMPUTE: the exact intrinsic family evaluates such an argument on that wide form.
      *> ISO §8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression, an
      *> arithmetic statement, the SUM clause, and all integer and numeric functions." (cite.py: OK 8.8.1.3), and §15.4.1:
      *> "When native arithmetic is in effect, the characteristics and representation of the returned value are defined by
      *> the implementor." (cite.py: OK 15.4.1). The standard fixes no width, so CLAUDE.md rule 1 makes GnuCOBOL's behaviour
      *> the answer, and GnuCOBOL's argument temporary is arbitrary-precision: every leg below shows the EXACT value of its
      *> expression. Before the fix a 39-digit argument reached the function as its 34-digit round-to-odd image: leg 4
      *> showed ...100000, leg 5 answered 000000000, leg 6 could not tell its two arguments apart, and leg 7 stored
      *> ...0000000100000.
      *>
      *> EXPECTED VALUES, DERIVED (BIG = 10**18 - 1, K2 = 99, K3 = 999):
      *>   BIG * BIG * K2 = 98999999999999999802000000000000000099                       (38 digits - inside the window)
      *>   BIG * BIG * K3 = 998999999999999998002000000000000000999                     (39 digits - past the window)
      *>   Leg 1 ABS of the 38-digit product shows it whole.                          (ABS shows its value: DOC-A.1-92)
      *>   Leg 2 MOD (98999999999999999802000000000000000099, 1000) = 99, shown as the 9-position item a value under
      *>         2**31 has (DOC-A.1-92): 000000099.
      *>   Leg 3 BIG * BIG * K3 - (BIG * BIG * K3 - 999) is exact in a COMPUTE, 999, into S9(31): +0000000000000000000000000000999.
      *>   Leg 4 ABS of the 39-digit product (§15.7.1: "The ABS function returns the absolute value of the argument.") shows all 39 digits.
      *>   Leg 5 MOD (998999999999999998002000000000000000999, 1000) (§15.64.4: a - b * INTEGER (a / b)) = 999: 000000999.
      *>   Leg 6 MAX of the product and the product + 1 (§15.59.4 1: the content of the argument having the greatest value) is
      *>         998999999999999998002000000000000001000 - the two share their 34-digit image, so only the exact form orders them.
      *>   Leg 7 MOVE FUNCTION INTEGER (BIG * BIG * K3) TO R: INTEGER is the product itself, and the MOVE keeps its 31 low-order
      *>         digits (§14.6.8.2 4: "transferred to the receiving digits with zero fill or truncation on either end as
      *>         required"): +9999999998002000000000000000999.
      *> Each leg can fail on its own: legs 1/2 if the function window moved below 38 digits, leg 3 if the arithmetic
      *> statement lost the exact wide form, legs 4-6 if a function argument past the window is lowered to its image again,
      *> leg 7 if a MOVE stores a wide function value through that image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3953FA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K2  PIC 99    VALUE 99.
       01 K3  PIC 999   VALUE 999.
       01 R   PIC S9(31).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FUNCTION ABS (BIG * BIG * K2)
           DISPLAY FUNCTION MOD (BIG * BIG * K2, 1000)
           COMPUTE R = BIG * BIG * K3 - (BIG * BIG * K3 - 999)
           DISPLAY R
           DISPLAY FUNCTION ABS (BIG * BIG * K3)
           DISPLAY FUNCTION MOD (BIG * BIG * K3, 1000)
           DISPLAY FUNCTION MAX (BIG * BIG * K3, BIG * BIG * K3 + 1)
           MOVE FUNCTION INTEGER (BIG * BIG * K3) TO R
           DISPLAY R
           STOP RUN.
