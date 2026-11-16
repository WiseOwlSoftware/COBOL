      *>--- conformance-case
      *> id: g2_combined_and_or
      *> rules: [2002-FMT-8.8.4.2.2.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.2.2.1 - condition-1 {AND | OR} condition-2 ...
      *> (the format has no quotable four-word line; rendered PDF p157
      *> checked). §2002:8.8.4.2.2 (cite.py OK): "A combined condition
      *> results from connecting conditions with one of the logical
      *> operators 'AND' or 'OR'". Truth values from the table of
      *> §2002:8.8.4.2 (cite.py OK: "Logical inclusive OR; the truth
      *> value is true if one or both of the included conditions is
      *> true"; AND "true if both of the conjoined conditions are
      *> true"), and precedence §2002:8.8.4.2.3 (cite.py OK: "The
      *> order of precedence of logical operators is 'NOT', 'AND'").
      *> A=1 B=2 X-YES true N=-3.
      *> K01-K04 the AND truth table T F F F; K05-K08 the OR table
      *> T T T F; K09/K10 a four-condition AND chain (the ellipsis),
      *> mixing relation, condition-name and sign conditions: T, F;
      *> K11/K12 a four-condition OR chain: T, F; K13 T OR (F AND T)
      *> = T; K14 T OR (F AND F) = T (left to right it would be F);
      *> K15 (T OR F) AND F = F; K16 (T AND F) OR (T AND T) = T.
      *> A leg fails if a connective is mis-evaluated, a repeated
      *> connective is refused, or AND does not bind before OR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMB01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       01  X PIC X VALUE "Y".
           88  X-YES VALUE "Y".
       01  N PIC S9 VALUE -3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = 1 AND B = 2 DISPLAY "K01 T" ELSE DISPLAY "K01 F".
           IF A = 1 AND B = 3 DISPLAY "K02 T" ELSE DISPLAY "K02 F".
           IF A = 2 AND B = 2 DISPLAY "K03 T" ELSE DISPLAY "K03 F".
           IF A = 2 AND B = 3 DISPLAY "K04 T" ELSE DISPLAY "K04 F".
           IF A = 1 OR B = 2 DISPLAY "K05 T" ELSE DISPLAY "K05 F".
           IF A = 1 OR B = 3 DISPLAY "K06 T" ELSE DISPLAY "K06 F".
           IF A = 2 OR B = 2 DISPLAY "K07 T" ELSE DISPLAY "K07 F".
           IF A = 2 OR B = 3 DISPLAY "K08 T" ELSE DISPLAY "K08 F".
           IF A = 1 AND B = 2 AND X-YES AND N IS NEGATIVE
              DISPLAY "K09 T" ELSE DISPLAY "K09 F".
           IF A = 1 AND B = 2 AND X-YES AND N IS POSITIVE
              DISPLAY "K10 T" ELSE DISPLAY "K10 F".
           IF A = 9 OR B = 9 OR NOT X-YES OR N IS NEGATIVE
              DISPLAY "K11 T" ELSE DISPLAY "K11 F".
           IF A = 9 OR B = 9 OR NOT X-YES OR N IS ZERO
              DISPLAY "K12 T" ELSE DISPLAY "K12 F".
           IF A = 1 OR B = 9 AND X-YES DISPLAY "K13 T"
           ELSE DISPLAY "K13 F".
           IF A = 1 OR B = 9 AND N IS ZERO DISPLAY "K14 T"
           ELSE DISPLAY "K14 F".
           IF (A = 1 OR B = 9) AND N IS ZERO DISPLAY "K15 T"
           ELSE DISPLAY "K15 F".
           IF (A = 1 AND B = 9) OR (X-YES AND N IS NEGATIVE)
              DISPLAY "K16 T" ELSE DISPLAY "K16 F".
           STOP RUN.
