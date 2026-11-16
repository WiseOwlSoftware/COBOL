      *>--- conformance-case
      *> id: g2-exit-raising-last-method-outside-decl
      *> rules: [2002-SR-14.8.13.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> NEGATIVE. §2002:14.8.13.2 SR6 - "The LAST phrase may be
      *>   specified only in a declarative procedure." EXIT METHOD
      *>   RAISING LAST EXCEPTION in a method's nondeclarative procedure
      *>   shall be refused (COBOL2103). The accepted form is in
      *>   g2_exit_method_ends_method.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2EXN62C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
           EXIT METHOD RAISING LAST EXCEPTION.
       END METHOD M1.
       END OBJECT.
       END CLASS G2EXN62C.
