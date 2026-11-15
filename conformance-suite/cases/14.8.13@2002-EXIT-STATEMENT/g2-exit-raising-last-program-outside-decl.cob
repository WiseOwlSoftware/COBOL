      *>--- conformance-case
      *> id: g2-exit-raising-last-program-outside-decl
      *> rules: [2002-SR-14.8.13.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR6 - "The LAST phrase may be
      *>   specified only in a declarative procedure." EXIT PROGRAM
      *>   RAISING LAST EXCEPTION in a nondeclarative paragraph shall be
      *>   refused (COBOL2103). The accepted form is
      *>   g2_exit_raising_last_in_declarative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXN61.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           EXIT PROGRAM RAISING LAST EXCEPTION.
       END PROGRAM G2EXN61.
