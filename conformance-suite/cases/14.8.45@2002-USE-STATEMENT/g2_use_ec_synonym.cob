      *>--- conformance-case
      *> id: g2_use_ec_synonym
      *> rules: [2002-SR-14.8.45.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR12 - "EC is synonymous with EXCEPTION
      *>   CONDITION." D1 is written USE AFTER EC, D2 USE AFTER EXCEPTION
      *>   CONDITION; both are format 3 and select their own condition.
      *> Flow: COMPUTE Q = 5 * 9 into PIC S9 with no SIZE ERROR phrase
      *>   sets EC-SIZE-TRUNCATION; COMPUTE Q = 5 / N with N = 0 sets
      *>   EC-SIZE-ZERO-DIVIDE (§2002:14.6.4). Checking is on, so each runs
      *>   the declarative naming it (§2002:14.5.12.1.2 item 2), whose
      *>   FUNCTION EXCEPTION-STATUS is the 31-character, left-justified
      *>   exception-name (§2002:15.28.2 RV1). RESUME AT NEXT STATEMENT
      *>   continues after the interrupted COMPUTE (§2002:14.8.31.3 GR2).
      *> Each leg can fail: a refusal of either spelling fails the
      *>   compile; a spelling that bound differently (or not at all)
      *>   loses its line or selects the wrong declarative.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 Q PIC S9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EC EC-SIZE-TRUNCATION.
       D1-P.
           DISPLAY "EC-FORM [" FUNCTION EXCEPTION-STATUS "]"
           RESUME AT NEXT STATEMENT.
       D2 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
       D2-P.
           DISPLAY "LONG-FORM [" FUNCTION EXCEPTION-STATUS "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       M SECTION.
       M1.
           COMPUTE Q = 5 * 9
           DISPLAY "AFTER-1"
           COMPUTE Q = 5 / N
           DISPLAY "AFTER-2"
           STOP RUN.
