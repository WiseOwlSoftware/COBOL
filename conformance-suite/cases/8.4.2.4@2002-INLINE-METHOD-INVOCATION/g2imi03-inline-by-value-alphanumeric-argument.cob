      *>--- conformance-case
      *> id: g2imi03-inline-by-value-alphanumeric-argument
      *> rules: [2002-SR-8.4.2.4.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.4.2 SR3 - "One of the INVOKE statements specified in
      *> general rule 1 shall be valid according to 14.8.22, INVOKE
      *> statement, syntax rules." O :: "DBL" (X) stands for INVOKE O
      *> "DBL" USING X RETURNING temp; DBL's formal is BY VALUE, and
      *> §2002:14.8.22.2 SR15 requires "identifier-5 shall be of class
      *> numeric, object or pointer" - X is alphanumeric, so no valid
      *> INVOKE exists and the inline invocation is refused. Everything
      *> else is legal (the same method takes the numeric N in g2imi02),
      *> so the only ground for refusal is SR3 through SR15. The refusal
      *> is expected on the COMPUTE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IMI03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2IMI03C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O USAGE OBJECT REFERENCE G2IMI03C.
       01  X PIC X(4) VALUE "ABCD".
       01  N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2IMI03C "NEW" RETURNING O.
           COMPUTE N = O :: "DBL" (X).
           DISPLAY "N=" N.
           STOP RUN.
       END PROGRAM G2IMI03.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2IMI03C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. DBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LN PIC 9(4).
       01  LR PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LN RETURNING LR.
           COMPUTE LR = LN * 2.
       END METHOD DBL.
       END OBJECT.
       END CLASS G2IMI03C.
