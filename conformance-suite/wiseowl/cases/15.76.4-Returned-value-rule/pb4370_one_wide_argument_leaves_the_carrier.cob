      *>--- conformance-case
      *> id: pb4370_one_wide_argument_leaves_the_carrier
      *> rules: [RV-15.76.4-1, RV-15.61.4-1, RV-15.61.4-2, RV-15.62.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4370 (found on the way): under NATIVE arithmetic
      *> RANGE, MEDIAN and MIDRANGE align every argument to the list's
      *> largest scale on the Int128 carrier, and WiseOwl COBOL's
      *> determination (kb/Work PB621) moves a list whose aligned
      *> arguments may leave that carrier onto the SDIDI, which holds
      *> the value. The question (IntrinsicCarrierRules.
      *> AlignmentMayLeaveCarrier) ordered the arguments' aligned
      *> bounds with OrderByDescending, which puts an UNBOUNDED term
      *> (past 38 digits) LAST, then took the two largest: with ONE
      *> wide argument and two others it proved a fit, kept the native
      *> arm, and the run ended in EC-SIZE-OVERFLOW for a value the
      *> receiver holds. PB621's legs each had two wide arguments.
      *> BIG = 31 nines, FR = 0.123456789012345678 (scale 18), so BIG
      *> aligned to scale 18 needs 49 digits; TH = 3, FV = 5, Z = 0.
      *> Accepted from 2002: the functions are 85's, but an argument
      *> wide enough to leave the carrier needs the 31-digit item
      *> 2002 introduced (COBOL-85 caps a fixed-point item at 18
      *> digits, so its widest alignment is 36 digits and fits).
      *> Expected values are exact arithmetic, truncated into the
      *> receiver (no ROUNDED phrase):
      *>  - RANGE(BIG FR TH) = MAX - MIN = BIG - FR
      *>    = 9999999999999999999999999999998.876543210987654322
      *>    -> 9999999999999999999999999999998;
      *>  - MIDRANGE(BIG FR TH) = (BIG + FR) / 2
      *>    = 4999999999999999999999999999999.561728394506172839
      *>    -> 4999999999999999999999999999999;
      *>  - MEDIAN(BIG FR Z), odd: Z < FR < BIG, the middle value FR
      *>    = 0.123456789012345678 (18 digits, exact on the SDIDI);
      *>  - MEDIAN(BIG FR TH FV), even: the two middle values are TH
      *>    and FV, (3 + 5) / 2 = 4;
      *>  - control, no wide argument (the native arm):
      *>    RANGE(TH FR Z) = 3 - 0 = 3.
      *> Each wide leg fails on the unfixed predicate (EC-SIZE-OVERFLOW
      *> ends the run unit before its DISPLAY).
      *>   cite.py --check 15.76.4 "(FUNCTION MAX (argument-list) -
      *>     FUNCTION MIN (argument-list))" -> OK 15.76.4 1)
      *>   cite.py --check 15.62.4 "FUNCTION MAX (argument-list) +
      *>     FUNCTION MIN (argument-list)" -> OK 15.62.4 1)
      *>   cite.py --check 15.61.4 "When the number of occurrences of
      *>     argument-1 is odd, the returned value is such that at
      *>     least half of the occurrences referenced by argument-1 are
      *>     greater than or equal to the returned value"
      *>     -> OK 15.61.4 1)
      *>   cite.py --check 15.61.4 "When the number of occurrences of
      *>     argument-1 is even, the returned value is the arithmetic
      *>     mean of the two middle values" -> OK 15.61.4 2)
      *>   cite.py --check 15.4.1 "the value returned is an
      *>     implementor-defined approximation of the value of that
      *>     expression" -> OK 15.4.1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4370WID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(31) VALUE 9999999999999999999999999999999.
       01 FR  PIC V9(18) VALUE .123456789012345678.
       01 TH  PIC 9 VALUE 3.
       01 FV  PIC 9 VALUE 5.
       01 Z   PIC 9 VALUE 0.
       01 RW  PIC S9(31).
       01 RS  PIC S9V9(18).
       01 EW  PIC +9(31).
       01 ES  PIC +9.9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE RW = FUNCTION RANGE(BIG FR TH)
           MOVE RW TO EW
           DISPLAY "RANGE=" EW
           COMPUTE RW = FUNCTION MIDRANGE(BIG FR TH)
           MOVE RW TO EW
           DISPLAY "MIDRANGE=" EW
           COMPUTE RS = FUNCTION MEDIAN(BIG FR Z)
           MOVE RS TO ES
           DISPLAY "MEDIAN-ODD=" ES
           COMPUTE RS = FUNCTION MEDIAN(BIG FR TH FV)
           MOVE RS TO ES
           DISPLAY "MEDIAN-EVEN=" ES
           COMPUTE RW = FUNCTION RANGE(TH FR Z)
           MOVE RW TO EW
           DISPLAY "RANGE-SMALL=" EW
           STOP RUN.
