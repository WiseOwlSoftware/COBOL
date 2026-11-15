      *>--- conformance-case
      *> id: g2rai01_raise_object_and_name
      *> rules: [2002-GR-14.8.28.3-1, 2002-GR-14.8.28.3-2,
      *>    2002-GR-14.8.28.3-3, 2002-SR-14.8.28.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.28.2 SR2 - "Identifier-1 shall be an object
      *>   reference": OA, OA2, OB are object references of classes
      *>   G2RXA and G2RXB (both INHERITS FROM BASE), so every RAISE
      *>   identifier-1 below is the permitted form and compiles.
      *> §2002:14.8.28.3 GR1 - "If exception-name-1 is specified, the
      *>   associated exception condition is raised, and
      *>   EXCEPTION-OBJECT is set to null."
      *> §2002:14.8.28.3 GR2 - "If identifier-1 is specified,
      *>   EXCEPTION-OBJECT is set to reference the object referenced
      *>   by identifier-1."
      *> §2002:14.8.28.3 GR3 - the RAISE acts as CONTINUE when a)
      *>   "Exception-name-1 is specified, the exception condition is
      *>   non-fatal, and there is no applicable declarative" or b)
      *>   "Identifier-1 is specified and there is no applicable
      *>   declarative".
      *> Context: §2002:14.5.12.1 - a raised condition sets the last
      *>   exception status and nulls EXCEPTION-OBJECT; user-defined
      *>   conditions are non-fatal and checked only under TURN
      *>   (§2002:7.2.19.3 GR4). §2002:14.5.12.1.4 - a raised object
      *>   sets EXCEPTION-OBJECT and a status of EXCEPTION-OBJECT.
      *>   §2002:15.28.2 RV1 - EXCEPTION-STATUS is 31 characters, the
      *>   name or 'EXCEPTION-OBJECT', space-filled. §2002:14.8.45.3
      *>   GR13 a - USE AFTER EXCEPTION OBJECT G2RXA is selected for an
      *>   instance of G2RXA only. §2002:8.8.4.1.1.11 - "=" on object
      *>   references is identity.
      *>   §2002:14.8.8.3 GR1 - CONTINUE "has no effect on the
      *>   execution of the runtime element". Read literally with GR3,
      *>   a RAISE that GR3 makes a CONTINUE may leave EXCEPTION-OBJECT
      *>   and the last exception status unchanged, so this case reads
      *>   neither after such a RAISE (N1, R3) and pins only that
      *>   control reaches the next statement. GR1 and GR2 are witnessed
      *>   only by RAISEs that have an applicable declarative.
      *> DERIVATION of every .out line:
      *>   DECL-OA  RAISE OA: a G2RXA instance, so DA runs and sees OA
      *>            (GR2; 14.8.45.3 GR14).
      *>   R1 ...   after it: EXCEPTION-OBJECT is still OA (GR2), and
      *>            the status is EXCEPTION-OBJECT (14.5.12.1.4 2).
      *>   DECL-H   RAISE EXCEPTION EC-USER-G2H: a USE names it, so GR3
      *>            does not apply - DU runs and the status names it
      *>            (GR1; 14.5.12.1), then execution continues
      *>            (14.5.12.1.3 2).
      *>   N2 ...   EXCEPTION-OBJECT was OA going in; it is null after
      *>            the named raise (GR1).
      *>   N1       RAISE EXCEPTION EC-USER-G2A: TURNed on, non-fatal,
      *>            no USE names it -> CONTINUE (GR3 a): the next
      *>            statement runs.
      *>   DECL-OA2 RAISE OA2: DA sees OA2, not OA (GR2 is identity).
      *>   R2 ...   still OA2 afterwards.
      *>   R3       RAISE OB: G2RXB is not G2RXA, so no declarative
      *>            applies -> CONTINUE (GR3 b): no DECL line, and the
      *>            next statement runs.
      *>   D=2      DA ran exactly twice (OA, OA2); DU once.
      *> Each leg fails if: a RAISE of an object with a declarative
      *>   leaves EO unchanged or null (R1/R2); a named RAISE keeps the
      *>   prior object (N2 EO-NOT-NULL); a no-declarative RAISE
      *>   terminates or skips (N1/R3 missing); the object declarative
      *>   runs for OB (D=3) or a named raise is swallowed with a USE
      *>   (no DECL-H).
       >>TURN EC-USER-G2A EC-USER-G2H CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RAI01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2RXA
           CLASS G2RXB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OA  USAGE OBJECT REFERENCE G2RXA.
       01 OA2 USAGE OBJECT REFERENCE G2RXA.
       01 OB  USAGE OBJECT REFERENCE G2RXB.
       01 D   PIC 9 VALUE 0.
       01 H   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE AFTER EXCEPTION OBJECT G2RXA.
       DA-P.
           ADD 1 TO D
           IF EXCEPTION-OBJECT = OA
               DISPLAY "DECL-OA"
           ELSE
               IF EXCEPTION-OBJECT = OA2
                   DISPLAY "DECL-OA2"
               ELSE
                   DISPLAY "DECL-OTHER"
               END-IF
           END-IF.
       DU SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-G2H.
       DU-P.
           ADD 1 TO H
           DISPLAY "DECL-H [" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE G2RXA "NEW" RETURNING OA
           INVOKE G2RXA "NEW" RETURNING OA2
           INVOKE G2RXB "NEW" RETURNING OB
           RAISE OA
           IF EXCEPTION-OBJECT = OA
               DISPLAY "R1 EO-OA [" FUNCTION EXCEPTION-STATUS "]"
           ELSE
               DISPLAY "R1 EO-NOT-OA"
           END-IF
           RAISE EXCEPTION EC-USER-G2H
           IF EXCEPTION-OBJECT = NULL
               DISPLAY "N2 EO-NULL"
           ELSE
               DISPLAY "N2 EO-NOT-NULL"
           END-IF
           RAISE EXCEPTION EC-USER-G2A
           DISPLAY "N1"
           RAISE OA2
           IF EXCEPTION-OBJECT = OA2
               DISPLAY "R2 EO-OA2"
           ELSE
               DISPLAY "R2 EO-NOT-OA2"
           END-IF
           RAISE OB
           DISPLAY "R3"
           DISPLAY "D=" D " H=" H
           STOP RUN.
       END PROGRAM G2RAI01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2RXA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2RXA.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2RXB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2RXB.
