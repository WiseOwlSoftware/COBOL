*>--- conformance-case
*> id: pb5932-exception-file-glued-expression
*> rules: [AR-15.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.28.3 rule 1: "Argument-1 is optional and when specified shall be the name of a file connector
      *> that is specified in an FD statement." The argument A - B is an arithmetic expression, not a name, even though
      *> its tokens glued together spell the file-name A-B. Listing step 6b's first build handed the whole argument to
      *> the name lookup, which keyed it by its glued text and bound the file (kb/Work PB5932 review round 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5932GLUE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT A-B ASSIGN TO "pb5932glue.dat".
       DATA DIVISION.
       FILE SECTION.
       FD A-B.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 1.
       01 S PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION EXCEPTION-FILE (A - B) TO S
           STOP RUN.
