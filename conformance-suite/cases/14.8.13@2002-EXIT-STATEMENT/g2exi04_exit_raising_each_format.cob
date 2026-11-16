      *>--- conformance-case
      *> id: g2exi04_exit_raising_each_format
      *> rules: [2002-SR-14.8.13.2-3, 2002-FMT-14.8.13.1, 2002-GR-14.8.13.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR3 - "If the RAISING phrase is specified, an
      *> exception condition is raised in the activating runtime element
      *> if checking for that exception condition is enabled in the
      *> activating runtime element, and execution continues in that
      *> runtime element as specified in the rules for the activating
      *> statement after the result, if any, of the activated element is
      *> returned". a) "If exception-name-1 is specified, it is that
      *> exception condition." b) "the object referenced by identifier-1
      *> becomes the current exception object in the activating runtime
      *> element". c) 1. "If an exception condition is currently raised,
      *> that exception condition is set to exist in the activating
      *> runtime element" and, for an EC-USER name not in the header,
      *> "the EC-RAISING-NOT-SPECIFIED exception condition is set to
      *> exist in the activating runtime element instead of the EC-USER
      *> exception condition".
      *> §2002:14.8.13.2 SR3 (accepted side): each EXIT ... RAISING
      *> EXCEPTION names a level-3 EC-USER name LISTED in the RAISING
      *> phrase of its own procedure division header.
      *> §2002:14.8.13.1 (page 468 rendered): Formats 2, 3 and 4 each take
      *> RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST
      *> EXCEPTION } with only LAST underlined, so both "RAISING LAST
      *> EXCEPTION" (L, FL) and "RAISING LAST" (U, ML) are written.
      *> Every quotation: cite.py --check OK (GR3, GR3a, GR3b, GR3c1,
      *> SR3). EC-USER-* is non-fatal (§2002:14.5.12.1), so a
      *> declarative that completes normally lets the activating
      *> statement continue normally (§2002:14.5.12.1.3): the CALL,
      *> INVOKE or MOVE completes and the result is stored.
      *> EC-RAISING-NOT-SPECIFIED is Fatal (Table 14), so its declarative
      *> ends with RESUME AT NEXT STATEMENT.
      *> Checking: EC-USER is ON for the whole group except EC-USER-PD,
      *> which is OFF, so it is disabled in the ACTIVATOR.
      *>  DECL-PA AFTER-PA    EXIT PROGRAM RAISING EXCEPTION EC-USER-PA (a)
      *>  AFTER-PD            EC-USER-PD not enabled in the activator: not
      *>                      raised, DECL-PD-WRONG never shown (GR3)
      *>  DECL-OBJ-E AFTER-PB EXIT PROGRAM RAISING LK (b), LK refers to
      *>                      E's object (passed BY CONTENT)
      *>  L-RAISED DECL-PL AFTER-PL   LAST, EC-USER-PL listed (c1)
      *>  U-RAISED DECL-RNS AFTER-PU  LAST, EC-USER-PU not listed (c1):
      *>                      EC-RAISING-NOT-SPECIFIED, never DECL-PU-WRONG
      *>  DECL-MA W=06        EXIT METHOD RAISING EXCEPTION (a); result 6
      *>  ML-RAISED DECL-ML W=07  EXIT METHOD RAISING LAST (c1); result 7
      *>  DECL-FA V=5         EXIT FUNCTION RAISING EXCEPTION (a); result 5
      *>  FL-RAISED DECL-FL V=3   EXIT FUNCTION RAISING LAST EXCEPTION
      *>                      (c1); result 3
      *> (b) for EXIT METHOD and EXIT FUNCTION is the 2002 case
      *> 8.4.2.6@2002-EXCEPTION-OBJECT/g2exo01.
      *> GR3 c) 2. "If no exception condition is raised, the RAISING
      *> phrase is ignored and no exception condition is set to exist in
      *> the activating runtime element" (cite.py --check OK, GR3c2):
      *>  N-DECL-PERFORMED AFTER-PN  G2EXI04N PERFORMs its own declarative,
      *>                      which runs EXIT PROGRAM RAISING LAST
      *>                      EXCEPTION. It is the FIRST statement of the
      *>                      run unit, so nothing has been raised in it
      *>                      and the last exception status is "no
      *>                      exception" (14.5.12.1): no declarative runs.
      *> This leg is the one where every reading of "currently raised"
      *> agrees. LAST after an earlier exception was raised and handled
      *> elsewhere in the run unit is left out: which reading of
      *> "currently raised" governs it is an open adjudication.
       >>TURN EC-USER CHECKING ON
       >>TURN EC-USER-PD CHECKING OFF
       >>TURN EC-RAISING-NOT-SPECIFIED CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXI04F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R RAISING EC-USER-FA.
       F-P.
           MOVE 5 TO R
           EXIT FUNCTION RAISING EXCEPTION EC-USER-FA
           DISPLAY "F-NEVER".
       END FUNCTION G2EXI04F.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXI04G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R RAISING EC-USER-FL.
       DECLARATIVES.
       G-D SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-FL.
       G-D-P.
           DISPLAY "FL-RAISED"
           EXIT FUNCTION RAISING LAST EXCEPTION.
       END DECLARATIVES.
       G-M SECTION.
       G-P.
           MOVE 3 TO R
           RAISE EXCEPTION EC-USER-FL
           DISPLAY "G-NEVER"
           EXIT FUNCTION.
       END FUNCTION G2EXI04G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXI04X
           CLASS G2EXI04K
           FUNCTION G2EXI04F
           FUNCTION G2EXI04G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E USAGE OBJECT REFERENCE G2EXI04X.
       01 K USAGE OBJECT REFERENCE G2EXI04K.
       01 W PIC 9(2) VALUE 0.
       01 V PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-PN SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PN.
       D-PN-P.
           DISPLAY "DECL-PN-WRONG".
       D-PA SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PA.
       D-PA-P.
           DISPLAY "DECL-PA".
       D-PD SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PD.
       D-PD-P.
           DISPLAY "DECL-PD-WRONG".
       D-OB SECTION.
           USE AFTER EXCEPTION OBJECT G2EXI04X.
       D-OB-P.
           IF EXCEPTION-OBJECT = E
               DISPLAY "DECL-OBJ-E"
           ELSE
               DISPLAY "DECL-OBJ-OTHER"
           END-IF.
       D-PL SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PL.
       D-PL-P.
           DISPLAY "DECL-PL".
       D-PU SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PU.
       D-PU-P.
           DISPLAY "DECL-PU-WRONG".
       D-RN SECTION.
           USE AFTER EXCEPTION CONDITION EC-RAISING-NOT-SPECIFIED.
       D-RN-P.
           DISPLAY "DECL-RNS"
           RESUME AT NEXT STATEMENT.
       D-MA SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-MA.
       D-MA-P.
           DISPLAY "DECL-MA".
       D-ML SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-ML.
       D-ML-P.
           DISPLAY "DECL-ML".
       D-FA SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-FA.
       D-FA-P.
           DISPLAY "DECL-FA".
       D-FL SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-FL.
       D-FL-P.
           DISPLAY "DECL-FL".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "G2EXI04N"
           DISPLAY "AFTER-PN"
           CALL "G2EXI04A"
           DISPLAY "AFTER-PA"
           CALL "G2EXI04D"
           DISPLAY "AFTER-PD"
           INVOKE G2EXI04X "NEW" RETURNING E
           CALL "G2EXI04B" USING BY CONTENT E
           DISPLAY "AFTER-PB"
           CALL "G2EXI04L"
           DISPLAY "AFTER-PL"
           CALL "G2EXI04U"
           DISPLAY "AFTER-PU"
           INVOKE G2EXI04K "NEW" RETURNING K
           INVOKE K "MA" RETURNING W
           DISPLAY "W=" W
           INVOKE K "ML" RETURNING W
           DISPLAY "W=" W
           MOVE FUNCTION G2EXI04F TO V
           DISPLAY "V=" V
           MOVE FUNCTION G2EXI04G TO V
           DISPLAY "V=" V
           STOP RUN.
       END PROGRAM G2EXI04.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04N.
       PROCEDURE DIVISION RAISING EC-USER-PN.
       DECLARATIVES.
       N-D SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PN.
       N-D-P.
           DISPLAY "N-DECL-PERFORMED"
           EXIT PROGRAM RAISING LAST EXCEPTION.
       END DECLARATIVES.
       N-M SECTION.
       N-P.
           PERFORM N-D
           DISPLAY "N-NEVER"
           EXIT PROGRAM.
       END PROGRAM G2EXI04N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04A.
       PROCEDURE DIVISION RAISING EC-USER-PA.
       A-P.
           EXIT PROGRAM RAISING EXCEPTION EC-USER-PA
           DISPLAY "A-NEVER".
       END PROGRAM G2EXI04A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04D.
       PROCEDURE DIVISION RAISING EC-USER-PD.
       D-P.
           EXIT PROGRAM RAISING EXCEPTION EC-USER-PD
           DISPLAY "D-NEVER".
       END PROGRAM G2EXI04D.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2EXI04X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE OBJECT REFERENCE G2EXI04X.
       PROCEDURE DIVISION USING LK RAISING G2EXI04X.
       B-P.
           EXIT PROGRAM RAISING LK
           DISPLAY "B-NEVER".
       END PROGRAM G2EXI04B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04L.
       PROCEDURE DIVISION RAISING EC-USER-PL.
       DECLARATIVES.
       L-D SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PL.
       L-D-P.
           DISPLAY "L-RAISED"
           EXIT PROGRAM RAISING LAST EXCEPTION.
       END DECLARATIVES.
       L-M SECTION.
       L-P.
           RAISE EXCEPTION EC-USER-PL
           DISPLAY "L-NEVER"
           EXIT PROGRAM.
       END PROGRAM G2EXI04L.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXI04U.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-D SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-PU.
       U-D-P.
           DISPLAY "U-RAISED"
           EXIT PROGRAM RAISING LAST.
       END DECLARATIVES.
       U-M SECTION.
       U-P.
           RAISE EXCEPTION EC-USER-PU
           DISPLAY "U-NEVER"
           EXIT PROGRAM.
       END PROGRAM G2EXI04U.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXI04X INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2EXI04X.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXI04K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(2).
       PROCEDURE DIVISION RETURNING RES RAISING EC-USER-MA.
       MA-P.
           MOVE 6 TO RES
           EXIT METHOD RAISING EXCEPTION EC-USER-MA
           DISPLAY "MA-NEVER".
       END METHOD MA.
       METHOD-ID. ML.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(2).
       PROCEDURE DIVISION RETURNING RES RAISING EC-USER-ML.
       DECLARATIVES.
       ML-D SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-ML.
       ML-D-P.
           DISPLAY "ML-RAISED"
           EXIT METHOD RAISING LAST.
       END DECLARATIVES.
       ML-M SECTION.
       ML-P.
           MOVE 7 TO RES
           RAISE EXCEPTION EC-USER-ML
           DISPLAY "ML-NEVER"
           EXIT METHOD.
       END METHOD ML.
       END OBJECT.
       END CLASS G2EXI04K.
