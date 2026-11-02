      *>--- conformance-case
      *> id: pb4442_override_pointer_same_prototype_two_type_names
      *> rules: [GR-9.3.8.2.3-3, GR-9.3.8.2.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4442 - 9.3.8.2.3 rule 3 (cite.py --check 9.3.8.2.3
      *> -> OK 9.3.8.2.3 3)): the formal of the method in interface-1
      *> "has the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC
      *> LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses", rule 6
      *> (OK 9.3.8.2.3 6)) the same of the returning item, and 11.7.3
      *> SR9 (OK 11.7.3 9)) makes an override obey them. A restricted
      *> program-pointer's USAGE clause is USAGE PROGRAM-POINTER TO
      *> prototype-name, so the pair conforms exactly when both name
      *> the same prototype. The two type-names (T1 in OVBASE, T3 in
      *> OVSUB) are no clause of rule 3: only a strongly-typed group
      *> is compared by its type (rule 7).
      *> OVSUB's M1 (USING T3, TO Q1) overrides OVBASE's M1 (USING T1,
      *> TO Q1), and its M2 (RETURNING T3, TO Q1) overrides OVBASE's
      *> M2 (RETURNING T1, TO Q1): both name Q1, so both conform and
      *> the subclass's bodies run. The pair written over two
      *> prototypes Q1 / Q2 of ONE signature is refused:
      *> wiseowl/diagnostics/pb4442-override-pointer-two-prototypes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM Q1.
       IDENTIFICATION DIVISION.
       CLASS-ID. OVBASE INHERITS FROM BASE.
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
           DISPLAY "base WRONG".
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR TYPE T1.
       PROCEDURE DIVISION RETURNING LR.
           DISPLAY "base WRONG".
       END METHOD M2.
       END OBJECT.
       END CLASS OVBASE.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVSUB INHERITS FROM OVBASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           CLASS OVBASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T3 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       PROCEDURE DIVISION.
       METHOD-ID. M1 OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T3.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "override formal null" END-IF.
       END METHOD M1.
       METHOD-ID. M2 OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR TYPE T3.
       PROCEDURE DIVISION RETURNING LR.
           SET LR TO NULL
           DISPLAY "override returning".
       END METHOD M2.
       END OBJECT.
       END CLASS OVSUB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4442OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           CLASS OVSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 A TYPE T1.
       01 R TYPE T1.
       01 O USAGE OBJECT REFERENCE OVSUB.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           INVOKE OVSUB "NEW" RETURNING O
           INVOKE O "M1" USING BY CONTENT A
           INVOKE O "M2" RETURNING R
           STOP RUN.
       END PROGRAM PB4442OK.
