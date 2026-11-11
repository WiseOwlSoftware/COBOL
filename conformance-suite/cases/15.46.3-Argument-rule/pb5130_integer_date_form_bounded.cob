      *>--- conformance-case
      *> id: pb5130_integer_date_form_bounded
      *> rules: [AR-15.46.3-1, AR-15.22.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5130 - from COBOL-2002 the integer date form ends
      *> at 9999-12-31.
      *>
      *> §2023:15.46.3 AR1 a) (cite.py --check): YYYY "shall be an
      *> integer greater than 1600 and less than 10000"; §2023:15.5.2:
      *> a value in integer date form "shall be less than or equal to
      *> the value of FUNCTION INTEGER-OF-DATE (99991231), which is
      *> 3,067,671" (the 2002 text words both the same). An argument
      *> outside the rules is an incorrect value (§2023:15.3) and,
      *> with EC-ARGUMENT-FUNCTION checking not enabled, the result is
      *> WiseOwl COBOL's documented default 0. So year 10000 and day
      *> 3,067,672 give 0 where COBOL-85 gives a date
      *> (pb5130_integer_date_year_10000_85); 9999-12-31 is still
      *> day 3,067,671.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5130B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DATE             PIC 9(9) VALUE 100000101.
       01  WS-DAYS             PIC 9(8).
       01  WS-W                PIC 9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (99991231).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DATE (WS-DATE).
           DISPLAY WS-DAYS.
           COMPUTE WS-DAYS = FUNCTION INTEGER-OF-DAY (10000001).
           DISPLAY WS-DAYS.
           COMPUTE WS-W = FUNCTION DATE-OF-INTEGER (3067671).
           DISPLAY WS-W.
           COMPUTE WS-W = FUNCTION DATE-OF-INTEGER (3067672).
           DISPLAY WS-W.
           COMPUTE WS-W = FUNCTION DAY-OF-INTEGER (3067672).
           DISPLAY WS-W.
           STOP RUN.
