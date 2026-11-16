      *>--- conformance-case
      *> id: g2-concat-operand-forms
      *> rules: [2002-FMT-8.8.3.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.1 - "{ literal-1 | concatenation-expression-1 }
      *> & literal-2": the left operand is a literal or a concatenation
      *> expression, the right a literal. §2002:13.9.2 SR2 -
      *> "Constant-name-1 may be used anywhere that a format specifies a
      *> literal of the class and category of constant-name-1", so a
      *> constant-name is a literal-1 or literal-2 operand, with the
      *> effect "as if literal-1 ... were written where constant-name-1
      *> is written" (§2002:13.9.3 GR1). The value is "the
      *> concatenation of the value of the literals" (§2002:8.8.3.3
      *> GR2) and the expression is "equivalent to a literal of the
      *> same class and value" (§2002:8.8.3.3 GR3).
      *> K = "KK", KB = B"10", KN = N"Q".
      *> C1 VALUE "AB" & "CD" & "EF" (chained, PIC X(8)): [ABCDEF  ].
      *> C2 "X" & K & "Y" (constant as literal-2, then the expression
      *>    as concatenation-expression-1): XKKY.
      *> C3 K & "Z" (constant as literal-1): KKZ.
      *> C4 B"01" & B"1" & KB (boolean, chained, constant last): 01110.
      *> C5 KN & N"AB" & N"C" (national, constant first) = N"QABC": T.
      *> C6 the expression as a relation operand: "AB" & K = "ABKK": T.
      *> A refused operand fails the compile; an operand dropped or
      *> reordered changes the printed value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K  CONSTANT AS "KK".
       01  KB CONSTANT AS B"10".
       01  KN CONSTANT AS N"Q".
       01  W  PIC X(8) VALUE "AB" & "CD" & "EF".
       01  WB PIC 1(5).
       01  WN PIC N(4).
       01  W4 PIC X(4) VALUE "ABKK".
       PROCEDURE DIVISION.
           DISPLAY "C1 [" W "]".
           DISPLAY "C2 " "X" & K & "Y".
           DISPLAY "C3 " K & "Z".
           MOVE B"01" & B"1" & KB TO WB.
           DISPLAY "C4 " WB.
           MOVE KN & N"AB" & N"C" TO WN.
           IF WN = N"QABC" DISPLAY "C5 T" ELSE DISPLAY "C5 F".
           IF "AB" & K = W4 DISPLAY "C6 T" ELSE DISPLAY "C6 F".
           STOP RUN.
