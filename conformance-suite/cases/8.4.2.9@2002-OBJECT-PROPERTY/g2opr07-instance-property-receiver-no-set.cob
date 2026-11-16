      *>--- conformance-case
      *> id: g2opr07-instance-property-receiver-no-set
      *> rules: [2002-SR-8.4.2.9.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.9.2 SR4 - "If the object property is used as a
      *> receiving item, a set property method shall exist for
      *> property-name-1 in the object referenced by identifier-1 or in
      *> the factory object of the class class-name-1." This case takes
      *> the identifier-1 (instance object) arm: PV is described
      *> PROPERTY WITH NO SET, so no set property method exists
      *> (§2002:13.16.40.3), and PV OF ... is written as a receiving
      *> item.
      *> The sending use of the same property just before it is legal
      *> (its get method exists) and the class is otherwise valid, so
      *> the only ground for refusal is SR4; the refusal is expected on
      *> the line that uses PV as a receiving item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPR07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2OPR07C
           PROPERTY PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O USAGE OBJECT REFERENCE G2OPR07C.
       01  W PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2OPR07C "NEW" RETURNING O.
           MOVE PV OF O TO W.
           MOVE 7 TO PV OF O.
           DISPLAY "W=" W.
           STOP RUN.
       END PROGRAM G2OPR07.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2OPR07C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PV PIC 9 VALUE 0 PROPERTY WITH NO SET.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2OPR07C.
