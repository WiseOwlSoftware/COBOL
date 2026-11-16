      *>--- conformance-case
      *> id: g2-property-superclass-clause-name-refused
      *> rules: [2002-SR-13.16.40.2-4]
      *> source-format: fixed
      *>---
      *> §2002:13.16.40.2 SR4 - "The data-name for the subject of the
      *> entry shall not be the same as a property-name defined in a
      *> superclass."  Superclass G2PRCB defines property-name P "by
      *> describing a data description entry with the PROPERTY clause"
      *> (the rule's NOTE); the PROPERTY subject of subclass G2PRCS is
      *> also P, so the program is refused at the P entry of G2PRCS.
      *> The accepted sibling is g2-property-superclass-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRS02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRCS
           PROPERTY P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE G2PRCS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2PRCS "NEW" RETURNING S
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
       END PROGRAM G2PRS02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRCB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC 9 VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2PRCB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRCS INHERITS FROM G2PRCB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRCB.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC 9 VALUE 2 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2PRCS.
