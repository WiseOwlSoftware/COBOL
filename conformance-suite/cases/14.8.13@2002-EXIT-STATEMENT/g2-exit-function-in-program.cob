      *>--- conformance-case
      *> id: g2-exit-function-in-program
      *> rules: [2002-SR-14.8.13.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR9 - "An EXIT FUNCTION statement may
      *>   be specified only in a function procedure division." EXIT
      *>   FUNCTION in a program procedure division shall be refused
      *>   (COBOL0827). The accepted form is
      *>   g2_exit_function_ends_function.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXN9.
       PROCEDURE DIVISION.
           DISPLAY "X".
           EXIT FUNCTION.
