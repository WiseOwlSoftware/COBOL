*>--- conformance-case
*> id: pb5123-not-is-simple-operator-2014
*> rules: [FMT-8.8.4.12.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5123, review R1-1. NOT before an IS-led simple relational operator in an abbreviated tail. The
      *> 2014 and 2023 general formats (§2014:8.8.4.2.4.1; §2023:8.8.4.12.2) admit NOT there only alone before
      *> object-1, and IS > is a simple relational operator, so NOT IS > C is in no format: COBOL0902, construct
      *> abbreviated-not-is-simple-operator-removed-2014. At 85 it is the logical NOT (§1985:VI.6.3.3 GRL2.2) and at
      *> 2002 the NOT is part of the operator (§2002:8.8.4.2.4.3 GR2b); the 2002 reading runs in
      *> cases/8.8.4.2.4@2002-ABBREVIATED-COMBINED-RELATION-CONDITIONS/pb5123_not_is_simple_operator_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5123N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 9.
       01 C PIC 9 VALUE 6.
       PROCEDURE DIVISION.
       MAIN.
           IF A > B AND NOT IS > C
               DISPLAY "X"
           ELSE
               DISPLAY "Y"
           END-IF
           STOP RUN.
