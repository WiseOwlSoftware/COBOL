      *>--- conformance-case
      *> id: pb3960_figurative_zero_refmod_position
      *> rules: [GR-8.4.3.3.4-5, SR-8.4.3.3.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE FIGURATIVE ZERO IN A REFERENCE-MODIFICATION POSITION IS THE
      *> INTEGER 0 (kb/Work PB3960). ISO 8.4.3.3.3 SR4 makes the
      *> leftmost-position and the length arithmetic expressions, 8.8.1.1
      *> admits "the figurative constant ZERO (ZEROS, ZEROES)" in one, and
      *> 8.3.3.6.4 GR4 gives the zero format "the numeric value '0'" in a
      *> numeric context (each cite.py --check OK). Every legal position
      *> below is therefore an ordinary integer expression over ABCD, and
      *> each leg fails if the ZERO is read as anything but 0 or if the
      *> position the direct reader now gives it is lost:
      *>   T(ZERO + 2:2)         -> BC   (start 0 + 2 = 2, length 2)
      *>   T(2:ZERO + 2)         -> BC   (length 0 + 2 = 2)
      *>   T(ZERO + N:ZERO + 1)  -> B    (N = 2: a data-name beside ZERO)
      *>   T(4 - ZERO:1)         -> D    (start 4 - 0)
      *>   T(1:4 * ZERO + 3)     -> ABC  (4 * 0 + 3 = 3)
      *> The bare T(ZERO:2) and T(2:ZERO) are the value 0 as start and as
      *> length: 8.4.3.3.4 5) b) and c) refuse them (pb3960-refmod-zero-start,
      *> pb3960-refmod-zero-length), as T(0:2) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3960A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T    PIC X(4) VALUE "ABCD".
       01 N    PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY T(ZERO + 2:2)
           DISPLAY T(2:ZERO + 2)
           DISPLAY T(ZERO + N:ZERO + 1)
           DISPLAY T(4 - ZERO:1)
           DISPLAY T(1:4 * ZERO + 3)
           STOP RUN.
