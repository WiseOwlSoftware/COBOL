      *>--- conformance-case
      *> id: pb4491-prototype-pointer-formal-two-names
      *> rules: [SR-10.6.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4491 - a program prototype whose formal is a pointer
      *> TO Q1 and its definition whose formal is a pointer TO Q2 do
      *> not have the same signature, though Q1 and Q2 do (USING one
      *> PIC 9(4) each). 8.13 (cite.py --check 8.13 -> OK): the
      *> signature is "the description of the parameters"; 13.7.3 SR2
      *> (OK 13.7.3 2)): the prototype's formals "shall match the
      *> description of the formal parameters" of the definition; a
      *> pointer formal's description is its USAGE clause, and USAGE
      *> PROGRAM-POINTER TO Q1 and TO Q2 are two USAGE clauses, the TO
      *> phrase compared by the prototype it names (docs/CONFORMANCE.md
      *> D-RP1), as the same pair as strong-group members is
      *> (pb4107-group-formal-pointer-two-names). Were they admitted,
      *> a caller checked against the prototype would pass a pointer
      *> TO Q1 BY REFERENCE to a formal TO Q2, which 14.8.2.3.2 (OK
      *> 14.8.2.3.2 4)) refuses: "both shall be restricted and of the
      *> same type". So 10.6.2 SR2 (OK 10.6.2 2)) "the signatures of
      *> these two compilation units shall be the same" refuses the
      *> pair (COBOL1513).
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
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 L-P TYPE T1.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       01 L-P TYPE T2.
       PROCEDURE DIVISION USING L-P.
       PX-P.
           GOBACK.
       END PROGRAM PX.
