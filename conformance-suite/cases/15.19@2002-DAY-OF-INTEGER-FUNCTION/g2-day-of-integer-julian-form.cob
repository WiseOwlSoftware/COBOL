      *>--- conformance-case
      *> id: g2-day-of-integer-julian-form
      *> rules: [2002-RV-15.19.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.19.3 RV2 - "The returned value is an integer of the
      *> form (YYYYDDD) where YYYY represents a year in the Gregorian
      *> calendar and DDD represents the day of that year."
      *> §2002:15.4: integer date 1 is "Monday, January 1, 1601" and
      *> leap years follow ISO 8601:2000 (divisible by 4, a century
      *> year only when divisible by 400). Each leg is derived by
      *> counting days from that origin:
      *>   I1    1        Jan 1 1601            -> 1601001
      *>   I2    59       Feb 28 1601           -> 1601059
      *>   I3    60       Mar 1 1601 (no leap)  -> 1601060
      *>   I4    36219    Dec 31 1699 is 99*365+24 = 36159; +60 is
      *>                  Mar 1 1700 (1700 not leap) -> 1700060
      *>   I5    36524    36159 + 365, Dec 31 1700 -> 1700365
      *>   I6    145791   Dec 31 1999 = 146097 - 366 = 145731; +60 is
      *>                  Feb 29 2000 (2000 leap)    -> 2000060
      *>   I7    146097   400 Gregorian years, Dec 31 2000 -> 2000366
      *>   I8    146522   146097 + 365 + 60, day 60 of 2002 -> 2002060
      *>   I9    3067671  the AR1 maximum, Dec 31 9999 -> 9999365
      *> The YYYY/DDD split (R / 1000, MOD(R, 1000)) shows the form:
      *> DDD is the ordinal day (1..366), never a month-day pair.
      *> A leg fails if the returned value is not seven digits YYYYDDD,
      *> if the origin is off by one, or if a century year is counted
      *> leap wrongly (I4/I5 versus I6/I7).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DOI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R       PIC 9(7).
       01  YYYY    PIC 9(4).
       01  DDD     PIC 9(3).
       01  W-I     PIC 9(7) VALUE 146097.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION DAY-OF-INTEGER(1) TO R
           DISPLAY "I1=" R
           MOVE FUNCTION DAY-OF-INTEGER(59) TO R
           DISPLAY "I2=" R
           MOVE FUNCTION DAY-OF-INTEGER(60) TO R
           DISPLAY "I3=" R
           MOVE FUNCTION DAY-OF-INTEGER(36219) TO R
           DISPLAY "I4=" R
           MOVE FUNCTION DAY-OF-INTEGER(36524) TO R
           DISPLAY "I5=" R
           MOVE FUNCTION DAY-OF-INTEGER(145791) TO R
           DISPLAY "I6=" R
           MOVE FUNCTION DAY-OF-INTEGER(W-I) TO R
           DISPLAY "I7=" R
           MOVE FUNCTION DAY-OF-INTEGER(W-I + 425) TO R
           DISPLAY "I8=" R
           MOVE FUNCTION DAY-OF-INTEGER(3067671) TO R
           DISPLAY "I9=" R
           COMPUTE YYYY = FUNCTION INTEGER(
               FUNCTION DAY-OF-INTEGER(145791) / 1000)
           COMPUTE DDD = FUNCTION MOD(FUNCTION DAY-OF-INTEGER(145791),
               1000)
           DISPLAY "SPLIT=" YYYY "/" DDD
           STOP RUN.
