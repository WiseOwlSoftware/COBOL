      *>--- conformance-case
      *> id: pb4107-group-formal-template-pointer-two-names
      *> rules: [SR-10.6.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4107 (branch review R1-1) - a data-pointer
      *> restricted to a strongly-typed record is compared through that
      *> type's TYPEDEF TEMPLATE, whose program-pointer member is
      *> compared by the prototype its TO phrase NAMES
      *> (docs/CONFORMANCE.md D-RP1). 8.5.3.1 (cite.py --check 8.5.3.1
      *> -> OK 8.5.3.1): "Two type declarations are considered
      *> equivalent when they have the same type-name", and each pair
      *> of corresponding elementary items has the same USAGE clauses.
      *> QA and QB both take one PIC 9(4): ONE signature, two names.
      *> PX's prototype: T-REC holds a program-pointer TO QA; its
      *> definition: TO QB. The two T-REC are not equivalent, so
      *> neither are the TG whose pointer each restricts to its T-REC,
      *> and 10.6.2 SR2 (OK 10.6.2 2)) "the signatures of these two
      *> compilation units shall be the same" refuses the pair
      *> (COBOL1513). The same-name pair compiles and runs:
      *> _unassigned/pb3120_group_formal_template_pointer_signature.
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
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T1.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4107T.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB4107T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QB.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T2.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           GOBACK.
       END PROGRAM PX.
