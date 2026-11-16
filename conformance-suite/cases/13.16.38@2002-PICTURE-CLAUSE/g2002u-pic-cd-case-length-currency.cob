      *>--- conformance-case
      *> id: g2002u-pic-cd-case-length-currency
      *> rules: [2002-SR-13.16.38.2-2, 2002-SR-13.16.38.2-3, 2002-SR-13.16.38.2-4, 2002-SR-13.16.38.2-25]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every item is in a record under an OUTPUT CD (§2002:A.1.2.2 SR9).
      *> Each picture is a combination the precedence rules allow
      *> (§2002:13.16.38.2 SR2), so the program must compile and edit.
      *> Z1  §2002:13.16.38.2 SR3 - "zz9.99" is "ZZ9.99": 5.5 edits by
      *>     zero suppression to "  5.50" (§2002:13.16.38.4 GR7); were
      *>     'z' not the symbol 'Z' the entry would not compile.
      *> X1  SR3 - "x(3)" is "X(3)": 3 positions, "ABCD" stores "ABC".
      *> C1  SR3 and SR25 - "uuU9" with PICTURE SYMBOL "U" for "EUR":
      *>     'u' is 'U' (also §2002:12.2.6.3 GR13), so the floating string
      *>     is three occurrences of one currency symbol, which SR25
      *>     requires. Size 3 + 1 + 1 + 1 = 6 (§2002:13.16.38.3 GR13d);
      *>     12 edits to " EUR12" (§2002:13.16.38.4 GR6: one currency
      *>     string just left of the first nonzero digit, spaces before).
      *> C2  SR25 - "$$$9", one symbol '$': size 4, 12 edits to " $12".
      *> P50 §2002:13.16.38.2 SR4 - a character-string of exactly 50
      *>     characters is allowed: 50 'X', length 50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PICCL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS "$".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L PIC 9(2).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 Z1 PIC zz9.99.
           05 X1 PIC x(3).
           05 C1 PIC uuU9.
           05 C2 PIC $$$9.
           05 P50
               PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5.5 TO Z1
           MOVE FUNCTION LENGTH(Z1) TO L
           DISPLAY "Z1[" Z1 "] LEN " L
           MOVE "ABCD" TO X1
           MOVE FUNCTION LENGTH(X1) TO L
           DISPLAY "X1[" X1 "] LEN " L
           MOVE 12 TO C1
           MOVE FUNCTION LENGTH(C1) TO L
           DISPLAY "C1[" C1 "] LEN " L
           MOVE 12 TO C2
           MOVE FUNCTION LENGTH(C2) TO L
           DISPLAY "C2[" C2 "] LEN " L
           MOVE FUNCTION LENGTH(P50) TO L
           DISPLAY "P50 LEN " L
           STOP RUN.
