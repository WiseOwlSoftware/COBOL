      *> reject-at: 2002 2014 2023
      *> ISO 14.8.2.3.3 rule 2a: a BY CONTENT argument that is an expression is
      *> transferred "according to the rules of the COMPUTE statement", which
      *> requires a category-numeric formal parameter. An arithmetic expression
      *> against an alphanumeric formal has no conforming rule: 14.9.25.3
      *> Table 16 admits a numeric sender to an alphanumeric receiver only for an
      *> INTEGER sender, and an arithmetic expression carries no compile-time
      *> guarantee of integrality.
      *>
      *> This is the CONFORMANCE half of PB46, not a gap: the grammar now admits
      *> arithmetic-expression-1 under BY CONTENT (14.9.23.2), so the pairing has
      *> to be judged rather than refused by the parser for the wrong reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46NEGCONT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB46N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB46N.
       01 N PIC S9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB46N "NEW" RETURNING O.
           INVOKE O "TAKEA" USING BY CONTENT N + 1.
           STOP RUN.
       END PROGRAM PB46NEGCONT.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB46N.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC X(4).
       PROCEDURE DIVISION USING Q.
       M.
           DISPLAY "A=[" Q "]".
       END METHOD TAKEA.
       END OBJECT.
       END CLASS CPB46N.
