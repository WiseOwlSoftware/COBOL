      *>--- conformance-case
      *> id: pb4442-override-pointer-two-prototypes
      *> rules: [GR-9.3.8.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4442 - 9.3.8.2.3 rule 3 (cite.py --check 9.3.8.2.3
      *> -> OK 9.3.8.2.3 3)) asks the corresponding formal "the same
      *> ... USAGE clauses", and 11.7.3 SR9 (OK 11.7.3 9)) makes an
      *> override obey it. Q1 and Q2 both take one PIC 9(4): ONE
      *> signature, two names. OVSUB's M1 OVERRIDE formal is USAGE
      *> PROGRAM-POINTER TO Q2 over OVBASE's TO Q1: two USAGE clauses,
      *> so the override is refused (COBOL0829), as the same pair as
      *> strongly-typed group members is (rule 7, 8.5.3.1; the sibling
      *> pb4107-group-formal-pointer-two-names). The same-name pair
      *> conforms: the 9.3.8.2.3 case
      *> pb4442_override_pointer_same_prototype_two_type_names.
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
       CLASS-ID. OVBASEN INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T1.
       PROCEDURE DIVISION USING LP.
           DISPLAY "base".
       END METHOD M1.
       END OBJECT.
       END CLASS OVBASEN.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVSUBN INHERITS FROM OVBASEN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2
           CLASS OVBASEN.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       PROCEDURE DIVISION.
       METHOD-ID. M1 OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T2.
       PROCEDURE DIVISION USING LP.
           DISPLAY "override".
       END METHOD M1.
       END OBJECT.
       END CLASS OVSUBN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4442N1.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB4442N1.
