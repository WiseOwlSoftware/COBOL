      *>--- conformance-case
      *> id: g85_f35_02
      *> rules: [1989-FMT-A.2.12.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.12.2 - the general format "FUNCTION DATE-OF-INTEGER
      *> ( argument-1 )": one parenthesised argument, given here as an
      *> integer literal, an integer data item and an arithmetic
      *> expression (§1989:A.2.2 AR4). §1989:A.2.3 GR3 confines an
      *> integer function to an arithmetic expression, hence COMPUTE.
      *> Values from §1989:A.2.12.3 AR1 (days succeeding 1600-12-31) and
      *> §1989:A.2.12.4 RV2 (form YYYYMMDD):
      *>   1      -> 16010101  the first day after 1600-12-31
      *>   366    -> 16020101  1601 is not a leap year (365 days)
      *>   36524  -> 17001231  1601-1700: 100*365 + 24 leap days
      *>                       (1604..1696; 1700 is not leap)
      *>   145792 -> 20000301  2000-01-01 is 146097-366+1 = 145732;
      *>                       Jan 31 + Feb 29 (2000 is leap) = 60
      *>   365 * 400 + 97 = 146097 -> 20001231, four hundred Gregorian
      *>                       years of 365 days plus 97 leap days
      *> Each leg fails if the format is refused at 85 or a date is
      *> wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3502.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-N      PIC 9(6) VALUE 366.
       01  W-D      PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-D = FUNCTION DATE-OF-INTEGER (1).
           DISPLAY "D1=" W-D.
           COMPUTE W-D = FUNCTION DATE-OF-INTEGER (W-N).
           DISPLAY "D366=" W-D.
           MOVE 36524 TO W-N.
           COMPUTE W-D = FUNCTION DATE-OF-INTEGER (W-N).
           DISPLAY "D36524=" W-D.
           MOVE 145792 TO W-N.
           COMPUTE W-D = FUNCTION DATE-OF-INTEGER (W-N).
           DISPLAY "D145792=" W-D.
           COMPUTE W-D = FUNCTION DATE-OF-INTEGER (365 * 400 + 97).
           DISPLAY "D146097=" W-D.
           STOP RUN.
