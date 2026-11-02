      *>--- conformance-case
      *> id: pb4107-set-group-member-pointers-naming-each-other
      *> rules: [SR-14.9.39.3-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4107 (branch review R1-1) - "the same signature" of
      *> two prototypes whose formals are strongly-typed groups is
      *> decided by 9.3.8.2.3 rule 7's equivalent type declarations,
      *> 8.5.3.1 (cite.py --check 8.5.3.1 -> OK 8.5.3.1): "Two type
      *> declarations are considered equivalent when they have the same
      *> type-name", with corresponding elementary items of the same
      *> USAGE clauses, whose TO phrase is compared by the prototype it
      *> names (docs/CONFORMANCE.md D-RP1).
      *> GS1 takes a strong group TG holding a pointer TO GS2, GS2 one
      *> holding a pointer TO GS1: the two TG differ in that USAGE
      *> clause, so GS1 and GS2 have two signatures, and SET B TO A (B
      *> restricted to GS2, A to GS1) is refused by 14.9.39.3 SR22 (OK
      *> 14.9.39.3 22)) "the program-prototypes associated with
      *> identifier-7 and identifier-8 shall have the same signature"
      *> (COBOL1959). Strong groups naming ONE prototype have one
      *> signature: _unassigned/pb3120_group_formal_pointer_type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GS1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM GS2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO GS2.
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
       PROGRAM-ID. PB4107S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM GS1
           PROGRAM GS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PA IS TYPEDEF USAGE PROGRAM-POINTER TO GS1.
       01 PB IS TYPEDEF USAGE PROGRAM-POINTER TO GS2.
       01 A TYPE PA.
       01 B TYPE PB.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           SET B TO A
           STOP RUN.
       END PROGRAM PB4107S.
