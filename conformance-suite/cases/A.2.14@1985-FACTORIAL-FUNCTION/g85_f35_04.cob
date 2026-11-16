      *>--- conformance-case
      *> id: g85_f35_04
      *> rules: [1989-FMT-A.2.14.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.14.2 - the general format "FUNCTION FACTORIAL
      *> ( argument-1 )": one parenthesised integer argument, given as a
      *> literal, an integer data item and an always-integral arithmetic
      *> expression (§1989:A.2.2 AR4). §1989:A.2.3 GR3 confines an
      *> integer function to an arithmetic expression, hence COMPUTE.
      *> Values: §1989:A.2.14.4 RV1 "If the value of argument-1 is
      *> zero, the value 1 is returned." and RV2 "If the value of
      *> argument-1 is positive, its factorial is returned.":
      *>   FACTORIAL (0)        = 1
      *>   FACTORIAL (1)        = 1
      *>   FACTORIAL (W-I = 5)  = 120
      *>   FACTORIAL (W-I + 5)  = 10! = 3628800
      *> Each leg fails if the format is refused at 85 or a value is
      *> wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3504.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I      PIC 9(2) VALUE 5.
       01  W-F      PIC 9(7).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-F = FUNCTION FACTORIAL (0).
           DISPLAY "F0=" W-F.
           COMPUTE W-F = FUNCTION FACTORIAL (1).
           DISPLAY "F1=" W-F.
           COMPUTE W-F = FUNCTION FACTORIAL (W-I).
           DISPLAY "F5=" W-F.
           COMPUTE W-F = FUNCTION FACTORIAL (W-I + 5).
           DISPLAY "F10=" W-F.
           STOP RUN.
