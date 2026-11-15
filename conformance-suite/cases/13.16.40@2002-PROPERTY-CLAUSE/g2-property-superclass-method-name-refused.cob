      *>--- conformance-case
      *> id: g2-property-superclass-method-name-refused
      *> rules: [2002-SR-13.16.40.2-4]
      *> source-format: fixed
      *>---
      *> §2002:13.16.40.2 SR4 - "The data-name for the subject of the
      *> entry shall not be the same as a property-name defined in a
      *> superclass."  Superclass G2PRMB defines property-name Q "by
      *> defining a method or a pair of methods with the PROPERTY
      *> phrase" (the rule's NOTE: METHOD-ID. GET PROPERTY Q); the
      *> PROPERTY subject of subclass G2PRMS is also Q, so the program
      *> is refused at the Q entry of G2PRMS.  The accepted sibling is
      *> g2-property-superclass-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRS03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRMS
           PROPERTY Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE G2PRMS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2PRMS "NEW" RETURNING S
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
       END PROGRAM G2PRS03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRMB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-Q PIC 9.
       PROCEDURE DIVISION RETURNING LS-Q.
       Q-PARA.
           MOVE V TO LS-Q
           EXIT METHOD.
       END METHOD.
       END OBJECT.
       END CLASS G2PRMB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRMS INHERITS FROM G2PRMB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRMB.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q PIC 9 VALUE 2 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2PRMS.
