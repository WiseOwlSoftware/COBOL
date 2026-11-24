      *>--- conformance-case
      *> id: g2-exit-function-in-global-declarative
      *> rules: [2002-SR-14.8.13.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR2 - "The EXIT statement shall not
      *>   be specified in a declarative procedure for which the GLOBAL
      *>   phrase is specified in the associated USE statement." The
      *>   rule names the EXIT statement, every format of it: EXIT
      *>   FUNCTION in the GLOBAL declarative of a function shall be
      *>   refused (COBOL2102), as EXIT PROGRAM and GOBACK are. The
      *>   accepted form is g2_exit_function_ends_function.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXN2F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2exn2f.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(10).
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
       DECLARATIVES.
       D1 SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       D1-P.
           EXIT FUNCTION.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE 7 TO R
           EXIT FUNCTION.
       END FUNCTION G2EXN2F.
