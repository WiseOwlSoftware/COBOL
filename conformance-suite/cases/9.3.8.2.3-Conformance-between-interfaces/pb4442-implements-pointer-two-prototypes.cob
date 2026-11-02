      *>--- conformance-case
      *> id: pb4442-implements-pointer-two-prototypes
      *> rules: [GR-9.3.8.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4442 - the IMPLEMENTS twin of
      *> pb4442-override-pointer-two-prototypes: 9.3.11 (cite.py
      *> --check 9.3.11 -> OK 9.3.11) "the interface of the instance
      *> objects of the implementing class shall conform to the
      *> interfaces to be implemented", conformance being 9.3.8.2.3
      *> (OK 9.3.8.2.3 3)), whose rule 3 asks the same USAGE clauses.
      *> IVN's M2 takes a
      *> program-pointer TO Q1, the class's M2 one TO Q2; Q1 and Q2
      *> have ONE signature and two names, so the two USAGE clauses
      *> differ and the pair is refused (COBOL0841).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM Q1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM Q2.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. IVN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1.
       PROCEDURE DIVISION.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 LP TYPE T1.
       PROCEDURE DIVISION USING LP.
       END METHOD M2.
       END INTERFACE IVN.

       IDENTIFICATION DIVISION.
       CLASS-ID. IMPLN INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2
           CLASS BASE
           INTERFACE IVN.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IVN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       PROCEDURE DIVISION.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T2.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "implements null" END-IF.
       END METHOD M2.
       END OBJECT.
       END CLASS IMPLN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4442N3.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB4442N3.
