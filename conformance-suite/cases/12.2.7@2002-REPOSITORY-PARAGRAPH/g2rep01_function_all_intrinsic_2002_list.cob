      *>--- conformance-case
      *> id: g2rep01_function_all_intrinsic_2002_list
      *> rules: [2002-GR-12.2.7.3-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.7.3 GR14 - "If ALL is specified in the intrinsic
      *> format of the function-specifier, it is as if each of the
      *> intrinsic-function-names defined in 8.11, Intrinsic function
      *> names, were specified."  cite.py --check -> OK
      *> §2002:12.2.7.3 GR13 - a specified intrinsic-function-name "may be
      *> specified in a function-identifier without being preceded by the
      *> word FUNCTION"  cite.py --check -> OK
      *> §2002:12.2.7.2 SR13 forbids only "the names of the intrinsic
      *> functions" as user-defined words  cite.py --check -> OK; the
      *> list is §2002:8.11 ("The following is the list of intrinsic
      *> function names" -> cite.py OK), which has no TRIM and no
      *> SECONDS-PAST-MIDNIGHT (both added after 2002).
      *> LEGS: nine §2002:8.11 names from across the list (ABS, FACTORIAL,
      *> INTEGER-OF-DATE, MAX, ORD-MAX, REVERSE, SQRT, TEST-NUMVAL,
      *> UPPER-CASE) are called with no FUNCTION word; each fails if GR14
      *> omits that name (it is then an undefined data-name). TRIM and
      *> SECONDS-PAST-MIDNIGHT are data-names; that leg fails if the
      *> compiler applies a later edition's list under ALL at 2002.
      *> DERIVATION: ABS(-7)=7; FACTORIAL(5)=5*4*3*2*1=120; INTEGER-OF-DATE
      *> (16010101)=1 (§2002:15.36.3 RV1: days "the date represented by
      *> argument-1 succeeds December 31, 1600" -> cite.py OK); MAX(3 7 5)
      *> =7; ORD-MAX(3 9 1)=2 (§2002:15.58.3 RV1: "the position of the
      *> argument-1 having the greatest value" -> cite.py OK); REVERSE
      *> ("ABC")="CBA"; SQRT(16)=4; TEST-NUMVAL("12")=0 (§2002:15.75.3
      *> RV1: 0 "If the content of argument-1 conforms to the argument
      *> rules for the NUMVAL function" -> cite.py OK); UPPER-CASE("abc")
      *> ="ABC". N is PIC 9(5), so each integer shows as five digits.
      *> The data-names display their VALUEs "TRIM" and 123.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REP03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TRIM                    PIC X(4) VALUE "TRIM".
       01  SECONDS-PAST-MIDNIGHT   PIC 9(3) VALUE 123.
       01  N                       PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE N = ABS(-7).
           DISPLAY "ABS:" N.
           COMPUTE N = FACTORIAL(5).
           DISPLAY "FACTORIAL:" N.
           COMPUTE N = INTEGER-OF-DATE(16010101).
           DISPLAY "INTEGER-OF-DATE:" N.
           COMPUTE N = MAX(3 7 5).
           DISPLAY "MAX:" N.
           COMPUTE N = ORD-MAX(3 9 1).
           DISPLAY "ORD-MAX:" N.
           DISPLAY "REVERSE:" REVERSE("ABC").
           COMPUTE N = SQRT(16).
           DISPLAY "SQRT:" N.
           COMPUTE N = TEST-NUMVAL("12").
           DISPLAY "TEST-NUMVAL:" N.
           DISPLAY "UPPER-CASE:" UPPER-CASE("abc").
           DISPLAY "TRIM:" TRIM.
           DISPLAY "SECONDS-PAST-MIDNIGHT:" SECONDS-PAST-MIDNIGHT.
           STOP RUN.
