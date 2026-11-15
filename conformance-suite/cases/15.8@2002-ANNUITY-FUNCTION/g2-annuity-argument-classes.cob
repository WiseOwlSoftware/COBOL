      *>--- conformance-case
      *> id: g2-annuity-argument-classes
      *> rules: [2002-AR-15.8.2-1, 2002-AR-15.8.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
      *> §2002:15.8.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric data
      *> item shall be specified." Legs A1-A4 write a class-numeric
      *> argument-1 of four shapes (literal, display item, arithmetic
      *> expression, packed-decimal item); each must be ACCEPTED and
      *> its value used. Values from the equivalent arithmetic
      *> expression of §2002:15.8.3 RV1 (native arithmetic is in
      *> effect, so an approximation of it), COMPUTE ROUNDED into
      *> +9.9(4):
      *>   A1 ANNUITY(0 4)       arg-1 zero: 1/4            -> +0.2500
      *>   A2 ANNUITY(W-RATE 1)  1/(1-2**-1) = 2            -> +2.0000
      *>   A3 ANNUITY(W-RATE / 2 1) .5/(1-1.5**-1) = 1.5    -> +1.5000
      *>   A4 ANNUITY(W-PK 2)    .1/(1-1.1**-2) = .576190.. -> +0.5762
      *> §2002:15.8.2 AR3 - "Argument-2 shall be a positive integer."
      *> §2002:15.2 AR5 - "An arithmetic expression that will always
      *> result in an integer value or an integer data item shall be
      *> specified." B1-B3 write a positive integer argument-2 as an
      *> integer item, an integer expression and a literal:
      *>   B1 ANNUITY(1 W-N), W-N = 2   1/(1-2**-2) = 4/3   -> +1.3333
      *>   B2 ANNUITY(1 W-N + 1)        1/(1-2**-3) = 8/7   -> +1.1429
      *>   B3 ANNUITY(0 3)              1/3                 -> +0.3333
      *> C1/C2 write an integer argument-2 whose VALUE is zero and
      *> then -2, which is not positive: §2002:15.2 "the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist";
      *> checking is ON (§2002:7.2.19.3 GR4), so the declarative is
      *> executed (§2002:14.5.12.1.2 "the associated declarative is
      *> executed"), and FUNCTION EXCEPTION-STATUS returns the name
      *> left-justified in 31 characters (§2002:15.28.2 RV1). RESUME
      *> AT NEXT STATEMENT continues after the COMPUTE (§2002:14.8.31.3
      *> GR2), so the declarative does not complete normally and the
      *> run unit is not terminated. The receiving item is not shown
      *> after an interrupted statement. EC-COUNT=2 shows the
      *> declarative ran for C1 and C2 only: a positive leg that
      *> raised the condition, or a zero/negative leg that did not,
      *> changes the count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ANN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-RATE  PIC 9V9 VALUE 1.0.
       01  W-PK    PIC S9V99 USAGE PACKED-DECIMAL VALUE 0.10.
       01  W-N     PIC S9(3) VALUE 2.
       01  W-K     PIC S9(3) VALUE 0.
       01  W-EC    PIC 9 VALUE 0.
       01  R       PIC +9.9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-ARG SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       D-ARG-P.
           ADD 1 TO W-EC
           DISPLAY "EC [" FUNCTION EXCEPTION-STATUS "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M SECTION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ANNUITY(0 4)
           DISPLAY "A1=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(W-RATE 1)
           DISPLAY "A2=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(W-RATE / 2 1)
           DISPLAY "A3=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(W-PK 2)
           DISPLAY "A4=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(1 W-N)
           DISPLAY "B1=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(1 W-N + 1)
           DISPLAY "B2=" R
           COMPUTE R ROUNDED = FUNCTION ANNUITY(0 3)
           DISPLAY "B3=" R
           DISPLAY "C1"
           COMPUTE R ROUNDED = FUNCTION ANNUITY(0.1 W-K)
           DISPLAY "C1-AFTER"
           MOVE -2 TO W-K
           DISPLAY "C2"
           COMPUTE R ROUNDED = FUNCTION ANNUITY(0.1 W-K)
           DISPLAY "C2-AFTER"
           DISPLAY "EC-COUNT=" W-EC
           STOP RUN.
