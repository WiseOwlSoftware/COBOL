      *>--- conformance-case
      *> id: g2-goback-raise-resume-formats
      *> rules: [2002-FMT-14.8.17.1, 2002-FMT-14.8.28.1,
      *>    2002-FMT-14.8.31.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.17.1 GOBACK [RAISING {EXCEPTION exception-name-1 |
      *>   identifier-1 | LAST EXCEPTION}] (rendered p.476: GOBACK,
      *>   RAISING, the first EXCEPTION and LAST underlined; the
      *>   EXCEPTION after LAST is not, so it is optional). Written:
      *>   GOBACK (K=1), RAISING EXCEPTION EC-SIZE-OVERFLOW (K=2),
      *>   RAISING OX (K=3), RAISING LAST EXCEPTION (K=4) and RAISING
      *>   LAST (K=5), the last two in a declarative (§2002:14.8.13.2
      *>   SR6, applied through §2002:14.8.17.3 GR1).
      *> §2002:14.8.28.1 RAISE {EXCEPTION exception-name-1 |
      *>   identifier-1} (rendered p.517): both written, in the
      *>   subprogram and the main program.
      *> §2002:14.8.31.1 RESUME AT {NEXT STATEMENT | procedure-name-1}
      *>   (rendered p.526: AT is not underlined, so optional). Written:
      *>   RESUME AT NEXT STATEMENT, RESUME NEXT STATEMENT, RESUME
      *>   procedure-name and RESUME AT procedure-name.
      *> Semantics pinned by the output (2002 text only):
      *>   §2002:14.8.17.3 GR1 - GOBACK in a called program operates as
      *>   EXIT PROGRAM with its RAISING phrase; §2002:14.8.13.3 GR3 -
      *>   the condition is raised in the activating (calling) element:
      *>   a) exception-name-1, b) identifier-1 becomes the current
      *>   exception object, c)1 LAST: the currently raised condition.
      *>   §2002:14.8.28.3 GR1/GR2 - RAISE EXCEPTION raises the named
      *>   condition; RAISE identifier-1 sets EXCEPTION-OBJECT to it.
      *>   §2002:14.8.31.3 GR2a/b - NEXT STATEMENT continues after the
      *>   raising statement, or after the CALL for a propagated one;
      *>   GR3 - procedure-name-1 transfers as GO TO.
      *>   §2002:15.28.2 RV1 - EXCEPTION-STATUS is 31 characters.
      *> DERIVATION of every .out line:
      *>   BACK 1        K=1 plain GOBACK returns to the CALL.
      *>   DECL EC [..]  K=2 raised in the caller (checking on there),
      *>   BACK 2        E-P resumes after the CALL (GR2b).
      *>   DECL OBJ another / BACK 3   K=3 the sub's object is the
      *>                 exception object; O-P sees it is not main's OX
      *>                 and RESUME NEXT STATEMENT returns after the CALL.
      *>   DECL EC [..] / BACK 4   K=4 RAISE in the sub, its EC-SIZE
      *>                 declarative does GOBACK RAISING LAST EXCEPTION.
      *>   DECL EC [..] / BACK 5   K=5 the same with RAISING LAST.
      *>   DECL EC [..] / AFTER RAISE EC   main RAISE EXCEPTION.
      *>   DECL OBJ main's / AFTER RAISE OBJ   main RAISE OX.
      *>   DECL EC [..] / RESUMED AT PROC 2   K=8: RESUME R-TARGET2.
      *>   DECL EC [..] / RESUMED AT PROC 1   K=9: RESUME AT R-TARGET.
      *> CAN FAIL: a form refused, a RAISING lost or raised in the
      *>   callee (no DECL line), LAST raising nothing, a RESUME that
      *>   lands elsewhere (a NOT REACHED line or a missing BACK).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GRR01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2GRRX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K  PIC 9.
       01 OX USAGE OBJECT REFERENCE G2GRRX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       E-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
       E-P.
           DISPLAY "DECL EC [" FUNCTION EXCEPTION-STATUS "]"
           IF K = 8
               RESUME R-TARGET2
           END-IF
           IF K = 9
               RESUME AT R-TARGET
           END-IF
           RESUME AT NEXT STATEMENT.
       O-SEC SECTION.
           USE AFTER EXCEPTION OBJECT G2GRRX.
       O-P.
           IF EXCEPTION-OBJECT = OX
               DISPLAY "DECL OBJ main's"
           ELSE
               DISPLAY "DECL OBJ another"
           END-IF
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       M-SEC SECTION.
       M-P.
           >>TURN EC-SIZE-OVERFLOW CHECKING ON
           INVOKE G2GRRX "NEW" RETURNING OX
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 5
               CALL "G2GRRS" USING K
               DISPLAY "BACK " K
           END-PERFORM
           RAISE EXCEPTION EC-SIZE-OVERFLOW
           DISPLAY "AFTER RAISE EC"
           RAISE OX
           DISPLAY "AFTER RAISE OBJ"
           MOVE 8 TO K
           RAISE EXCEPTION EC-SIZE-OVERFLOW
           DISPLAY "NOT REACHED 8".
       R-TARGET2.
           DISPLAY "RESUMED AT PROC 2"
           MOVE 9 TO K
           RAISE EXCEPTION EC-SIZE-OVERFLOW
           DISPLAY "NOT REACHED 9".
       R-TARGET.
           DISPLAY "RESUMED AT PROC 1"
           STOP RUN.
       END PROGRAM G2GRR01.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GRRS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2GRRX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OX USAGE OBJECT REFERENCE G2GRRX.
       LINKAGE SECTION.
       01 K  PIC 9.
       PROCEDURE DIVISION USING K RAISING G2GRRX.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       D-P.
           IF K = 5
               GOBACK RAISING LAST
           END-IF
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       S-SEC SECTION.
       S-P.
           >>TURN EC-SIZE-OVERFLOW CHECKING ON
           EVALUATE K
             WHEN 1 GOBACK
             WHEN 2 GOBACK RAISING EXCEPTION EC-SIZE-OVERFLOW
             WHEN 3
                    INVOKE G2GRRX "NEW" RETURNING OX
                    GOBACK RAISING OX
             WHEN OTHER
                    RAISE EXCEPTION EC-SIZE-OVERFLOW
           END-EVALUATE
           GOBACK.
       END PROGRAM G2GRRS.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2GRRX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2GRRX.
