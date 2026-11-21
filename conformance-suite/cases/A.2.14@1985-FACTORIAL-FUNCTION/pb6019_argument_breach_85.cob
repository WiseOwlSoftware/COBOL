      *>--- conformance-case
      *> id: pb6019_argument_breach_85
      *> rules: [1989-AR-A.2.14.3-1, 1989-AR-A.2.40.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6019 - at COBOL-85 an intrinsic argument whose
      *> value breaks its function's argument rules terminates the
      *> run unit, the same path an integer argument that is not an
      *> integer takes (pb5889_run_time_check_85).
      *>
      *> §1989:A.2.14.3 AR1 (cite.py --check): "Argument-1 must be
      *> an integer greater than or equal to zero." N - 5 is -4.
      *> §1989:A.2.2 p3: if the arguments "do not have values that
      *> comply with the specified constraints" the returned value
      *> is undefined, and the Correction Amendment's clarification
      *> "allows an implementation of amended third Standard COBOL
      *> to terminate processing" (§1985:p.B-46). The run unit is
      *> terminated with a message naming the function and the
      *> value, never continued with a made-up 0.
      *> EXPECTED: SQRT (4) is 2 (§1989:A.2.40.4 RV1 "the absolute
      *> value of the approximation of the square root"), shown as
      *> SQRT 2; BEFORE is shown; the FACTORIAL reference ends the
      *> run, so AFTER is never shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6019F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S9 VALUE 1.
       01  Q  PIC 9 VALUE 4.
       01  S  PIC 9.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           COMPUTE S = FUNCTION SQRT (Q).
           DISPLAY "SQRT " S.
           DISPLAY "BEFORE".
           COMPUTE R = FUNCTION FACTORIAL (N - 5).
           DISPLAY "AFTER " R.
           STOP RUN.
