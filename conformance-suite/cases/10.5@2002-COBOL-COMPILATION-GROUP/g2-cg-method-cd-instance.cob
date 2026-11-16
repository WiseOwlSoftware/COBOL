      *>--- conformance-case
      *> id: g2-cg-method-cd-instance
      *> rules: [2002-SR-10.5.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:10.5.2 SR4 - "The data division of a method in a class
      *> definition shall not contain a communication section." An
      *> instance (OBJECT) method's data division carries a COMMUNICATION
      *> SECTION with an input CD; the compilation group must be refused at
      *> the COMMUNICATION SECTION header. The same CD in a program
      *> definition is accepted at 2002 (Annex A), so the refusal is the
      *> method placement, not the facility.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CGCI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CGCI.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           MESSAGE COUNT IS MC.
       01 INREC PIC X(87).
       PROCEDURE DIVISION.
           EXIT METHOD.
       END METHOD M1.
       END OBJECT.
       END CLASS G2CGCI.
