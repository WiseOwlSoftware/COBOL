      *>--- conformance-case
      *> id: 1985-SR-II_4_2-3
      *> rules: [1985-SR-II.4.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:II.4.2 SR3 (TABLE HANDLING). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the witness-drafting pass (a rule the audit never
      *> drafted) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TBLVAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1.
           02  E1 OCCURS 4 TIMES PIC X(2) VALUE "AB".
       01  T2.
           02  G2 OCCURS 3 TIMES.
               03  N2 PIC 9(2) VALUE 7.
               03  A2 PIC X VALUE "Z".
       01  T3.
           02  G3 OCCURS 2 TIMES.
               03  E3 OCCURS 3 TIMES PIC 9 VALUE 5.
       01  T4.
           02  G4 OCCURS 3 TIMES VALUE "XYW".
               03  F4 PIC X.
               03  H4 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-1.
           DISPLAY T1.
           DISPLAY E1 (1) "-" E1 (4).
           DISPLAY T2.
           DISPLAY N2 (1) "-" N2 (3) "-" A2 (1) "-" A2 (3).
           DISPLAY T3.
           DISPLAY E3 (1, 1) "-" E3 (2, 3).
           DISPLAY T4.
           DISPLAY G4 (1) "-" G4 (3) "-" H4 (3).
           STOP RUN.
