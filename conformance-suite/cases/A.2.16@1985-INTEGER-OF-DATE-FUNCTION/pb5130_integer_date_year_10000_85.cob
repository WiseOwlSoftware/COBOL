      *>--- conformance-case
      *> id: pb5130_integer_date_year_10000_85
      *> rules: [1989-AR-A.2.16.3-1, 1989-AR-A.2.17.3-1,
      *>     1989-AR-A.2.12.3-1, 1989-AR-A.2.13.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5130 - the 1989 intrinsic function module bounds
      *> the year of the integer date conversions BELOW only.
      *>
      *> §1989:A.2.16.3 AR1 a. (cite.py --check): YYYY "must be an
      *> integer greater than 1600"; §1989:A.2.17.3 AR1 the same for
      *> INTEGER-OF-DAY; §1989:A.2.12.3 AR1 and §1989:A.2.13.3 AR1:
      *> argument-1 "is a positive integer that represents a number
      *> of days succeeding December 31, 1600"; §1989:A.2.16.4: the
      *> value is "the number of days the date represented by
      *> argument-1 succeeds December 31, 1600". No upper bound.
      *> Derivation: 1601 through 10000 is 8,400 years, 21 cycles of
      *> 146,097 days, so 10000-12-31 is day 3,068,037; 10000 is a
      *> leap year, so 9999-12-31 is day 3,067,671 (§2023:15.5.2's
      *> figure) and 10000-01-01 is day 3,067,672. Each line fails
      *> if the year is capped at 9999 (it shows 0) or a leading
      *> digit is dropped (00000101). From 2002 the year ends at
      *> 9999 (pb5130_integer_date_form_bounded).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5130A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DATE             PIC 9(9) VALUE 100000101.
       01  WS-DAYS             PIC 9(8).
       01  WS-W                PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (16010101).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (99991231).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (WS-DATE).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (100001231).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DAY (10000001).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DAY (10000366).
           DISPLAY WS-DAYS.
           COMPUTE WS-W = FUNCTION DATE-OF-INTEGER (3067672).
           DISPLAY WS-W.
           COMPUTE WS-W = FUNCTION DAY-OF-INTEGER (3068037).
           DISPLAY WS-W.
           DISPLAY FUNCTION DATE-OF-INTEGER (3068037).
           DISPLAY FUNCTION DAY-OF-INTEGER (3067672).
           STOP RUN.
