      *>--- conformance-case
      *> id: pb4442-override-returning-pointer-two-prototypes
      *> rules: [GR-9.3.8.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4442 - 9.3.8.2.3 rule 6 (cite.py --check 9.3.8.2.3
      *> -> OK 9.3.8.2.3 6)): "the corresponding returning item has
      *> the same ALIGNED, ... and USAGE clauses", and 11.7.3 SR9 (OK
      *> 11.7.3 9)) makes an override obey it. Q1 and Q2 take one
      *> PIC 9(4): ONE signature, two names. OVSUBR's M1 OVERRIDE
      *> RETURNS a program-pointer TO Q2 where OVBASER's M1 returns
      *> TO Q1: two USAGE clauses, so the override is refused
      *> (COBOL0829), the returning twin of
      *> pb4442-override-pointer-two-prototypes.
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
       CLASS-ID. OVBASER INHERITS FROM BASE.
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
       01 LR TYPE T1.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO NULL.
       END METHOD M1.
       END OBJECT.
       END CLASS OVBASER.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVSUBR INHERITS FROM OVBASER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2
           CLASS OVBASER.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       PROCEDURE DIVISION.
       METHOD-ID. M1 OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR TYPE T2.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO NULL.
       END METHOD M1.
       END OBJECT.
       END CLASS OVSUBR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4442N2.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB4442N2.
