      *>--- conformance-case
      *> id: g2opr04-factory-property-sender-no-get
      *> rules: [2002-SR-8.4.2.9.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.9.2 SR3 - "If the object property is used as a
      *> sending item, a get property method shall exist for
      *> property-name-1 in the object referenced by identifier-1 or in
      *> the factory object of the class class-name-1." This case takes
      *> the class-name-1 (factory object) arm: PV is described
      *> PROPERTY WITH NO GET, so no get property method exists
      *> (§2002:13.16.40.3), and PV OF ... is written as a sending item.
      *> The receiving use of the same property just before it is legal
      *> (its set method exists) and the class is otherwise valid, so
      *> the only ground for refusal is SR3; the refusal is expected on
      *> the line that uses PV as a sending item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPR04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2OPR04C
           PROPERTY PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O USAGE OBJECT REFERENCE G2OPR04C.
       01  W PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 7 TO PV OF G2OPR04C.
           MOVE PV OF G2OPR04C TO W.
           DISPLAY "W=" W.
           STOP RUN.
       END PROGRAM G2OPR04.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2OPR04C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PV PIC 9 VALUE 0 PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       END FACTORY.
       END CLASS G2OPR04C.
