      *>--- conformance-case
      *> id: g2_goback_global_declarative_range
      *> rules: [2002-GR-14.8.17.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.17.3 GR5 - "If a GOBACK statement is executed within
      *>   the range of a declarative procedure whose USE statement
      *>   contains the GLOBAL phrase and that USE statement is specified
      *>   in the same program as the GOBACK statement, the
      *>   EC-FLOW-GLOBAL-GOBACK exception condition is set to exist."
      *> LEGAL SHAPE: §2002:14.8.17.2 SR1 bars GOBACK in the GLOBAL
      *>   declarative itself and §2002:14.8.45.2 SR3 bars a reference
      *>   from a declarative to a nondeclarative procedure, so the
      *>   GLOBAL declarative D-GLOB PERFORMs a paragraph of another,
      *>   non-GLOBAL declarative section (§2002:14.8.45.2 SR4), and the
      *>   GOBACK there executes within D-GLOB's range.
      *> D-GLOB runs for the READ at end of file (no AT END phrase,
      *>   §2002:14.8.45.3 GR6). EC-FLOW-GLOBAL-GOBACK is Fatal
      *>   (§2002:14.5.12.1.5); checking is on and D-FLOW applies, so
      *>   (§2002:14.5.12.1.2 item 2) D-FLOW is executed. D-FLOW is a
      *>   different USE procedure from D-GLOB, so §2002:14.8.45.3 GR2
      *>   (EC-FLOW-USE) does not apply. A RESUME there would be a
      *>   CONTINUE (§2002:14.8.31.3 GR1: within the scope of a global
      *>   declarative) and the declarative would complete normally,
      *>   terminating the run unit abnormally; D-FLOW executes STOP RUN
      *>   instead, which is not a normal completion (§2002:14.5.12.1.1
      *>   item 1), so the run ends normally after FLOW.
      *> DERIVATION.
      *>   IN-DECL 10 - the READ at end (status 10) runs D-GLOB.
      *>   IN-GB-P - the PERFORMed paragraph is entered.
      *>   FLOW EC-FLOW-GLOBAL-GOBACK - the GOBACK sets the condition:
      *>     a GOBACK that did not raise would end the run here instead.
      *>   No AFTER-GOBACK, BACK-IN-DECL or AFTER-READ: the GOBACK is
      *>     interrupted, D-FLOW runs, and its STOP RUN ends the run.
       >>TURN EC-FLOW-GLOBAL-GOBACK CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GBK02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2gbk02.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 WS-EC PIC X(21).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FLOW SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-GLOBAL-GOBACK.
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
           USE AFTER EXCEPTION CONDITION EC-USER-G2GB.
       D-GB-P.
           DISPLAY "IN-GB-P".
           GOBACK.
           DISPLAY "AFTER-GOBACK".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "AFTER-READ " FS.
           CLOSE F.
           STOP RUN.
       END PROGRAM G2GBK02.
