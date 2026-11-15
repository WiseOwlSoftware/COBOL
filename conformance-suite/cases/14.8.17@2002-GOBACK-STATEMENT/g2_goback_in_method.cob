      *>--- conformance-case
      *> id: g2_goback_in_method
      *> rules: [2002-GR-14.8.17.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.17.3 GR4 - "If a GOBACK statement is executed in a
      *>   method, the method operates as if executing an EXIT METHOD
      *>   statement with the RAISING phrase, if any, that is specified
      *>   in the GOBACK statement."
      *> §2002:14.8.13.3 GR7 - EXIT METHOD "causes the executing method
      *>   to terminate, and control to return to the invoking
      *>   statement"; "the value in the data item referenced by the
      *>   RETURNING phrase becomes the result of the method invocation".
      *> §2002:14.8.13.3 GR3 / GR3a - with RAISING EXCEPTION, "an
      *>   exception condition is raised in the activating runtime
      *>   element if checking for that exception condition is enabled
      *>   in the activating runtime element"; it is exception-name-1.
      *> §2002:14.5.12.1.5 - EC-SIZE-OVERFLOW is Fatal; §2002:14.5.12.1.2
      *>   item 2 - checking enabled and an applicable USE: "the
      *>   associated declarative is executed"; its RESUME AT NEXT
      *>   STATEMENT (§2002:14.8.31.3 GR2) continues after the INVOKE.
      *> DERIVATION.
      *>   IN-M1, AFTER-M1 - M1's GOBACK ends the method (GR7): the
      *>     NOT-REACHED-M1 line fails a GOBACK that falls through.
      *>   AFTER-M3 R=042 - M3's GOBACK returns LR (42) as the result;
      *>     a GOBACK that fell through would return 99.
      *>   IN-M2, DECL EC-SIZE-OVERFLOW, AFTER-M2 - M2's GOBACK RAISING
      *>     raises EC-SIZE-OVERFLOW in the invoker (GR3a): its USE
      *>     procedure runs and resumes after the INVOKE. A GOBACK that
      *>     ignored RAISING (the main-program rule, GR2) prints no DECL.
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GBK01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2GBK01C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2GBK01C.
       01 R PIC 9(3) VALUE 0.
       01 WS-EC PIC X(16).
       PROCEDURE DIVISION.
       DECLARATIVES.
       HU SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
       HU-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EC
           DISPLAY "DECL " WS-EC.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN.
           INVOKE G2GBK01C "NEW" RETURNING O.
           INVOKE O "M1".
           DISPLAY "AFTER-M1".
           INVOKE O "M3" RETURNING R.
           DISPLAY "AFTER-M3 R=" R.
           INVOKE O "M2".
           DISPLAY "AFTER-M2".
           STOP RUN.
       END PROGRAM G2GBK01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2GBK01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       M1-P.
           DISPLAY "IN-M1".
           GOBACK.
           DISPLAY "NOT-REACHED-M1".
       END METHOD M1.
       METHOD-ID. M2.
       PROCEDURE DIVISION.
       M2-P.
           DISPLAY "IN-M2".
           GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW.
           DISPLAY "NOT-REACHED-M2".
       END METHOD M2.
       METHOD-ID. M3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC 9(3).
       PROCEDURE DIVISION RETURNING LR.
       M3-P.
           MOVE 42 TO LR.
           GOBACK.
           MOVE 99 TO LR.
       END METHOD M3.
       END OBJECT.
       END CLASS G2GBK01C.
