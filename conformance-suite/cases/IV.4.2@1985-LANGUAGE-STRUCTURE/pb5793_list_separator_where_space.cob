      *>--- conformance-case
      *> id: pb5793_list_separator_where_space
      *> rules: [1985-SR-IV.4.2.1-2, 1985-SR-IV.2.1.6-p1,
      *>    SR-8.3.5-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5793 - §1985:IV.4.2.1 SR2: "the punctuation
      *> characters comma and semicolon, immediately followed by a
      *> space, are separators that may be used anywhere the
      *> separator space is used". A space may follow '(' (SR9) and
      *> precede ')' (SR8), and surrounds an arithmetic operator, so
      *> each comma or semicolon below stands for a space, inside a
      *> subscript list, a reference modifier and an argument list.
      *> W-T = "XYZ", I = 1, J = 2, A = 4, E (2) = 7:
      *> S1 W-E (I +, 1) is W-E (I + 1)            = Y
      *> S2 W-E (, J)                              = Y
      *> S3 W-E (J, )                              = Y
      *> S4 W-E (I; )                              = X
      *> F1 FUNCTION MAX (, A I)        MAX (4 1)   = 04
      *> F2 FUNCTION MAX (A, )          MAX (4)     = 04
      *> F3 FUNCTION MAX (A +, 1; I)    MAX (5 1)   = 05
      *> F4 FUNCTION MAX (A, , J)       MAX (4 2)   = 04
      *> F5 FUNCTION MAX (A, -1)        MAX (4 -1)  = 04
      *> F6 FUNCTION MAX (A, (J))       MAX (4 2)   = 04
      *> R1 W-T (2:, 2)                             = YZ
      *> R2 W-T (, 1 :2)                            = XY
      *> Q1 E OF, G (J)                             = 7
      *> and before or after a qualifier connective inside a list,
      *> H = K (3):
      *> Q2 W-E (K OF, H)                           = Z
      *> Q3 W-E (K, OF H)                           = Z
      *> Q4 W-E (K; IN H)                           = Z
      *> Q5 FUNCTION MIN (A, K, IN H)   MIN (4 3)   = 03
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5793A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T VALUE "XYZ".
          05 W-E OCCURS 3 TIMES PIC X.
       01 G.
          05 E PIC 9 OCCURS 3 TIMES.
       01 H.
          05 K PIC 9 VALUE 3.
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 2.
       01 A PIC 9 VALUE 4.
       01 R PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE 7 TO E (2).
           DISPLAY "S1=" W-E (I +, 1).
           DISPLAY "S2=" W-E (, J).
           DISPLAY "S3=" W-E (J, ).
           DISPLAY "S4=" W-E (I; ).
           COMPUTE R = FUNCTION MAX (, A I).
           DISPLAY "F1=" R.
           COMPUTE R = FUNCTION MAX (A, ).
           DISPLAY "F2=" R.
           COMPUTE R = FUNCTION MAX (A +, 1; I).
           DISPLAY "F3=" R.
           COMPUTE R = FUNCTION MAX (A, , J).
           DISPLAY "F4=" R.
           COMPUTE R = FUNCTION MAX (A, -1).
           DISPLAY "F5=" R.
           COMPUTE R = FUNCTION MAX (A, (J)).
           DISPLAY "F6=" R.
           DISPLAY "R1=" W-T (2:, 2).
           DISPLAY "R2=" W-T (, 1 :2).
           DISPLAY "Q1=" E OF, G (J).
           DISPLAY "Q2=" W-E (K OF, H).
           DISPLAY "Q3=" W-E (K, OF H).
           DISPLAY "Q4=" W-E (K; IN H).
           COMPUTE R = FUNCTION MIN (A, K, IN H).
           DISPLAY "Q5=" R.
           STOP RUN.
