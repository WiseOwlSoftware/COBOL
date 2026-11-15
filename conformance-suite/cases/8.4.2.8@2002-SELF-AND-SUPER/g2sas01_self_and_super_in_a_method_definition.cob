      *>--- conformance-case
      *> id: g2sas01_self_and_super_in_a_method_definition
      *> rules: [2002-SR-8.4.2.8.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.8.2 SR1 - "This identifier format may be specified
      *> only in a method definition." The granted form: SELF and SUPER
      *> written inside a method definition (method SHOWME of G2SASC) are
      *> accepted and reference the invoking object.
      *>   SELF-IS-C  "SELF and SUPER both reference the object that was
      *>              used to invoke the method" (§2002:8.4.2.8.3 GR1): C
      *>              invoked SHOWME, so SELF = LK-C (identity).
      *>   SELF-CHILD INVOKE SELF "WHO": resolution uses "the methods
      *>              defined for the runtime class of the object
      *>              referenced by SELF" (§2002:8.4.2.8.3 GR2): G2SASC's
      *>              override returns "CHILD" (X(6): "CHILD "
      *>              with the space fill of the move).
      *>   SUPER-PARENT INVOKE SUPER "WHO": resolution "ignores all the
      *>              methods defined in the class containing the
      *>              invocation" (§2002:8.4.2.8.3 GR3): G2SASP's WHO,
      *>              "PARENT". SUPER as the object of an INVOKE is the
      *>              use §2002:8.4.2.8.2 SR3 permits.
      *> A refusal of SELF/SUPER here (the program-only rule misapplied)
      *> or a wrong resolution fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2SASC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C USAGE OBJECT REFERENCE G2SASC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2SASC "NEW" RETURNING C.
           INVOKE C "SHOWME" USING BY CONTENT C.
           STOP RUN.
       END PROGRAM G2SAS01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2SASP INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-W PIC X(6).
       PROCEDURE DIVISION RETURNING LK-W.
       MAIN-PARA.
           MOVE "PARENT" TO LK-W.
       END METHOD WHO.
       END OBJECT.
       END CLASS G2SASP.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2SASC INHERITS FROM G2SASP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2SASP
           CLASS G2SASC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-W PIC X(6).
       PROCEDURE DIVISION RETURNING LK-W.
       MAIN-PARA.
           MOVE "CHILD" TO LK-W.
       END METHOD WHO.
       METHOD-ID. SHOWME.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  WX PIC X(6).
       LINKAGE SECTION.
       01  LK-C USAGE OBJECT REFERENCE G2SASC.
       PROCEDURE DIVISION USING LK-C.
       MAIN-PARA.
           IF SELF = LK-C
               DISPLAY "SELF-IS-C"
           ELSE
               DISPLAY "SELF-NOT-C"
           END-IF
           INVOKE SELF "WHO" RETURNING WX
           DISPLAY "SELF-" WX
           INVOKE SUPER "WHO" RETURNING WX
           DISPLAY "SUPER-" WX.
       END METHOD SHOWME.
       END OBJECT.
       END CLASS G2SASC.
