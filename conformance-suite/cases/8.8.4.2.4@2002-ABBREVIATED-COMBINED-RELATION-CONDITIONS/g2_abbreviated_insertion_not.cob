      *>--- conformance-case
      *> id: g2_abbreviated_insertion_not
      *> rules: [2002-GR-8.8.4.2.4.3-1, 2002-GR-8.8.4.2.4.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.2.4.3 GR1 (cite.py OK): "the last preceding stated
      *> subject were inserted in place of the omitted subject", the
      *> last stated relational operator in place of an omitted one,
      *> and "The insertion of an omitted subject and/or relational
      *> operator terminates once a complete simple condition is
      *> encountered within a complex condition."
      *> §2002:8.8.4.2.4.3 GR2b (cite.py OK): "then the NOT participates
      *> as part of the simple relational operator"; GR2c (cite.py OK):
      *> "the implied insertion of subject or relational operator
      *> results in a negated relation condition". GR2a (NOT before an
      *> extended operator) has no accepted form: the tail format
      *> (§2002:8.8.4.2.4.1, rendered PDF p158) prints no NOT before
      *> extended-relational-operator, and §2002:F.1 item 22 (cite.py
      *> OK): "The word NOT is prohibited before the relational
      *> operators meaning 'greater than or equal to'".
      *> GR1 legs: L01/L02 subject only omitted, A > B AND < C =
      *> (5>3) AND (5<C): C=4 F, C=6 T. L03/L04 subject and operator
      *> omitted, A = B OR C OR D, A=5 B=3 C=4: D=5 T, D=6 F. L05 a
      *> stated NOT operator carried: A NOT EQUAL B OR C = (5 NOT=5)
      *> OR (5 NOT=7) = T. L06 termination: A > B AND C = D OR B =
      *> (5>3) AND (7=4) OR (7=3) = F (carrying A > would give T).
      *> L07 an extended operator carried: A >= B AND C =
      *> (5>=5) AND (5>=4) = T.
      *> GR2b legs: L08 A > B AND NOT < C OR D, B=9 C=3 D=7 =
      *> ((5>9) AND (5 NOT<3)) OR (5 NOT<7) = F (a logical NOT with <
      *> carried would give T); L09 the same with NOT IS < = F.
      *> GR2c legs: L10 A > B AND NOT C OR D, B=9 C=3 D=4 =
      *> ((5>9) AND NOT (5>3)) OR (5>4) = T (NOT carried as part of
      *> the operator would give F). L11 NOT A = B OR C, B=3 C=5 =
      *> (NOT (5=3)) OR (5=5) = T (NOT over the whole would give F).
      *> NOTE table rows: L12 NOT (A GREATER B OR < C), B=3 C=7 =
      *> NOT ((5>3) OR (5<7)) = F; L13 NOT (A NOT > B AND C AND NOT
      *> D), B=6 C=7 D=4 = NOT (((5 NOT>6) AND (5 NOT>7)) AND
      *> (NOT (5 NOT>4))) = NOT (T AND T AND T) = F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ABR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9.
       01  B PIC 9.
       01  C PIC 9.
       01  D PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO A. MOVE 3 TO B. MOVE 4 TO C.
           IF A > B AND < C DISPLAY "L01 T" ELSE DISPLAY "L01 F".
           MOVE 6 TO C.
           IF A > B AND < C DISPLAY "L02 T" ELSE DISPLAY "L02 F".
           MOVE 3 TO B. MOVE 4 TO C. MOVE 5 TO D.
           IF A = B OR C OR D DISPLAY "L03 T" ELSE DISPLAY "L03 F".
           MOVE 6 TO D.
           IF A = B OR C OR D DISPLAY "L04 T" ELSE DISPLAY "L04 F".
           MOVE 5 TO B. MOVE 7 TO C.
           IF A NOT EQUAL B OR C DISPLAY "L05 T" ELSE DISPLAY "L05 F".
           MOVE 3 TO B. MOVE 7 TO C. MOVE 4 TO D.
           IF A > B AND C = D OR B DISPLAY "L06 T"
           ELSE DISPLAY "L06 F".
           MOVE 5 TO B. MOVE 4 TO C.
           IF A >= B AND C DISPLAY "L07 T" ELSE DISPLAY "L07 F".
           MOVE 9 TO B. MOVE 3 TO C. MOVE 7 TO D.
           IF A > B AND NOT < C OR D DISPLAY "L08 T"
           ELSE DISPLAY "L08 F".
           IF A > B AND NOT IS < C OR D DISPLAY "L09 T"
           ELSE DISPLAY "L09 F".
           MOVE 9 TO B. MOVE 3 TO C. MOVE 4 TO D.
           IF A > B AND NOT C OR D DISPLAY "L10 T"
           ELSE DISPLAY "L10 F".
           MOVE 3 TO B. MOVE 5 TO C.
           IF NOT A = B OR C DISPLAY "L11 T" ELSE DISPLAY "L11 F".
           MOVE 3 TO B. MOVE 7 TO C.
           IF NOT (A GREATER B OR < C) DISPLAY "L12 T"
           ELSE DISPLAY "L12 F".
           MOVE 6 TO B. MOVE 7 TO C. MOVE 4 TO D.
           IF NOT (A NOT > B AND C AND NOT D) DISPLAY "L13 T"
           ELSE DISPLAY "L13 F".
           STOP RUN.
