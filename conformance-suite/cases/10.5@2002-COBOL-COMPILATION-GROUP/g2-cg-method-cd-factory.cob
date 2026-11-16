      *>--- conformance-case
      *> id: g2-cg-method-cd-factory
      *> rules: [2002-SR-10.5.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:10.5.2 SR4 - "The data division of a method in a class
      *> definition shall not contain a communication section." A factory
      *> method's data division carries a COMMUNICATION SECTION with an
      *> input CD; the compilation group must be refused at the
      *> COMMUNICATION SECTION header (the instance-method leg is
      *> g2-cg-method-cd-instance).
       IDENTIFICATION DIVISION.
       CLASS-ID. G2CGCF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2CGCF.
       IDENTIFICATION DIVISION.
       FACTORY.
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
       END FACTORY.
       END CLASS G2CGCF.
