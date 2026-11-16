      *>--- conformance-case
      *> id: pb5784_not_not_abbreviated_relation_85
      *> rules: [1985-GR-VI.6.3.3-L2.1, 1985-GR-VI.6.3.3-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5784 - §1985:VI.6.3.3 GRL2.1: "If the word immediately
      *> following NOT is GREATER, >, LESS, <, EQUAL, =, then the NOT
      *> participates as part of the relational operator; otherwise,"
      *> GRL2.2: "The NOT is interpreted as a logical operator". In
      *> A > B OR NOT NOT < C the first NOT is followed by NOT, so it is
      *> the logical operator; the second is followed by <, so it is part
      *> of the relational operator: A > B OR NOT (A NOT < C), the same
      *> condition as A > B OR NOT IS NOT < C. A = 1, B = 5, C = 3:
      *>   T1  A > B false; A NOT < C (1 >= 3) false; NOT of it true: T1.
      *>   T2  the IS-spelled control: T2.
      *>   T3  A = 1, Z = 0: A NOT < Z true, so NOT of it false and
      *>       A > B false: F3.
      *> The defect refused the first form (COBOL0001 near NOT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5784NN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 5.
       01 C PIC 9 VALUE 3.
       01 Z PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           IF A > B OR NOT NOT < C
               DISPLAY "T1"
           ELSE
               DISPLAY "F1"
           END-IF.
           IF A > B OR NOT IS NOT < C
               DISPLAY "T2"
           ELSE
               DISPLAY "F2"
           END-IF.
           IF A > B OR NOT NOT < Z
               DISPLAY "T3"
           ELSE
               DISPLAY "F3"
           END-IF.
           STOP RUN.
