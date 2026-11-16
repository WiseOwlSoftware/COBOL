      *>--- conformance-case
      *> id: g85_bf42_03
      *> rules: [1989-FMT-A.2.34.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.34.2 - FUNCTION PRESENT-VALUE ( argument-1 { argument-2 } ... ):
      *> one rate and one or more amounts. Values from §1989:A.2.34.4 RV1:
      *> sum of argument-2 / (1 + argument-1) ** n, n = 1, 2, ...; each
      *> result is ROUNDED to 2 places, so the approximation shows exactly.
      *>   PV (1 100)              100/2                     -> 50.00
      *>   PV (1 100 100)          100/2 + 100/4             -> 75.00
      *>   PV (0, 10, 20, 30)      10 + 20 + 30              -> 60.00
      *>   PV (0.25 125)           125/1.25                  -> 100.00
      *>   PV (-0.5 50)            50/0.5 (AR2: > -1 legal)  -> 100.00
      *>   PV (A - 4 100)          rate 1, 100/2             -> 50.00
      *>   PV (RATE AMT (ALL))     110/1.1 + 121/1.21        -> 200.00
      *> A leg fails if a count of argument-2, a separator or an argument
      *> form is refused, or a term is omitted or mis-exponented.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4203.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                    PIC 9(3)    VALUE 5.
       01  WS-RATE                 PIC 9V99    VALUE 0.10.
       01  WS-TAB.
           05  WS-AMT              PIC 9(3)    OCCURS 2 TIMES.
       01  WS-R                    PIC 9(6)V99.
       01  WS-SHOW                 PIC Z(5)9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 110 TO WS-AMT (1).
           MOVE 121 TO WS-AMT (2).
           COMPUTE WS-R ROUNDED = FUNCTION PRESENT-VALUE (1 100).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED = FUNCTION PRESENT-VALUE (1 100 100).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED =
               FUNCTION PRESENT-VALUE (0, 10, 20, 30).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED = FUNCTION PRESENT-VALUE (0.25 125).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED = FUNCTION PRESENT-VALUE (-0.5 50).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED =
               FUNCTION PRESENT-VALUE (WS-A - 4 100).
           PERFORM SHOW-PARA.
           COMPUTE WS-R ROUNDED =
               FUNCTION PRESENT-VALUE (WS-RATE WS-AMT (ALL)).
           PERFORM SHOW-PARA.
           STOP RUN.
       SHOW-PARA.
           MOVE WS-R TO WS-SHOW.
           DISPLAY WS-SHOW.
