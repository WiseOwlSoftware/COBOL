      *>--- conformance-case
      *> id: pb4107-group-formal-pointer-two-names
      *> rules: [SR-10.6.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4107 (branch review R1-1) - a strongly-typed group
      *> formal's program-pointer member is compared by the prototype
      *> its TO phrase NAMES, as the elementary pointer's USAGE clause
      *> is (docs/CONFORMANCE.md D-RP1), never by that prototype's
      *> signature. 8.5.3.1 (cite.py --check 8.5.3.1 -> OK 8.5.3.1):
      *> "Two type declarations are considered equivalent when they
      *> have the same type-name", and each pair of corresponding
      *> elementary items has the same USAGE clauses.
      *> QA and QB both take one PIC 9(4): ONE signature, two names.
      *> PX's prototype holds TG with a pointer TO QA, its definition
      *> TG with one TO QB: two USAGE clauses, so the two TG are not
      *> equivalent and 10.6.2 SR2 (OK 10.6.2 2)) "the signatures of
      *> these two compilation units shall be the same" refuses the
      *> pair (COBOL1513). The same-name pair compiles:
      *> _unassigned/pb3120_group_formal_pointer_type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM QA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QB IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM QB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T1.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4107N.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB4107N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QB.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T2.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           GOBACK.
       END PROGRAM PX.
