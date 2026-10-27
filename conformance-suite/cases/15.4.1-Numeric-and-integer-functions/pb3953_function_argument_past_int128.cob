*>--- conformance-case
*> id: pb3953_function_argument_past_int128
*> rules: [RV-15.4.1-1, DOC-A.1-123]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3953 (docs/CONFORMANCE.md DOC-A.1-123) - the width at which an arithmetic expression used as a FUNCTION
      *> ARGUMENT stops being exact. ISO §8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an
      *> arithmetic expression, an arithmetic statement, the SUM clause, and all integer and numeric functions." (cite.py:
      *> OK 8.8.1.3), and §15.4.1: "When native arithmetic is in effect, the characteristics and representation of the
      *> returned value are defined by the implementor." (cite.py: OK 15.4.1). So the standard fixes no width, and WiseOwl
      *> COBOL's determination is the one DOC-A.1-123 states: an argument expression is a receiver-less operand; it is exact
      *> while its digits fit the 38-digit Int128 window the function bodies compute on, and past it the expression is
      *> lowered to the 34-significant-digit standard-decimal intermediate by ROUND-TO-ODD (a value that loses digits keeps an
      *> odd last digit), and the function answers from that image. A COMPUTE (and a relation) keeps the exact 256-bit form
      *> to 77 digits and rounds once, at the final transfer, so the same expression there is exact (leg 5).
      *>
      *> EXPECTED VALUES, DERIVED (BIG = 10**18 - 1, K2 = 99, K3 = 999):
      *>   BIG * BIG * K2 = 98999999999999999802000000000000000099                       (38 digits - inside the window)
      *>   BIG * BIG * K3 = 998999999999999998002000000000000000999                      (39 digits - past it)
      *>   its 34 significant digits are 9989999999999999980020000000000000 and the dropped five digits, 00999, are not all
      *>   zero, so round-to-odd makes the 34th digit odd: 9989999999999999980020000000000001, then five zeros.
      *>   Leg 1 ABS of the 38-digit product shows it whole.                          (ABS shows its value: DOC-A.1-92)
      *>   Leg 2 ABS of the 39-digit product shows the image: 998999999999999998002000000000000100000.
      *>   Leg 3 MOD (98999999999999999802000000000000000099, 1000) = 99 (the 38-digit product is exact), shown as the
      *>         9-position item a value under 2**31 has (DOC-A.1-92): 000000099.
      *>   Leg 4 MOD of the 39-digit product is the image's residue, 100000 mod 1000 = 0: 000000000. The exact residue
      *>         is 999, and leg 5 shows the exact lane holds it.
      *>   Leg 5 BIG * BIG * K3 - (BIG * BIG * K3 - 999) is exact in a COMPUTE, 999, into S9(31): +0000000000000000000000000000999.
      *> Each leg can fail on its own: leg 1/3 if the window moved below 38 digits, leg 2/4 if an argument past it stopped
      *> being lowered (or were exact, which is a different determination, to be made in DOC-A.1-123 and these outputs),
      *> leg 5 if the arithmetic statement lost the exact wide form.
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
           DISPLAY FUNCTION ABS (BIG * BIG * K3)
           DISPLAY FUNCTION MOD (BIG * BIG * K2, 1000)
           DISPLAY FUNCTION MOD (BIG * BIG * K3, 1000)
           COMPUTE R = BIG * BIG * K3 - (BIG * BIG * K3 - 999)
           DISPLAY R
           STOP RUN.
