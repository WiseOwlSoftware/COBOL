      *>--- conformance-case
      *> id: pb5645_rewrite_from_user_function_refused
      *> rules: [SR-14.9.35.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5645. §2023:14.9.35.3 SR9 - "If identifier-1
      *>   references a function and the FILE phrase is not
      *>   specified, identifier-1 shall reference an alphanumeric,
      *>   boolean, or national function." (cite.py: OK). From
      *>   COBOL-2014 on the rule is on ANY function
      *>   (§2014:14.9.33.2 SR8), so a user-defined function whose
      *>   result is numeric is refused, COBOL1914. At COBOL-2002 the
      *>   same program is accepted (SR9 there names only an
      *>   intrinsic function, §2002:14.8.33.2 SR9; the accepted
      *>   twin is g2rw09_from_user_defined_numeric_function).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB5645N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 9(3).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-F.
           MOVE 42 TO RES
           GOBACK.
       END FUNCTION PB5645N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5645A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB5645N.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5645a.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F
           READ F
           REWRITE R FROM FUNCTION PB5645N
           CLOSE F
           STOP RUN.
       END PROGRAM PB5645A.
