      *>--- conformance-case
      *> id: pb5715_exit_program_main_global_declarative_is_continue
      *> rules: [2002-GR-14.8.13.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR5 - in a program not under the control of a
      *>   calling runtime element EXIT PROGRAM is treated as CONTINUE and
      *>   "No exception condition is raised", so GR2's EC-FLOW-GLOBAL-EXIT
      *>   is not set here although the EXIT PROGRAM executes within the
      *>   range of a GLOBAL declarative (the contrast of
      *>   pb5715_exit_program_global_declarative_range, a called program).
      *> DERIVATION: IN-DECL 10 (READ at end runs D-GLOB), IN-EXIT-P, then
      *>   the EXIT PROGRAM continues: AFTER-EXIT, BACK-IN-DECL; the run
      *>   goes on: AFTER-READ 10.
       >>TURN EC-FLOW-GLOBAL-EXIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "rmain.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-GLOB SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D-GLOB-P.
           DISPLAY "IN-DECL " FS.
           PERFORM D-EX-P.
           DISPLAY "BACK-IN-DECL".
       D-EX SECTION.
           USE AFTER EXCEPTION CONDITION EC-USER-RMAIN.
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
           STOP RUN.
