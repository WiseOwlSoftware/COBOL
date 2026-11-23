      *>--- conformance-case
      *> id: pb5715_exit_program_global_declarative_range
      *> rules: [2002-GR-14.8.13.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR2 - "If an EXIT statement is executed within
      *>   the range of a declarative procedure whose USE statement
      *>   contains the GLOBAL phrase and that USE statement is
      *>   specified in the same function, method, or program as the
      *>   function format, method format, or program format EXIT
      *>   statement respectively, the EC-FLOW-GLOBAL-EXIT exception
      *>   condition is set to exist." (§2014:14.9.13.3 GR2 the same.)
      *>   §2023 states no rule that sets it (docs/conformance/3/
      *>   D-GLOBAL-EXIT.md). The rule row is a 2002 one, so the test
      *>   is accepted at 2002; the program ends the same way at 2014.
      *> LEGAL SHAPE: §2002:14.8.13.2 SR2 bars EXIT PROGRAM in a GLOBAL
      *>   declarative itself and §2002:14.8.45.2 SR3 bars a reference
      *>   from a declarative to a nondeclarative procedure, so the
      *>   GLOBAL declarative D-GLOB PERFORMs the paragraph of another,
      *>   other declarative section D-EX (§2002:14.8.45.2 SR4), and
      *>   the EXIT PROGRAM there executes within D-GLOB's range. The
      *>   callee G5715B is a called program, so the EXIT PROGRAM is the
      *>   returning one (§2002:14.8.13.3 GR6).
      *> D-GLOB runs for the READ at end of file (no AT END phrase,
      *>   §2002:14.8.45.3 GR6). EC-FLOW-GLOBAL-EXIT is Fatal
      *>   (§2002:14.5.12.1.5); checking is on and D-FLOW applies, so
      *>   (§2002:14.5.12.1.2 item 2) D-FLOW is executed; its STOP RUN
      *>   is not a normal completion (§2002:14.5.12.1.1 item 1).
      *> DERIVATION.
      *>   IN-DECL 10 - the READ at end (status 10) runs D-GLOB.
      *>   IN-EXIT-P - D-GLOB's PERFORM enters D-EX's paragraph.
      *>   FLOW EC-FLOW-GLOBAL-EXIT - the EXIT PROGRAM sets the
      *>     condition: one that did not would return to the caller,
      *>     which would display SUB-BACK.
      *>   No AFTER-EXIT, BACK-IN-DECL, AFTER-READ or SUB-BACK: the
      *>     EXIT PROGRAM is interrupted, D-FLOW runs and its STOP RUN
      *>     ends the run.
       >>TURN EC-FLOW-GLOBAL-EXIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G5715A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "G5715B".
           DISPLAY "SUB-BACK".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G5715B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g5715b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 WS-EC PIC X(19).
       PROCEDURE DIVISION.
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
           PERFORM D-EX-P.
           DISPLAY "BACK-IN-DECL".
       D-EX SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-G5715.
       D-EX-P.
           DISPLAY "IN-EXIT-P".
           EXIT PROGRAM.
           DISPLAY "AFTER-EXIT".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "AFTER-READ " FS.
           CLOSE F.
           EXIT PROGRAM.
       END PROGRAM G5715B.
       END PROGRAM G5715A.
