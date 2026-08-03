      *> PB21 - a FLOATING-POINT argument to a 15.3 type-6 INTEGER function emitted a call to a runtime member
      *> that DOES NOT EXIST, so conforming source failed Roslyn with a raw CS0117.
      *>
      *> WHY IT IS CONFORMING: a COMP-2 item is category numeric (8.5.2.12 item 2) hence class numeric
      *> (8.5.2.1 Table 2), and 15.3's INTEGER type resolves through CLASS numeric - the integer-ness is a VALUE
      *> property, not a class the argument screen can reject on. So these calls are legal and must compute.
      *>
      *> ⛔ THE GUARD THAT EXISTED TO PREVENT THIS HAD TWO BLIND SPOTS, and the batch's "three missing members"
      *> was an under-count of TEN: IntrinsicRealArgDriftTests scoped on ArgKinds 'n' alone (exempting exactly the
      *> 'i' family this is about) AND its case-label regex captured only the FIRST name of an or-chain, so
      *>     case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
      *> contributed one name and hid three. Both are fixed; the guard now reports the whole class.
      *>
      *> All conversions funnel through ONE landing helper so a float operand gets the IDENTICAL disposition a
      *> fixed-point one gets - a function's value must not depend on how its argument happened to be stored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB21FLOAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-DAY  COMP-2 VALUE 1995046.
       01 D-DATE COMP-2 VALUE 19950215.
       01 D-INT  COMP-2 VALUE 143951.
       01 D-YY   COMP-2 VALUE 95.
       01 D-FAC  COMP-2 VALUE 5.
       01 R      PIC 9(9).
       01 F      PIC 9(12).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(D-DAY)
           DISPLAY "IODAY=" R
           COMPUTE R = FUNCTION INTEGER-OF-DATE(D-DATE)
           DISPLAY "IODATE=" R
           COMPUTE R = FUNCTION DAY-OF-INTEGER(D-INT)
           DISPLAY "DAYOFINT=" R
           COMPUTE R = FUNCTION DATE-OF-INTEGER(D-INT)
           DISPLAY "DATEOFINT=" R
           COMPUTE R = FUNCTION YEAR-TO-YYYY(D-YY, 50, 2000)
           DISPLAY "Y2YYYY=" R
           COMPUTE F = FUNCTION FACTORIAL(D-FAC)
           DISPLAY "FACT=" F
      *> The float path must agree with the FIXED-POINT path for the same value - that is the invariant, and it
      *> is why one landing helper serves both rather than two conversions that could drift apart.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(1995046)
           DISPLAY "IODAY-FIXED=" R
           STOP RUN.
