      *>--- conformance-case
      *> id: g2-exit-raising-last-function-outside-decl
      *> rules: [2002-SR-14.8.13.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR6 - "The LAST phrase may be
      *>   specified only in a declarative procedure." EXIT FUNCTION
      *>   RAISING LAST EXCEPTION in a function's nondeclarative
      *>   procedure shall be refused (COBOL2103). The accepted form is
      *>   in g2_exit_raising_last_in_declarative.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXN63F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE "GOOD" TO LR.
           EXIT FUNCTION RAISING LAST EXCEPTION.
       END FUNCTION G2EXN63F.
