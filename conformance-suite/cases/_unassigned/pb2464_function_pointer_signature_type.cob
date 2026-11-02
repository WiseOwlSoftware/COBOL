*>--- conformance-case
*> id: pb2464_function_pointer_signature_type
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2464 - what a FUNCTION-POINTER may CONTAIN is fixed by
      *> its prototype's SIGNATURE, and the SET relation asks that
      *> signature, not the prototype's name. The identity rules do not:
      *> 14.8.3.3 (OK 14.8.3.3 2)) asks "the same ... USAGE clauses" of two
      *> returning items, and TO F1 and TO F2 are two USAGE clauses
      *> (kb/Work PB4107; the typed INVOKE of the pair is the negative
      *> pb4107-invoke-returning-funcptr-same-signature).
      *> 13.18.60.4 GR26 (cite.py --check 13.18.60.4 -> OK 13.18.60.4 26)):
      *> "A function-pointer shall contain only the predefined address NULL
      *> or the address of a function with the same signature as that
      *> identified by the specified function-prototype-name-1." So a
      *> pointer TO F1 and a pointer TO F2 hold the same set of addresses
      *> when F1 and F2 have the same signature, and 14.9.39.3 SR20 (OK
      *> 14.9.39.3 20)) admits a SET between them: "The function-
      *> prototypes associated with identifier-12 and identifier-13 shall
      *> have the same signature". 14.8.2.3.2 (OK 14.8.2.3.2 4)): "If
      *> either is a restricted pointer, both shall be restricted and of
      *> the same type".
      *> DERIVATION (F1: USING 9(4) RETURNING 9(6); F2: the same with other
      *> data-names, the same signature; F3: USING X(4), another one):
      *>   UNIVERSAL  INVOKE U "RF1" RETURNING FP1 (TO F1)   rule 6: SET
      *>              valid (SR20, the same prototype), 14.8.3.3 holds
      *>              (one USAGE clause): r1 ok
      *>   UNIVERSAL  ... RETURNING FP2 (TO F2, same signature): SR20
      *>              holds, so rule 6 MATCHES (OK 9.3.6 6)) and binds
      *>              RF1; 14.8.3.3 then fails (TO F1 is not TO F2), so
      *>              14.9.23.4 GR7 c) (OK 14.9.23.4 7)) sets
      *>              EC-OO-UNIVERSAL: HANDLED=EC-OO-UNIVERSAL, r2 ok
      *>   UNIVERSAL  ... RETURNING FP3 (TO F3, another signature): SR20
      *>              refuses the SET, no match, "otherwise, the EC-OO-
      *>              METHOD exception condition is set to exist" (OK
      *>              9.3.6 6)): HANDLED=EC-OO-METHOD
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION F1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       01 L-S PIC 9(6).
       PROCEDURE DIVISION USING L-Y RETURNING L-S.
       END FUNCTION F2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F3 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Z PIC X(4).
       01 L-T PIC 9(6).
       PROCEDURE DIVISION USING L-Z RETURNING L-T.
       END FUNCTION F3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2464F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F1
           FUNCTION F2
           FUNCTION F3
           CLASS C2464F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 FP1 USAGE FUNCTION-POINTER TO F1.
       01 FP2 USAGE FUNCTION-POINTER TO F2.
       01 FP3 USAGE FUNCTION-POINTER TO F3.
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
           INVOKE C2464F "NEW" RETURNING U
           INVOKE U "RF1" RETURNING FP1
           DISPLAY "r1 ok"
           INVOKE U "RF1" RETURNING FP2
           DISPLAY "r2 ok"
           INVOKE U "RF1" RETURNING FP3
           DISPLAY "r3 done"
           STOP RUN.
       END PROGRAM PB2464F.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2464F INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RF1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF USAGE FUNCTION-POINTER TO F1.
       PROCEDURE DIVISION RETURNING LF.
           SET LF TO NULL.
       END METHOD RF1.
       END OBJECT.
       END CLASS C2464F.
