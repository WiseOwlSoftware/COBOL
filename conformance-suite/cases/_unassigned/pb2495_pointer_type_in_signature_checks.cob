      *>--- conformance-case
      *> id: pb2495_pointer_type_in_signature_checks
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2495 (sibling) - what a restricted program-pointer
      *> MAY CONTAIN is its prototype's signature (13.18.60.4 GR25;
      *> cite.py --check 13.18.60.4 -> OK 13.18.60.4 25)), and the
      *> group-level signature check and the SET-sent passes ask it.
      *> Q1 and Q2 are two prototypes of one signature (USING 9(4)),
      *> so a pointer TO Q1 and a pointer TO Q2 hold the same set of
      *> values (14.9.39.3 SR22, OK 14.9.39.3 22)).
      *> DERIVATION:
      *>   PX's prototype (USING TO Q1) and definition (USING TO Q2) -
      *>     10.6.2 SR2 (OK 10.6.2 2)) "the signatures of these two
      *>     compilation units shall be the same": they are.
      *>   INVOKE O "M1" USING BY CONTENT A (TO Q1) into OVSUB's M1
      *>     formal TO Q2 - 9.3.6 rule 4 c) and 14.8.2.3.3 (OK
      *>     14.8.2.3.3 2)) ask the SET (SR22: one signature): legal.
      *>     BY REFERENCE the pair is refused: 9.3.6 3 e) (OK 9.3.6 3)
      *>     e)) asks "the same ... USAGE clauses", and the TO phrase
      *>     is compared by the prototype it names (kb/Work PB4107).
      *> An OVERRIDE or an IMPLEMENTS pair over the two names is NOT
      *> admitted: 9.3.8.2.3 rule 3 asks the same USAGE clauses too
      *> (kb/Work PB4442; wiseowl/diagnostics/pb4442-*). The pair-mode
      *> and the signature relation were one before PB4442, and this
      *> test used to hold an override and an IMPLEMENTS over Q1 / Q2.
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
       PROGRAM-ID. OVMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           PROGRAM PX
           CLASS OVSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 A TYPE T1.
       01 O USAGE OBJECT REFERENCE OVSUB.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           CALL PX USING A
           INVOKE OVSUB "NEW" RETURNING O
           INVOKE O "M1" USING BY CONTENT A
           STOP RUN.
       END PROGRAM OVMAIN.
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
           IF L-P = NULL DISPLAY "px null" END-IF
           GOBACK.
       END PROGRAM PX.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVSUB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T2.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "content null" END-IF.
       END METHOD M1.
       END OBJECT.
       END CLASS OVSUB.
