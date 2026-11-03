      *>--- conformance-case
      *> id: pb2495_recursive_prototype_signature
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2495, PB4491 - "the same signature" over prototypes
      *> whose formals are restricted program-pointers. 8.13 (cite.py
      *> --check 8.13 -> OK) makes the signature "the description of
      *> the parameters", and a pointer formal's description is its
      *> USAGE clause, whose TO phrase is compared by the prototype it
      *> names (docs/CONFORMANCE.md D-RP1). PS3 (USING pointer TO PS3)
      *> names itself; PS1 and PS2 (each USING pointer TO PS3) have
      *> PS3's very description, so the three are ONE signature. DX1
      *> (USING pointer TO DX2) names another prototype: another.
      *> DERIVATION:
      *>   SET B TO A, C TO B, A TO C - 14.9.39.3 SR22 (OK 14.9.39.3
      *>     22)) "the program-prototypes associated with identifier-7
      *>     and identifier-8 shall have the same signature": legal.
      *>   PSDEF's prototype (USING TO PS3 through type T3) and
      *>     definition (USING TO PS3 through type T9) - 10.6.2 SR2 (OK
      *>     10.6.2 2)) "the signatures of these two compilation units
      *>     shall be the same": they are, one USAGE clause.
      *>   CALL PSDEF USING BY CONTENT A - the prototype's formal is
      *>     TO PS3, A is TO PS1; 14.8.2.3.3 (OK 14.8.2.3.3 2)) "the
      *>     conformance rules shall be the same as if a SET
      *>     statement were performed": SR22, one signature, legal;
      *>     PSDEF sees NULL. BY REFERENCE it is refused: 14.8.2.3.2
      *>     (OK 14.8.2.3.2 4)) "both shall be restricted and of the
      *>     same type" compares the TO phrase by the prototype it
      *>     names (kb/Work PB4107).
      *>   UNIVERSAL INVOKE U "RP" RETURNING B - RP returns TO PS1 into
      *>     TO PS2: SR22 holds, so 9.3.6 rule 6 (OK 9.3.6 6)) matches
      *>     and binds RP; its returning item then conforms by 14.8.3.3
      *>     (OK 14.8.3.3 2)) "the same ... USAGE clauses", which TO
      *>     PS1 and TO PS2 are not, so 14.9.23.4 GR7 c) (OK 14.9.23.4
      *>     7)) sets EC-OO-UNIVERSAL (kb/Work PB4107).
      *>   UNIVERSAL ... RETURNING D (TO DX1) - SR22 refuses the SET, no
      *>     match, "otherwise, the EC-OO-METHOD exception condition is
      *>     set to exist" (OK 9.3.6 6)): HANDLED=EC-OO-METHOD.
      *> Two prototypes that name EACH OTHER are two signatures: the
      *> refusal pb2495-recursive-prototype-signature-mismatch.
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PS1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T3 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 L-P TYPE T3.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM PS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PS2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T3 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 L-Q TYPE T3.
       PROCEDURE DIVISION USING L-Q.
       END PROGRAM PS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PS3 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T3 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 L-R TYPE T3.
       PROCEDURE DIVISION USING L-R.
       END PROGRAM PS3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DX1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM DX2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TE IS TYPEDEF USAGE PROGRAM-POINTER TO DX2.
       01 L-D TYPE TE.
       PROCEDURE DIVISION USING L-D.
       END PROGRAM DX1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DX2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM DX3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TF IS TYPEDEF USAGE PROGRAM-POINTER TO DX3.
       01 L-E TYPE TF.
       PROCEDURE DIVISION USING L-E.
       END PROGRAM DX2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DX3 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-F PIC 9(4).
       PROCEDURE DIVISION USING L-F.
       END PROGRAM DX3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PSDEF IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T3 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 L-S TYPE T3.
       PROCEDURE DIVISION USING L-S.
       END PROGRAM PSDEF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2495M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS1
           PROGRAM PS2
           PROGRAM PS3
           PROGRAM DX1
           PROGRAM PSDEF
           CLASS C2495.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO PS1.
       01 PT2 IS TYPEDEF USAGE PROGRAM-POINTER TO PS2.
       01 PT3 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 PTX IS TYPEDEF USAGE PROGRAM-POINTER TO DX1.
       01 A TYPE PT1.
       01 B TYPE PT2.
       01 C TYPE PT3.
       01 D TYPE PTX.
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
           SET A TO NULL
           SET B TO A
           SET C TO B
           SET A TO C
           IF A = NULL AND B = NULL AND C = NULL
               DISPLAY "set ok"
           END-IF
           CALL PSDEF USING BY CONTENT A
           INVOKE C2495 "NEW" RETURNING U
           INVOKE U "RP" RETURNING B
           DISPLAY "b ok"
           INVOKE U "RP" RETURNING D
           DISPLAY "d done"
           STOP RUN.
       END PROGRAM PB2495M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PSDEF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T9 IS TYPEDEF USAGE PROGRAM-POINTER TO PS3.
       01 L-S TYPE T9.
       PROCEDURE DIVISION USING L-S.
       PSDEF-P.
           IF L-S = NULL DISPLAY "psdef null"
           ELSE DISPLAY "psdef WRONG" END-IF
           GOBACK.
       END PROGRAM PSDEF.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2495 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO PS1.
       PROCEDURE DIVISION.
       METHOD-ID. RP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT1.
       PROCEDURE DIVISION RETURNING LP.
           SET LP TO NULL.
       END METHOD RP.
       END OBJECT.
       END CLASS C2495.
