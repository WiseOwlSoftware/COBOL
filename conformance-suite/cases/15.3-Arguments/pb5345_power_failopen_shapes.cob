      *>--- conformance-case
      *> id: pb5345_power_failopen_shapes
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5345 - the complement of the literal-base arm of
      *> the §2023:15.3 type 6 power screen: powers with no witness
      *> valuation are integer arguments.
      *>
      *> §2023:15.3 type 6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified"; §1989:A.2.2 AR4
      *> the same. 1024 is the 10th power of 2, so 1024 ** K is an
      *> integer for every K of one fraction digit (K = 0.5 gives
      *> 32 and MOD (32, 7) is 4); a literal base to an unsigned
      *> integer item is an integer (2 ** 2 is 4); 1 ** M is 1 for
      *> every M (MOD (1, 7) is 1). A screen that refused a literal
      *> base for its shape alone fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5345A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  M PIC S9 VALUE -2.
       01  K PIC 9V9 VALUE 0.5.
       01  U PIC 9 VALUE 2.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (1024 ** K, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (2 ** U, 7).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (1 ** M, 7).
           DISPLAY R.
           STOP RUN.
