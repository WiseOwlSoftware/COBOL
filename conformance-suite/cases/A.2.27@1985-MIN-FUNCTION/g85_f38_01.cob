      *>--- conformance-case
      *> id: g85_f38_01
      *> rules: [1989-RV-A.2.27.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.27.4 RV3 - an alphanumeric MIN returns a value
      *> the size of the SELECTED argument-1, not of the longest or
      *> shortest argument. Each leg DISPLAYs the result between
      *> brackets and its FUNCTION LENGTH. Leg 1 selects the 2-char
      *> argument from sizes 6/2/4 (a result padded to the longest
      *> prints [AB    ] 06); leg 2 selects the 7-char argument over
      *> a 4-char one (a result cut to the shortest prints [ABAC]);
      *> leg 3 mixes an alphabetic and an alphanumeric argument
      *> (§1989:A.2.27.3 AR1; the function type is alphanumeric per
      *> §1989:A.2.27.1) and selects the 3-char alphabetic one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3801.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-LONG      PIC X(6) VALUE "ZEBRAS".
       01  W-SHORT     PIC X(2) VALUE "AB".
       01  W-MID       PIC X(4) VALUE "MANY".
       01  W-SEVEN     PIC X(7) VALUE "ABACUSX".
       01  W-ALPHA     PIC A(3) VALUE "CAT".
       01  W-LEN       PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-LEN = FUNCTION LENGTH
               (FUNCTION MIN (W-LONG W-SHORT W-MID)).
           DISPLAY "LEG1 [" FUNCTION MIN (W-LONG W-SHORT W-MID)
               "] " W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH
               (FUNCTION MIN (W-MID W-SEVEN)).
           DISPLAY "LEG2 [" FUNCTION MIN (W-MID W-SEVEN)
               "] " W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH
               (FUNCTION MIN (W-ALPHA W-MID)).
           DISPLAY "LEG3 [" FUNCTION MIN (W-ALPHA W-MID)
               "] " W-LEN.
           STOP RUN.
