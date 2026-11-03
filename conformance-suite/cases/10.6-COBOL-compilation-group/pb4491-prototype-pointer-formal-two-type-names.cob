      *>--- conformance-case
      *> id: pb4491-prototype-pointer-formal-two-type-names
      *> rules: [SR-10.6.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4491 (branch review R1-1) - a program prototype
      *> whose formal is a pointer TO Q1 of type T1 and its definition
      *> whose formal is a pointer TO Q1 of type T9 do not have the
      *> same signature: 8.13 (cite.py --check 8.13 -> OK) puts the
      *> "type declarations required for the description of
      *> parameters and returning items" in the signature, and two
      *> type declarations are equivalent only "when they have the
      *> same type-name" (8.5.3.1, OK 8.5.3.1). Were they admitted, a
      *> caller checked against the prototype would pass a pointer of
      *> type T1 BY REFERENCE to a formal of type T9, which 14.8.2.3.2
      *> (OK 14.8.2.3.2 4)) refuses: "both shall be restricted and of
      *> the same type" (COBOL1688 for the direct CALL). So 10.6.2 SR2
      *> (OK 10.6.2 2)) "the signatures of these two compilation units
      *> shall be the same" refuses the pair (COBOL1513). The accepted
      *> twin is pb4491_prototype_pointer_formal_one_prototype.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM Q1.
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
           PROGRAM Q1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T9 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 L-P TYPE T9.
       PROCEDURE DIVISION USING L-P.
       PX-P.
           GOBACK.
       END PROGRAM PX.
