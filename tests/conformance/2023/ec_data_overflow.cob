      *> EC-DATA-OVERFLOW (ISO §14.9.25.4 GR4 step 4a, spec :28634; Table 13 Fatal): a MOVE whose FINITE algebraic
      *> value is farther from zero than a single-precision float receiver can represent — an exponent overflow to
      *> ±Infinity. MOVE-only (an arithmetic ±Inf result is the valid §14.6.8.3 GR1 value, never this EC). A double
      *> receiver cannot overflow from a finite double, and an underflow to zero is not this EC. Observed via a USE
      *> declarative + RESUME AT NEXT STATEMENT.
      >>TURN EC-DATA-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-DOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   USAGE COMP-1.
       01 WS-BIG USAGE COMP-2 VALUE 1.0E300.
       01 WS-D2  USAGE COMP-2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-OVERFLOW.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "R1-OVERFLOW".
           MOVE WS-BIG TO WS-S.
           DISPLAY "R2-DOUBLE-NOOVF".
           MOVE WS-BIG TO WS-D2.
           DISPLAY "R3-UNDERFLOW".
           MOVE 1.0E-50 TO WS-S.
           DISPLAY "DONE".
           STOP RUN.
