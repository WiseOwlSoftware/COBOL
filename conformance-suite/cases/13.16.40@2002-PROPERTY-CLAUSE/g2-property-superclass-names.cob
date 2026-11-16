      *>--- conformance-case
      *> id: g2-property-superclass-names
      *> rules: [2002-SR-13.16.40.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.40.2 SR4 - "The data-name for the subject of the
      *> entry shall not be the same as a property-name defined in a
      *> superclass. NOTE A property-name may be defined in a superclass
      *> either by defining a method or a pair of methods with the
      *> PROPERTY phrase or by describing a data description entry with
      *> the PROPERTY clause."  The accepted form: superclass G2PRSB
      *> defines property-name P by the PROPERTY clause and Q by a GET
      *> PROPERTY method; the PROPERTY subject R of subclass G2PRSS is
      *> neither, so SR4 holds.  §2002:9.3.9: "The subclass has all the
      *> methods defined for the inherited class definition or
      *> definitions", so P and Q are properties of a G2PRSS object too.
      *> All citations: python scripts/spec/cite.py --check -> OK.
      *> Expected: P=1 (the clause's implicit GET of the inherited data,
      *> VALUE 1), Q=2 (the explicit GET method), R=3 (the subclass's own
      *> clause, VALUE 3).  The refused siblings, a subclass subject equal
      *> to each kind of superclass property-name, are
      *> g2-property-superclass-clause-name-refused and
      *> g2-property-superclass-method-name-refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRSS
           PROPERTY P
           PROPERTY Q
           PROPERTY R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE G2PRSS.
       01 W PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2PRSS "NEW" RETURNING S
           MOVE P OF S TO W
           DISPLAY "P=" W
           MOVE Q OF S TO W
           DISPLAY "Q=" W
           MOVE R OF S TO W
           DISPLAY "R=" W
           STOP RUN.
       END PROGRAM G2PRS01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRSB INHERITS FROM BASE.
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
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS-Q PIC 9.
       PROCEDURE DIVISION RETURNING LS-Q.
       Q-PARA.
           MOVE 2 TO LS-Q
           EXIT METHOD.
       END METHOD.
       END OBJECT.
       END CLASS G2PRSB.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRSS INHERITS FROM G2PRSB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRSB.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9 VALUE 3 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2PRSS.
