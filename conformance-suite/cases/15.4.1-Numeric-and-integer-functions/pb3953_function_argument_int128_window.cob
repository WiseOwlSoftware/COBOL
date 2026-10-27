*>--- conformance-case
*> id: pb3953_function_argument_int128_window
*> rules: [RV-15.4.1-1, DOC-A.1-123]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3953 (docs/CONFORMANCE.md DOC-A.1-123) - an arithmetic expression used as a FUNCTION ARGUMENT is exact
      *> inside the 38-digit Int128 window the function bodies compute on, and a COMPUTE keeps every digit of a wider product.
      *> ISO §8.8.1.3: "Native arithmetic is an implementor-defined method of evaluating an arithmetic expression, an
      *> arithmetic statement, the SUM clause, and all integer and numeric functions." (cite.py: OK 8.8.1.3), and §15.4.1:
      *> "When native arithmetic is in effect, the characteristics and representation of the returned value are defined by
      *> the implementor." (cite.py: OK 15.4.1). The standard fixes no width, and every leg below shows the EXACT value of
      *> its expression, which is also what GnuCOBOL's arbitrary-precision argument temporary gives.
      *> An argument expression of 39 digits or more is NOT pinned here: it reaches the function as a 34-significant-digit
      *> image and answers a value other than the exact one (ABS (BIG * BIG * K3) ends 100000 for 000999), a departure from
      *> GnuCOBOL that kb/Work PB3953 keeps OPEN until the function family carries the exact wide form. A golden of that image
      *> would assert, as conforming, a value the compiler owes the user otherwise; the legs of the defect are added when it
      *> is fixed.
      *>
      *> EXPECTED VALUES, DERIVED (BIG = 10**18 - 1, K2 = 99, K3 = 999):
      *>   BIG * BIG * K2 = 98999999999999999802000000000000000099                       (38 digits - inside the window)
      *>   Leg 1 ABS of the 38-digit product shows it whole.                          (ABS shows its value: DOC-A.1-92)
      *>   Leg 2 MOD (98999999999999999802000000000000000099, 1000) = 99, shown as the 9-position item a value under
      *>         2**31 has (DOC-A.1-92): 000000099.
      *>   Leg 3 BIG * BIG * K3 - (BIG * BIG * K3 - 999) is exact in a COMPUTE, 999, into S9(31): +0000000000000000000000000000999.
      *> Each leg can fail on its own: leg 1/2 if the function window moved below 38 digits, leg 3 if the arithmetic
      *> statement lost the exact wide form.
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
           STOP RUN.
