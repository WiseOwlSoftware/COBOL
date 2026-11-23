      *>--- conformance-case
      *> id: pb5715_goback_in_function_global_declarative_range
      *> rules: [2002-GR-14.8.17.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.17.3 GR3 - "If a GOBACK statement is executed in a
      *>   function, the function operates as if executing an EXIT
      *>   FUNCTION statement with the RAISING phrase, if any, that is
      *>   specified in the GOBACK statement."
      *> §2002:14.8.13.3 GR2 - "If an EXIT statement is executed within
      *>   the range of a declarative procedure whose USE statement
      *>   contains the GLOBAL phrase and that USE statement is
      *>   specified in the same function, method, or program as the
      *>   function format, method format, or program format EXIT
      *>   statement respectively, the EC-FLOW-GLOBAL-EXIT exception
      *>   condition is set to exist." So a GOBACK executed in a
      *>   function within the range of that function's GLOBAL
      *>   declarative sets EC-FLOW-GLOBAL-EXIT (§2014:14.9.13.3 GR2 the
      *>   same; §2023 states no rule that sets it).
      *> LEGAL SHAPE: §2002:14.8.17.2 SR1 bars GOBACK in the GLOBAL
      *>   declarative itself and §2002:14.8.45.2 SR3 bars a reference
      *>   from a declarative to a nondeclarative procedure, so the
      *>   GLOBAL declarative D-GLOB PERFORMs the paragraph of another,
      *>   other declarative section D-GB (§2002:14.8.45.2 SR4), and
      *>   the GOBACK there executes within D-GLOB's range. The >>TURN
      *>   directive precedes every statement it must cover
      *>   (§2002:7.2.19.3 GR4: it applies to the statements that follow
      *>   it in the compilation group).
      *> D-GLOB runs for the READ at end of file (no AT END phrase,
      *>   §2002:14.8.45.3 GR6). EC-FLOW-GLOBAL-EXIT is Fatal
      *>   (§2002:14.5.12.1.5); checking is on and D-FLOW applies, so
      *>   (§2002:14.5.12.1.2 item 2) D-FLOW is executed; its STOP RUN
      *>   is not a normal completion (§2002:14.5.12.1.1 item 1).
      *> DERIVATION.
      *>   IN-DECL 10 - the READ at end (status 10) runs D-GLOB.
      *>   IN-GB-P - D-GLOB's PERFORM enters D-GB's paragraph.
      *>   FLOW EC-FLOW-GLOBAL-EXIT - the GOBACK sets the condition:
      *>     one that did not would return the function result, and the
      *>     caller would display R=3.
      *>   No AFTER-GOBACK, BACK-IN-DECL, AFTER-READ or R=: the GOBACK
      *>     is interrupted, D-FLOW runs and its STOP RUN ends the run.
       >>TURN EC-FLOW-GLOBAL-EXIT CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G5715F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g5715f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 WS-EC PIC X(19).
       LINKAGE SECTION.
       01 RES PIC 9.
       PROCEDURE DIVISION RETURNING RES.
       DECLARATIVES.
       D-FLOW SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-GLOBAL-EXIT.
       D-FLOW-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EC
           DISPLAY "FLOW " WS-EC.
           STOP RUN.
       D-GLOB SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D-GLOB-P.
           DISPLAY "IN-DECL " FS.
           PERFORM D-GB-P.
           DISPLAY "BACK-IN-DECL".
       D-GB SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-G5715F.
       D-GB-P.
           DISPLAY "IN-GB-P".
           GOBACK.
           DISPLAY "AFTER-GOBACK".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           MOVE 3 TO RES.
           OPEN OUTPUT F.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "AFTER-READ " FS.
           CLOSE F.
           GOBACK.
       END FUNCTION G5715F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G5715FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G5715F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION G5715F TO W.
           DISPLAY "R=" W.
           STOP RUN.
       END PROGRAM G5715FM.
