      *>--- conformance-case
      *> id: pb2495-recursive-prototype-signature-mismatch
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2495, PB4491 - the converse of 2002/pb2495_recursive_
      *> prototype_signature: two prototypes whose formals name EACH
      *> OTHER have two signatures. 8.13 (cite.py --check 8.13 -> OK)
      *> makes the signature "the description of the parameters", and
      *> the description of NS1's formal is USAGE PROGRAM-POINTER TO
      *> NS2, of NS2's USAGE PROGRAM-POINTER TO NS1: two USAGE clauses,
      *> the TO phrase compared by the prototype it names
      *> (docs/CONFORMANCE.md D-RP1), as 14.8.2.3.2 (OK 14.8.2.3.2 4))
      *> compares the pointer a caller passes BY REFERENCE. Were they
      *> one signature, a program of NS1's in a pointer TO NS2 would be
      *> called with a pointer TO NS1 for its formal TO NS2. So
      *> 14.9.39.3 SR22 (OK 14.9.39.3 22)) - "the program-prototypes
      *> associated with identifier-7 and identifier-8 shall have the
      *> same signature" - refuses SET B TO A (COBOL1959).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NS1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO NS2.
       01 L-P TYPE T2.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM NS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NS2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO NS1.
       01 L-Q TYPE T1.
       PROCEDURE DIVISION USING L-Q.
       END PROGRAM NS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2495N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS1
           PROGRAM NS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO NS1.
       01 PT2 IS TYPEDEF USAGE PROGRAM-POINTER TO NS2.
       01 A TYPE PT1.
       01 B TYPE PT2.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           SET B TO A
           STOP RUN.
       END PROGRAM PB2495N.
