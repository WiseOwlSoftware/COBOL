      *>--- conformance-case
      *> id: g2opr03_property_accessor_present
      *> rules: [2002-SR-8.4.2.9.2-3, 2002-SR-8.4.2.9.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.9.2 SR3 - "If the object property is used as a
      *> sending item, a get property method shall exist for
      *> property-name-1 in the object referenced by identifier-1 or in
      *> the factory object of the class class-name-1."
      *> §2002:8.4.2.9.2 SR4 - the same for a receiving item and a set
      *> property method. Each requirement applies only to its own use,
      *> so every reference below is legal:
      *>  - FCNT, a factory property with both methods (§2002:13.16.40.3
      *>    GR1 and GR2 define them for PROPERTY without WITH NO), set and
      *>    read through class-name-1: "FCNT=7"
      *>  - FRO, a factory property WITH NO SET, used only as a sender
      *>    (SR3 holds, SR4 does not apply): "FRO=3"
      *>  - FWO, a factory property WITH NO GET, used only as a receiver
      *>    (SR4 holds, SR3 does not apply); no output
      *>  - IVAL, an instance property with both methods, set and read
      *>    through identifier-1: "IVAL=5"
      *>  - IRO, an instance property WITH NO SET, used as a sender:
      *>    "IRO=4"; IWO, WITH NO GET, used as a receiver; no output
      *> The refusals of the missing-method uses are g2opr04 to g2opr07.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPR03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2OPR03C
           PROPERTY FCNT
           PROPERTY FRO
           PROPERTY FWO
           PROPERTY IVAL
           PROPERTY IRO
           PROPERTY IWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O USAGE OBJECT REFERENCE G2OPR03C.
       01  W PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 7 TO FCNT OF G2OPR03C.
           MOVE FCNT OF G2OPR03C TO W.
           DISPLAY "FCNT=" W.
           MOVE FRO OF G2OPR03C TO W.
           DISPLAY "FRO=" W.
           MOVE 9 TO FWO OF G2OPR03C.
           INVOKE G2OPR03C "NEW" RETURNING O.
           MOVE 5 TO IVAL OF O.
           MOVE IVAL OF O TO W.
           DISPLAY "IVAL=" W.
           MOVE IRO OF O TO W.
           DISPLAY "IRO=" W.
           MOVE 8 TO IWO OF O.
           STOP RUN.
       END PROGRAM G2OPR03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2OPR03C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FCNT PIC 9 VALUE 0 PROPERTY.
       01  FRO  PIC 9 VALUE 3 PROPERTY WITH NO SET.
       01  FWO  PIC 9 VALUE 0 PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  IVAL PIC 9 VALUE 0 PROPERTY.
       01  IRO  PIC 9 VALUE 4 PROPERTY WITH NO SET.
       01  IWO  PIC 9 VALUE 0 PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2OPR03C.
