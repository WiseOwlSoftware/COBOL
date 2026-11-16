      *>--- conformance-case
      *> id: g2-relop-not-one-operator
      *> rules: [2002-SR-8.7.5.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.7.5.2 SR1 - "When specified, NOT creates one relational
      *> operator that defines the comparison to be executed for truth
      *> value." The simple operators IS [NOT] GREATER THAN / > / LESS
      *> THAN / < / EQUAL TO / = (§2002:8.7.5.1 Format 1) in the general
      *> relation (§2002:8.8.4.1.1.1 Format 1), and IS [NOT] EQUAL TO
      *> and IS [NOT] = in the boolean (Format 2) and pointer (Format 3)
      *> relations.
      *> A = 5, B1 = B"1", B2 = B"0", P1 = ADDRESS OF W, P2 = NULL.
      *> R1-R6: each NOT form is the negated comparison:
      *>   5 NOT > 5 T, 5 NOT > 4 F, 5 NOT < 5 T, 5 NOT < 6 F,
      *>   5 NOT = 4 T, 5 NOT = 5 F.
      *> R7-R9: the one operator is what an abbreviation carries
      *> (§2002:8.8.4.2.4.3 GR1 "the last stated relational operator
      *> were inserted in place of the omitted relational operator";
      *> GR2b "then the NOT participates as part of the simple
      *> relational operator"):
      *>   A NOT > 3 OR 7 = (5 NOT > 3) OR (5 NOT > 7) = F OR T = T
      *>   A NOT < 9 OR 3 = (5 NOT < 9) OR (5 NOT < 3) = F OR T = T
      *>   A NOT = 5 OR 6 = (5 NOT = 5) OR (5 NOT = 6) = F OR T = T
      *> Were NOT a logical operator with the bare operator carried,
      *> each would be F (e.g. NOT (5 > 3) OR (5 > 7)).
      *> R10: a NOT before the subject is not part of the operator, so
      *>   NOT A = 5 OR 6 = (NOT (5 = 5)) OR (5 = 6) = F (GR1, GR2c).
      *> R11 B1 NOT = B2 T; R12 B1 IS NOT EQUAL TO B1 F;
      *> R13 P1 NOT = P2 T; R14 P1 IS NOT EQUAL TO P1 F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RO01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 5.
       01  B1 PIC 1 VALUE B"1".
       01  B2 PIC 1 VALUE B"0".
       01  W  PIC X.
       01  P1 USAGE POINTER.
       01  P2 USAGE POINTER.
       PROCEDURE DIVISION.
           SET P1 TO ADDRESS OF W.
           SET P2 TO NULL.
           IF A NOT GREATER THAN 5 DISPLAY "R1 T" ELSE DISPLAY "R1 F".
           IF A IS NOT > 4 DISPLAY "R2 T" ELSE DISPLAY "R2 F".
           IF A NOT LESS THAN 5 DISPLAY "R3 T" ELSE DISPLAY "R3 F".
           IF A NOT < 6 DISPLAY "R4 T" ELSE DISPLAY "R4 F".
           IF A NOT EQUAL TO 4 DISPLAY "R5 T" ELSE DISPLAY "R5 F".
           IF A IS NOT = 5 DISPLAY "R6 T" ELSE DISPLAY "R6 F".
           IF A NOT > 3 OR 7 DISPLAY "R7 T" ELSE DISPLAY "R7 F".
           IF A NOT < 9 OR 3 DISPLAY "R8 T" ELSE DISPLAY "R8 F".
           IF A NOT = 5 OR 6 DISPLAY "R9 T" ELSE DISPLAY "R9 F".
           IF NOT A = 5 OR 6 DISPLAY "R10 T" ELSE DISPLAY "R10 F".
           IF B1 NOT = B2 DISPLAY "R11 T" ELSE DISPLAY "R11 F".
           IF B1 IS NOT EQUAL TO B1 DISPLAY "R12 T"
           ELSE DISPLAY "R12 F".
           IF P1 NOT = P2 DISPLAY "R13 T" ELSE DISPLAY "R13 F".
           IF P1 IS NOT EQUAL TO P1 DISPLAY "R14 T"
           ELSE DISPLAY "R14 F".
           STOP RUN.
