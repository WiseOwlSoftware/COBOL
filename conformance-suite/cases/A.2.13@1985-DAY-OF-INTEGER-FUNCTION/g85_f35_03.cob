      *>--- conformance-case
      *> id: g85_f35_03
      *> rules: [1989-RV-A.2.13.4-1, 1989-RV-A.2.13.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.13.4 RV1 - "The returned value represents the Julian
      *> equivalent of the integer specified in argument-1." and
      *> §1989:A.2.13.4 RV2 - "an integer of the form (YYYYDDD) where
      *> YYYY represents a year in the Gregorian calendar and DDD
      *> represents the day of that year." Argument-1 counts days
      *> succeeding 1600-12-31 (§1989:A.2.13.3 AR1).
      *> RV1 (the Julian equivalent of the given day), lines J...:
      *>   59     -> 1601059  31 (Jan) + 28 (Feb 1601) = 1601-02-28
      *>   36524  -> 1700365  100*365 + 24 leap days; 1700 not leap
      *>   36525  -> 1701001
      *>   145792 -> 2000061  2000-03-01: 31 + 29 + 1 (2000 is leap)
      *>   146098 -> 2001001
      *>   146097 -> 2000366  400 Gregorian years end on day 366
      *> RV2 (the YYYYDDD shape), line SPLIT: 146097's value, seen
      *> through a 9(4) year and 9(3) day, gives YYYY=2000 and DDD=366.
      *> A leg fails if a leap rule, the day count or the form is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3503.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-N      PIC 9(6).
       01  W-J      PIC 9(7).
       01  W-JR     REDEFINES W-J.
           05  W-YYYY PIC 9(4).
           05  W-DDD  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (59).
           DISPLAY "J59=" W-J.
           MOVE 36524 TO W-N.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (W-N).
           DISPLAY "J36524=" W-J.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (W-N + 1).
           DISPLAY "J36525=" W-J.
           MOVE 145792 TO W-N.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (W-N).
           DISPLAY "J145792=" W-J.
           MOVE 146097 TO W-N.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (W-N + 1).
           DISPLAY "J146098=" W-J.
           COMPUTE W-J = FUNCTION DAY-OF-INTEGER (W-N).
           DISPLAY "J146097=" W-J.
           DISPLAY "SPLIT YYYY=" W-YYYY " DDD=" W-DDD.
           STOP RUN.
