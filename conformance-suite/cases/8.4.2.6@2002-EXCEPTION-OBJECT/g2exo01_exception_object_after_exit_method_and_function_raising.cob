      *>--- conformance-case
      *> id: g2exo01_exception_object_after_exit_method_and_function_raising
      *> rules: [2002-GR-8.4.2.6.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.6.3 GR1 - "EXCEPTION-OBJECT references the current
      *> exception object. If an exception object is not associated with
      *> the current exception, EXCEPTION-OBJECT is set to null."
      *> At 2002 the current exception object can also be made current by
      *> EXIT METHOD RAISING identifier-1 and EXIT FUNCTION RAISING
      *> identifier-1 (§2002:14.8.13.1 formats 3 and 4): "the object
      *> referenced by identifier-1 becomes the current exception object
      *> in the activating runtime element" (§2002:14.8.13.3 GR3b), and
      *> "EXCEPTION-OBJECT is set to the content of the object reference
      *> specified in the ... RAISING phrase of the EXIT or GOBACK
      *> statement" (§2002:14.5.12.1.4). An exception object is always
      *> enabled (§2002:7.2.19.2 SR2 NOTE). The USE AFTER EXCEPTION
      *> OBJECT G2EXOX declarative of the activating program applies
      *> (§2002:14.8.45.3 GR13a); during it "EXCEPTION-OBJECT references
      *> the exception object" (§2002:14.8.45.3 GR14); after it completes
      *> "execution continues as specified in the activating statement
      *> for normal execution" (§2002:14.5.12.1.4).
      *> E and F are two DISTINCT G2EXOX instances; "=" on object
      *> references tests identity, so each leg fails if the register
      *> holds the other object, a copy, or null.
      *>   START-NULL  nothing raised yet: null (GR1).
      *>   DECL-E      EXIT METHOD RAISING LK-E (LK-E is E's object).
      *>   AFTER-E     back in MAIN after the INVOKE: still E (GR1).
      *>   DECL-F      EXIT FUNCTION RAISING LK-F (F's object).
      *>   V=7         the function's RETURNING value 7 is its result
      *>               (§2002:14.8.13.3 GR8), the MOVE then completes.
      *>   AFTER-F     still F, not E (GR1).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXOF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXOX.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-F USAGE OBJECT REFERENCE G2EXOX.
       01  R    PIC 9.
       PROCEDURE DIVISION USING LK-F RETURNING R RAISING G2EXOX.
       MAIN-PARA.
           MOVE 7 TO R
           EXIT FUNCTION RAISING LK-F.
       END FUNCTION G2EXOF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXO01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXOX
           CLASS G2EXOC
           FUNCTION G2EXOF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C USAGE OBJECT REFERENCE G2EXOC.
       01  E USAGE OBJECT REFERENCE G2EXOX.
       01  F USAGE OBJECT REFERENCE G2EXOX.
       01  V PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EO-SEC SECTION.
           USE AFTER EXCEPTION OBJECT G2EXOX.
       EO-P.
           IF EXCEPTION-OBJECT = E
               DISPLAY "DECL-E"
           ELSE
               IF EXCEPTION-OBJECT = F
                   DISPLAY "DECL-F"
               ELSE
                   DISPLAY "DECL-NEITHER"
               END-IF
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "START-NULL"
           ELSE
               DISPLAY "START-NOT-NULL"
           END-IF
           INVOKE G2EXOC "NEW" RETURNING C
           INVOKE G2EXOX "NEW" RETURNING E
           INVOKE G2EXOX "NEW" RETURNING F
           INVOKE C "THROW" USING BY CONTENT E
           IF EXCEPTION-OBJECT = E
               DISPLAY "AFTER-E"
           ELSE
               DISPLAY "AFTER-NOT-E"
           END-IF
           MOVE FUNCTION G2EXOF (F) TO V
           DISPLAY "V=" V
           IF EXCEPTION-OBJECT = F
               DISPLAY "AFTER-F"
           ELSE
               DISPLAY "AFTER-NOT-F"
           END-IF
           STOP RUN.
       END PROGRAM G2EXO01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2EXOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXOC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS G2EXOX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. THROW.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-E USAGE OBJECT REFERENCE G2EXOX.
       PROCEDURE DIVISION USING LK-E RAISING G2EXOX.
       MAIN-PARA.
           EXIT METHOD RAISING LK-E.
       END METHOD THROW.
       END OBJECT.
       END CLASS G2EXOC.
