      *>--- conformance-case
      *> id: pb5676-invoke-parameterized-class
      *> rules: [GR-12.3.8.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5676. INVOKE of a parameterized class the REPOSITORY
      *> lists without EXPANDS: the name may be specified only in the
      *> REPOSITORY paragraph (§2023:12.3.8.4 GR1, §2014:12.3.7.3 GR1;
      *> at 2002 the determination D-PARAMETERIZED-UNEXPANDED).
      *> COBOL0823 states that ground, as COBOL0813 and COBOL0859 do,
      *> and cites the edition's rule.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB5676C INHERITS BASE USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS T.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       END CLASS PB5676C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5676P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB5676C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       M1.
           INVOKE PB5676C "NEW" RETURNING O.
           DISPLAY "OK".
           STOP RUN.
       END PROGRAM PB5676P.
