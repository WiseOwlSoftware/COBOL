      *>--- conformance-case
      *> id: pb3120_group_formal_template_pointer_signature
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120, review finding R1-1 (sibling); kb/Work PB4107
      *> - a data-pointer restricted to a strongly-typed record is
      *> compared through that type's TYPEDEF TEMPLATE, whose
      *> program-pointer member is compared by the prototype its TO
      *> phrase names (docs/CONFORMANCE.md D-RP1). 8.5.3.1 (cite.py
      *> --check 8.5.3.1 -> OK 8.5.3.1): "Two type declarations are
      *> considered equivalent when they have the same type-name", and
      *> corresponding elementary items have the same USAGE clauses.
      *> DERIVATION:
      *>   PX's prototype and PX's definition: T-REC holds a
      *>     program-pointer TO QA, TG a pointer restricted TO T-REC.
      *>   10.6.2 SR2 (OK 10.6.2 2)) "the signatures of these two
      *>     compilation units shall be the same": they are, so the unit
      *>     compiles, and CALL PX USING G (14.8.2.2 rule 2, OK
      *>     14.8.2.2 2)) runs PX, which displays PX RAN.
      *> The template's member TO a second prototype of the same
      *> signature is refused: 10.6-COBOL-compilation-group/
      *> pb4107-group-formal-template-pointer-two-names.
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
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T1.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA
           PROGRAM PX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE TA.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 M-P TYPE PT.
       01 G TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL PX USING G
           STOP RUN.
       END PROGRAM PB3120D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T2.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           DISPLAY "PX RAN"
           GOBACK.
       END PROGRAM PX.
