      *>--- conformance-case
      *> id: g85_bf05_04
      *> rules: [1989-RV-A.2.19.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.19.4 RV1 - "the value returned is an integer equal to
      *    the length of argument-1 in character positions", for each
      *    branch:
      * L1 nonnumeric literal "ABCDE": 5.
      * L2 elementary PIC X(7): 7. L3 PIC S9(4)V99 (sign not separate, V
      *    not counted): 6. L4 the same with SIGN LEADING SEPARATE: 7.
      * L5 numeric edited ZZ9.99: 6. L6 a table element PIC X(4): 4.
      * L7 a group without a variable occurrence item, 3 + 4 + 6 = 13.
      * L8 the fixed table group, 3 x 4 = 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0504.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X7              PIC X(7).
       01  S6              PIC S9(4)V99.
       01  SS              PIC S9(4)V99 SIGN LEADING SEPARATE.
       01  ED              PIC ZZ9.99.
       01  T-TBL.
           02  T           PIC X(4) OCCURS 3 TIMES.
       01  G.
           02  GA          PIC X(3).
           02  GB          PIC 9(4).
           02  GC          PIC ZZ9.99.
       01  N3              PIC 999.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE N3 = FUNCTION LENGTH ("ABCDE").
           DISPLAY "L1=" N3.
           COMPUTE N3 = FUNCTION LENGTH (X7).
           DISPLAY "L2=" N3.
           COMPUTE N3 = FUNCTION LENGTH (S6).
           DISPLAY "L3=" N3.
           COMPUTE N3 = FUNCTION LENGTH (SS).
           DISPLAY "L4=" N3.
           COMPUTE N3 = FUNCTION LENGTH (ED).
           DISPLAY "L5=" N3.
           COMPUTE N3 = FUNCTION LENGTH (T (2)).
           DISPLAY "L6=" N3.
           COMPUTE N3 = FUNCTION LENGTH (G).
           DISPLAY "L7=" N3.
           COMPUTE N3 = FUNCTION LENGTH (T-TBL).
           DISPLAY "L8=" N3.
           STOP RUN.
