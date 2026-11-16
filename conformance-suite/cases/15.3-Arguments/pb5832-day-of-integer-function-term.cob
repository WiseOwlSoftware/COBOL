      *>--- conformance-case
      *> id: pb5832-day-of-integer-function-term
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 - the integer argument of DAY-OF-INTEGER
      *> written as a sum with a numeric function, which does not
      *> always result in an integer value.
      *>
      *> §1989:A.2.13.3 AR1 (cite.py --check): "Argument-1 is a
      *> positive integer", and §1989:A.2.2 AR4: "An arithmetic
      *> expression which will always result in an integer value
      *> must be specified". With N = 2, SQRT (N) + 1 is
      *> 2.41421..., so the program is refused (COBOL1627) at every
      *> edition (§2023:15.24.3 AR1, §2023:15.3 AR6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9 VALUE 2.
       01  R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION DAY-OF-INTEGER (FUNCTION SQRT (N) + 1).
           STOP RUN.
