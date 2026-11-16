      *>--- conformance-case
      *> id: g2-exit-method-in-program
      *> rules: [2002-SR-14.8.13.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR8 - "An EXIT METHOD statement may
      *>   be specified only in a method procedure division." EXIT
      *>   METHOD in a program procedure division shall be refused
      *>   (COBOL0827). The accepted form is g2_exit_method_ends_method.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXN8.
       PROCEDURE DIVISION.
           DISPLAY "X".
           EXIT METHOD.
