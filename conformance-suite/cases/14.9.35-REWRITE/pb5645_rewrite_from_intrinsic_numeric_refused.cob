      *>--- conformance-case
      *> id: pb5645_rewrite_from_intrinsic_numeric_refused
      *> rules: [SR-14.9.35.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5645. §2002:14.8.33.2 SR9 - "If identifier-1
      *>   references an intrinsic function and the FILE phrase is
      *>   not specified, identifier-1 shall reference an
      *>   alphanumeric, boolean, or national function." (cite.py:
      *>   OK). FUNCTION LENGTH returns an integer, a numeric
      *>   function, so REWRITE FROM it is refused, COBOL1914, at
      *>   COBOL-2002 and at every later edition
      *>   (§2023:14.9.35.3 SR9). The user-defined-function half of
      *>   the rule is the twin g2rw09_from_user_defined_numeric_function.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5645B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5645b.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(3).
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN I-O F
           READ F
           REWRITE R FROM FUNCTION LENGTH(W)
           CLOSE F
           STOP RUN.
       END PROGRAM PB5645B.
