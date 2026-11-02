      *>--- conformance-case
      *> id: pb3120_group_formal_pointer_type
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120, PB4107 - a strongly-typed GROUP formal that
      *> holds a restricted pointer is compared by 8.5.3.1's type-
      *> declaration equivalence (cite.py --check 8.5.3.1 -> OK
      *> 8.5.3.1): "Two type declarations are considered equivalent
      *> when they have the same type-name", the same EXTERNAL and
      *> STRONG presence, and corresponding elementary items with the
      *> same USAGE clauses - a program-pointer's TO phrase compared by
      *> the prototype it names (docs/CONFORMANCE.md D-RP1), so each
      *> pair below restricts to ONE prototype name.
      *> DERIVATION:
      *>   PX's prototype and PX's definition each hold a strong group
      *>     TG whose pointer is TO QA: 10.6.2 SR2 (OK 10.6.2 2)) "the
      *>     signatures of these two compilation units shall be the
      *>     same" - they are, so the unit compiles.
      *>   CALL PX USING G - 14.8.2.2 rule 2 (OK 14.8.2.2 2)) "If
      *>     either the formal parameter or the corresponding argument
      *>     is a strongly-typed group item, both shall be of the same
      *>     type": G and the definition's TG both hold a pointer TO
      *>     QA: one type, legal; PX sees the NULL address.
      *>   GS1 and GS2 each take a strong group holding a pointer TO
      *>     GS1, so GS1 names itself and the two have one signature;
      *>     SET B TO A with B restricted to GS2 and A to GS1 is legal
      *>     under 14.9.39.3 SR22 (OK 14.9.39.3 22)) "the
      *>     program-prototypes associated with identifier-7 and
      *>     identifier-8 shall have the same signature".
      *>   DNX's prototype and definition each declare T-NODE with a
      *>     member restricted to T-NODE itself (13.18.60.4 GR23): the
      *>     declarations are equivalent, and comparing them terminates.
      *> Two names of one signature in the group's member are refused:
      *> 10.6-COBOL-compilation-group/pb4107-group-formal-pointer-two-
      *> names-one-signature.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM QA.
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
       PROGRAM-ID. GS1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO GS1.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM GS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GS2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM GS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO GS1.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM GS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DNX IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PT IS TYPEDEF USAGE POINTER TO T-NODE.
       01 T-NODE IS TYPEDEF STRONG.
          05 R-A PIC 9(4).
          05 R-NEXT TYPE PT.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM DNX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA
           PROGRAM PX
           PROGRAM GS1
           PROGRAM GS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 TG IS TYPEDEF STRONG.
          05 M-P TYPE TA.
       01 G TYPE TG.
       01 PA IS TYPEDEF USAGE PROGRAM-POINTER TO GS1.
       01 PB IS TYPEDEF USAGE PROGRAM-POINTER TO GS2.
       01 A TYPE PA.
       01 B TYPE PB.
       PROCEDURE DIVISION.
       MAIN-P.
           SET M-P TO NULL
           CALL PX USING G
           SET A TO NULL
           SET B TO A
           IF B = NULL DISPLAY "set ok" END-IF
           STOP RUN.
       END PROGRAM PB3120M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T2.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           IF L-P = NULL DISPLAY "PX NULL" END-IF
           GOBACK.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DNX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PT IS TYPEDEF USAGE POINTER TO T-NODE.
       01 T-NODE IS TYPEDEF STRONG.
          05 R-A PIC 9(4).
          05 R-NEXT TYPE PT.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       DNX-P.
           GOBACK.
       END PROGRAM DNX.
