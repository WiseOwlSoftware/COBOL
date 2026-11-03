*>--- conformance-case
*> id: pb4107_restricted_pointer_identity_and_signature
*> rules: [GR-14.8.2.3.2-4, GR-14.8.2.3.3-2, SR-14.9.39.3-22]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4107 - two relations over restricted program-pointers, each where the standard asks it.
      *> 14.8.2.3.2: "If either is a restricted pointer, both shall be restricted and of the same type."
      *>   cite.py: OK  14.8.2.3.2 4)
      *> 8.5.3.1: "Two typed items are of the same type when:" "The items are described with TYPE clauses that reference
      *>   equivalent type declarations"   cite.py: OK  8.5.3.1 - equivalence needs "the same type-name" and "the same ...
      *>   USAGE clauses", and the USAGE clause's TO phrase names a prototype.
      *> 13.18.60.3 SR19: "If program-prototype-name-1 is specified, the TYPEDEF clause shall be specified for the subject
      *>   of the entry."   cite.py: OK  13.18.60.3 19) - so every restricted program-pointer is a typed item.
      *> 14.8.2.3.3: "the conformance rules shall be the same as if a SET statement were performed"
      *>   cite.py: OK  14.8.2.3.3 2)
      *> 14.9.39.3 SR22: "the program-prototypes associated with identifier-7 and identifier-8 shall have the same
      *>   signature"   cite.py: OK  14.9.39.3 22)
      *> 9.3.6 3 e): "the corresponding parameter has the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH,
      *>   JUSTIFIED, PICTURE, SIGN, and USAGE clauses"   cite.py: OK  9.3.6 3) e)
      *> 14.9.4.4 GR3 d): "If a violation of these rules is detected, the EC-PROGRAM-ARG-MISMATCH exception condition is
      *>   set to exist"   cite.py: OK  14.9.4.4 3) d)
      *> 14.9.23.4 GR7 c): "the EC-OO-UNIVERSAL exception condition is set to exist"   cite.py: OK  14.9.23.4 7)
      *> P4107A and P4107B are two prototypes of one signature (USING 9(4)). TA restricts to P4107A; TB and TC restrict
      *> alike to P4107B under two type-names. S4107 (defined after the caller, its details the program prototype
      *> definition ahead of every unit, so 12.3.8.4 GR10 b) applies - cite.py: OK  12.3.8.4 10) b)) and the method M take
      *> a formal of type TB. The CALL of S4107 is checked against the prototype at compile time (and, since the program
      *> is the prototype's, at the activation too), so the BY REFERENCE CALLs of PA (TO P4107A is not TO P4107B) and
      *> of PC (type TC is not type TB) are compile-time refusals, witnessed by pb4107-call-byref-pointer-two-types
      *> (kb/Work PB2103: S4107 used to FOLLOW the caller with no prototype, which 12.3.8.3 SR14 does not admit).
      *> DERIVATION:
      *>   SET PB TO PA, SET PC TO PB - SR22: one signature, legal: "set ok".
      *>   CALL BY REFERENCE PB - the same type (TB) and USAGE clause (TO P4107B): "in sub", "ref same ok".
      *>   CALL BY CONTENT PA - 14.8.2.3.3 sends it to SR22, one signature: "in sub", "content ok".
      *>   INVOKE U "M" USING PB - 9.3.6 3 e) matches and 14.8.2.3.2 conforms: "in M".
      *>   INVOKE U "M" USING PC - 3 e) matches (one USAGE clause), so M is bound, and 14.8.2.3.2 then refuses the two
      *>     types under GR7 c): HANDLED=EC-OO-UNIVERSAL, M does not run.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4107A IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM P4107A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4107B IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM P4107B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S4107 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107B.
       01 F TYPE TB.
       PROCEDURE DIVISION USING F.
       END PROGRAM S4107.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T4107.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107A
           PROGRAM P4107B
           PROGRAM S4107
           CLASS C4107.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA TYPEDEF USAGE PROGRAM-POINTER TO P4107A.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107B.
       01 TC TYPEDEF USAGE PROGRAM-POINTER TO P4107B.
       01 PA TYPE TA.
       01 PB TYPE TB.
       01 PC TYPE TC.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET PA TO NULL
           SET PB TO PA
           SET PC TO PB
           DISPLAY "set ok"
           CALL S4107 USING BY REFERENCE PB
               ON EXCEPTION DISPLAY "ref same exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "ref same ok"
           END-CALL
           CALL S4107 USING BY CONTENT PA
               ON EXCEPTION DISPLAY "content exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "content ok"
           END-CALL
           INVOKE C4107 "NEW" RETURNING U
           INVOKE U "M" USING PB
           INVOKE U "M" USING PC
           DISPLAY "done"
           GOBACK.
       END PROGRAM T4107.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S4107.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107B.
       LINKAGE SECTION.
       01 F TYPE TB.
       PROCEDURE DIVISION USING F.
           DISPLAY "in sub"
           GOBACK.
       END PROGRAM S4107.

       IDENTIFICATION DIVISION.
       CLASS-ID. C4107 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107B
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO P4107B.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE TB.
       PROCEDURE DIVISION USING BY REFERENCE LP.
           DISPLAY "in M".
       END METHOD M.
       END OBJECT.
       END CLASS C4107.
