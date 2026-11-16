      *>--- conformance-case
      *> id: pb5793_list_separator_where_space_2023
      *> rules: [SR-8.3.5-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5793 - 8.3.5 SR2: the comma and semicolon followed
      *> by a space "are separators that may be used anywhere the
      *> separator space is used", in the lists COBOL 2002 widened:
      *> an arithmetic-expression subscript, a keyword-omitted
      *> function's argument list (8.4.3.2.3 SR2) and an
      *> exponentiation. I = 1, J = 2, A = 4, EL (2) = 7:
      *> L1 EL (I *, 2)                  EL (2)         = 7
      *> L2 EL (J **, 1 -, 1; )          EL (1)         = 0
      *> L3 MAX (, A J)                 keyword-omitted = 04
      *> L4 MIN (A; , J ; )             MIN (4 2)     = 02
      *> and before a qualifier connective, H = K (3):
      *> L5 MIN (A, K, IN H)             MIN (4 3)     = 03
      *> L6 EL (K, OF H -, 1)             EL (2)         = 7
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5793B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 EL PIC 9 OCCURS 3 TIMES VALUE 0.
       01 H.
          05 K PIC 9 VALUE 3.
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 2.
       01 A PIC 9 VALUE 4.
       01 R PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 7 TO EL (2).
           DISPLAY "L1=" EL (I *, 2).
           DISPLAY "L2=" EL (J **, 1 -, 1; ).
           COMPUTE R = MAX (, A J).
           DISPLAY "L3=" R.
           COMPUTE R = MIN (A; , J ; ).
           DISPLAY "L4=" R.
           COMPUTE R = MIN (A, K, IN H).
           DISPLAY "L5=" R.
           DISPLAY "L6=" EL (K, OF H -, 1).
           STOP RUN.
