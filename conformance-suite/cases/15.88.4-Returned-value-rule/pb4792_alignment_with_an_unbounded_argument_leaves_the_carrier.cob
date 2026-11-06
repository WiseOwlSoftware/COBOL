      *>--- conformance-case
      *> id: pb4792_alignment_with_an_unbounded_argument_leaves_the_carrier
      *> rules: [RV-15.88.4-1, RV-15.76.4-1, RV-15.61.4-2, RV-15.61.4-1, RV-15.62.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4792 (the function arm of PB4752): under NATIVE arithmetic SUM, RANGE, MEDIAN and MIDRANGE align every argument to the
      *> list's largest scale before they add, and an argument with no digit bound of its own (the value of FUNCTION INTEGER) is bounded
      *> only by its 16-byte carrier, so aligning it beside a fraction leaves the 128-bit lane. The value does not: D = 1000000000000000001
      *> beside F = 1.01 (V9(30)) is a 49-digit integer aligned and a plain 1000000000000000002.01 summed, and the receivers hold it.
      *> The run unit must not end in EC-SIZE-OVERFLOW for a value its receiver holds (15.4.1: "the value returned is an
      *> implementor-defined approximation of the value of that expression", cite.py: OK 15.4.1; the arithmetic expression's value is
      *> the exact one). Expected values are Python Decimal arithmetic, truncated into the receiver (no ROUNDED phrase):
      *>   S1  SUM (FUNCTION INTEGER (D) F) = D + F (15.88.4 1) b), cite.py: OK)           -> 1000000000000000002.010
      *>   S2  RANGE (FUNCTION INTEGER (D) F) = MAX - MIN = D - F (15.76.4 1))             -> 0999999999999999999.990
      *>   S3  MEDIAN (FUNCTION INTEGER (D) F), even: (D + F) / 2 (15.61.4 2))             -> 0500000000000000001.005
      *>   S4  MIDRANGE (FUNCTION INTEGER (D) F) = (MAX + MIN) / 2 (15.62.4 1))            -> 0500000000000000001.005
      *>   S5  MEDIAN (FUNCTION INTEGER (D) F G), odd, G = 0.5: the middle value F          -> 0000000000000000001.010
      *>   S6  control, no argument shifted: SUM (FUNCTION INTEGER (D) FUNCTION INTEGER (E)) -> 1000000000000000002.000
      *> Review round 1 (R1-1): a list whose exact aligned value FITS the carrier is never rounded to the SDIDI's 34 digits. A = 10000 and
      *> H = .9999999999999999999999999999999 (V9(31)): SUM is 10000.9999999999999999999999999999999 (36 digits; its 34-digit image rounds
      *> up to 10001.000...), stored by truncation at five places it is 10000.99999, as FUNCTION INTEGER (A) + H and SUM (A H) give:
      *>   T1  SUM (FUNCTION INTEGER (A) H)       -> 10000.99999
      *>   T2  RANGE (FUNCTION INTEGER (A) H) = A - H = 9999.0000000000000000000000000000001 -> 09999.00000
      *>   T3  MIDRANGE (FUNCTION INTEGER (A) H) = (A + H) / 2 = 5000.49999999...            -> 05000.49999
      *>   T4  MEDIAN (FUNCTION INTEGER (A) H), even, the same mean                         -> 05000.49999
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4792ALN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9(19) VALUE 1000000000000000001.
       01 E  PIC 9(19) VALUE 1.
       01 F  PIC 9V9(30) VALUE 1.010000000000000000000000000000.
       01 G  PIC 9V9 VALUE 0.5.
       01 R1 PIC 9(19)V999.
       01 A  PIC 9(5) VALUE 10000.
       01 H  PIC V9(31) VALUE .9999999999999999999999999999999.
       01 R2 PIC 9(5)V9(5).
       PROCEDURE DIVISION.
           COMPUTE R1 = FUNCTION SUM (FUNCTION INTEGER (D) F)
           DISPLAY "S1 " R1
           COMPUTE R1 = FUNCTION RANGE (FUNCTION INTEGER (D) F)
           DISPLAY "S2 " R1
           COMPUTE R1 = FUNCTION MEDIAN (FUNCTION INTEGER (D) F)
           DISPLAY "S3 " R1
           COMPUTE R1 = FUNCTION MIDRANGE (FUNCTION INTEGER (D) F)
           DISPLAY "S4 " R1
           COMPUTE R1 = FUNCTION MEDIAN (FUNCTION INTEGER (D) F G)
           DISPLAY "S5 " R1
           COMPUTE R1 = FUNCTION SUM (FUNCTION INTEGER (D)
                                      FUNCTION INTEGER (E))
           DISPLAY "S6 " R1
           COMPUTE R2 = FUNCTION SUM (FUNCTION INTEGER (A) H)
           DISPLAY "T1 " R2
           COMPUTE R2 = FUNCTION RANGE (FUNCTION INTEGER (A) H)
           DISPLAY "T2 " R2
           COMPUTE R2 = FUNCTION MIDRANGE (FUNCTION INTEGER (A) H)
           DISPLAY "T3 " R2
           COMPUTE R2 = FUNCTION MEDIAN (FUNCTION INTEGER (A) H)
           DISPLAY "T4 " R2
           STOP RUN.
       END PROGRAM PB4792ALN.
