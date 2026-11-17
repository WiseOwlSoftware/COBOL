      *>--- conformance-case
      *> id: pb5861-not-not-relational-operator-2023
      *> rules: [FMT-8.8.4.12.2, GR-8.8.4.12.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5861. The 2014 and 2023 general formats
      *> (§2023:8.8.4.12.2) admit before object-1 one of NOT, a
      *> simple-relational-operator or an extended-relational-operator,
      *> and NOT < is an extended one
      *> (§2023:8.7.5.1, "IS NOT LESS THAN"); the GR1 note says "NOT
      *> cannot immediately precede an extended-relational operator"
      *> (§2023:8.8.4.12.4 GR1). So NOT NOT < C is in no format: the
      *> specific COBOL0902 its sibling NOT IS NOT < C draws, construct
      *> abbreviated-not-logical-removed-2002. At 85 it is the logical
      *> NOT of A NOT < C (§1985:VI.6.3.3 GRL2.2), witnessed by
      *> cases/VI.6.3@1985-CONDITIONAL-EXPRESSIONS/
      *> pb5861_not_not_relation_sweep_85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5861B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 3.
       01  B PIC 9 VALUE 1.
       01  C PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A > B AND NOT NOT < C DISPLAY "X"
           ELSE DISPLAY "Y".
           STOP RUN.
