      *>--- conformance-case
      *> id: pb5832_scaled_integral_shapes
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832, PB5886 - the complement of the product arm
      *> of the type 6 screen: an expression that ALWAYS results in
      *> an integer value through literal factors is an integer
      *> argument. A product by a literal is exact, and a quotient
      *> by a literal whose reciprocal terminates is the product by
      *> that reciprocal (kb/Work PB5886 round 2), so the item's net
      *> coefficient decides; a quotient by an item is no integer
      *> term (pb5886-item-quotients).
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 the same. MOD's returned value is argument-1 -
      *> (argument-2 * FUNCTION INTEGER (argument-1 / argument-2))
      *> (§2023:15.64.4 RV1). With WN = 3, S = 1.5, U = 2 and
      *> N = 2 each argument-1 below is an integer, so:
      *>   WN * 0.5 * 2            = 3         MOD (3, 7) = 3
      *>   WN / 2 * 2              = 3         MOD (3, 7) = 3
      *>   WN * 0.5 + WN * 0.5     = 3         MOD (3, 7) = 3
      *>   S / 0.1                 = 15        MOD (15, 7) = 1
      *>   U * 4 / 2               = 4         MOD (4, 7) = 4
      *>   U * N * 2               = 8         MOD (8, 7) = 1
      *> A screen that refused a product by a fraction literal for
      *> its shape alone fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WN PIC 9 VALUE 3.
       01  S  PIC 9V9 VALUE 1.5.
       01  U  PIC 9 VALUE 2.
       01  N  PIC 9 VALUE 2.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (WN * 0.5 * 2, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (WN / 2 * 2, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (WN * 0.5 + WN * 0.5, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (S / 0.1, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (U * 4 / 2, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (U * N * 2, 7).
           DISPLAY R.
           STOP RUN.
