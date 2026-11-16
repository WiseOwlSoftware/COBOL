      *>--- conformance-case
      *> id: g85_bf42_05
      *> rules: [1989-FMT-A.2.40.2, 1989-AR-A.2.40.3-1, 1989-AR-A.2.40.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.40.2 - FUNCTION SQRT ( argument-1 ): one argument.
      *> §1989:A.2.40.3 AR1 - argument-1 must be class numeric: each argument
      *> below is numeric in a different form (§1989:A.2.2: literal, unsigned
      *> and signed identifier, arithmetic expression).
      *> §1989:A.2.40.3 AR2 - argument-1 zero or positive: the zero leg and the
      *> positive legs are the permitted values, accepted and evaluated.
      *> Values from §1989:A.2.40.4 RV1 (absolute value of the approximate
      *> square root), ROUNDED to 4 places:
      *>   SQRT (0)              -> 0.0000   (AR2 zero)
      *>   SQRT (16)             -> 4.0000
      *>   SQRT (0.0625)         -> 0.2500
      *>   SQRT (W) W = 2.25     -> 1.5000
      *>   SQRT (V) V = +144     -> 12.0000  (signed numeric item)
      *>   SQRT (3 * 3 + 4 * 4)  -> 5.0000   (arithmetic expression)
      *>   SQRT (2)              -> 1.4142   (1.41421356... rounded)
      *> A leg fails if a numeric form or a zero value is refused, or a
      *> value is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4205.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-W                    PIC 9V99    VALUE 2.25.
       01  WS-V                    PIC S9(3)   VALUE +144.
       01  WS-S                    PIC 9(4)V9(4).
       01  WS-SHOW                 PIC Z(3)9.9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (0).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (16).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (0.0625).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (WS-W).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (WS-V).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (3 * 3 + 4 * 4).
           PERFORM SHOW-PARA.
           COMPUTE WS-S ROUNDED = FUNCTION SQRT (2).
           PERFORM SHOW-PARA.
           STOP RUN.
       SHOW-PARA.
           MOVE WS-S TO WS-SHOW.
           DISPLAY WS-SHOW.
