      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_1-p1
      *> rules: [1985-GR-VI.6.3.1.1.1-p1, 1985-GR-VI.6.3.1.1.1-p2, 1985-SR-VI.6.3.1.1-p1, 1985-SR-VI.6.3.1.1-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.1 p1; §1985:VI.6.3.1.1.1 p2; §1985:VI.6.3.1.1
      *> p1; §1985:VI.6.3.1.1 p4 (Relation Condition). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10REL.
      * 1985 VI.6.3.1.1: operand kinds, every relational operator of
      * Table 1, numeric comparison by algebraic value across usages,
      * group operand compared by the nonnumeric rules.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D3       PIC 9(3)        VALUE 5.
       01  B4       PIC S9(4) COMP  VALUE 5.
       01  P5       PIC S9(5)V99 COMP VALUE 5.
       01  NZ       PIC S9(3)       VALUE ZERO.
       01  NEGZ     PIC S9(3) SIGN LEADING SEPARATE VALUE ZERO.
       01  U1       PIC 9           VALUE 3.
       01  S1       PIC S9          VALUE -3.
       01  G.
           05  G1   PIC X(2)        VALUE "12".
       01  NUM2     PIC 99          VALUE 12.
       01  TB.
           05  TE   PIC 9 OCCURS 5 TIMES INDEXED BY IX IY.
       PROCEDURE DIVISION.
       P0.
           SET IX TO 2. SET IY TO 4.
           IF D3 = 5 DISPLAY "O1 T" ELSE DISPLAY "O1 F".
           IF 5 = D3 DISPLAY "O2 T" ELSE DISPLAY "O2 F".
           IF D3 + 1 = B4 * 2 - 4 DISPLAY "O3 T" ELSE DISPLAY "O3 F".
           IF IX < IY DISPLAY "O4 T" ELSE DISPLAY "O4 F".
           IF IX = 2 DISPLAY "O5 T" ELSE DISPLAY "O5 F".
           IF D3 IS GREATER THAN 4 DISPLAY "T1 T" ELSE DISPLAY "T1 F".
           IF D3 IS NOT GREATER THAN 4 DISPLAY "T2 T"
               ELSE DISPLAY "T2 F".
           IF D3 IS > 5 DISPLAY "T3 T" ELSE DISPLAY "T3 F".
           IF D3 IS NOT > 5 DISPLAY "T4 T" ELSE DISPLAY "T4 F".
           IF D3 IS LESS THAN 6 DISPLAY "T5 T" ELSE DISPLAY "T5 F".
           IF D3 IS NOT LESS THAN 6 DISPLAY "T6 T" ELSE DISPLAY "T6 F".
           IF D3 IS < 5 DISPLAY "T7 T" ELSE DISPLAY "T7 F".
           IF D3 IS NOT < 5 DISPLAY "T8 T" ELSE DISPLAY "T8 F".
           IF D3 IS EQUAL TO 5 DISPLAY "T9 T" ELSE DISPLAY "T9 F".
           IF D3 IS NOT EQUAL TO 5 DISPLAY "TA T" ELSE DISPLAY "TA F".
           IF D3 IS = 4 DISPLAY "TB T" ELSE DISPLAY "TB F".
           IF D3 IS NOT = 4 DISPLAY "TC T" ELSE DISPLAY "TC F".
           IF D3 IS GREATER THAN OR EQUAL TO 5 DISPLAY "TD T"
               ELSE DISPLAY "TD F".
           IF D3 IS >= 6 DISPLAY "TE T" ELSE DISPLAY "TE F".
           IF D3 IS LESS THAN OR EQUAL TO 5 DISPLAY "TF T"
               ELSE DISPLAY "TF F".
           IF D3 IS <= 4 DISPLAY "TG T" ELSE DISPLAY "TG F".
           IF D3 GREATER 4 DISPLAY "TH T" ELSE DISPLAY "TH F".
           IF D3 = B4 DISPLAY "N1 T" ELSE DISPLAY "N1 F".
           IF P5 = B4 DISPLAY "N2 T" ELSE DISPLAY "N2 F".
           IF D3 = 5.000 DISPLAY "N3 T" ELSE DISPLAY "N3 F".
           IF D3 = 0000005 DISPLAY "N4 T" ELSE DISPLAY "N4 F".
           MOVE -0 TO NEGZ.
           IF NZ = NEGZ DISPLAY "N5 T" ELSE DISPLAY "N5 F".
           IF NZ = -0 DISPLAY "N6 T" ELSE DISPLAY "N6 F".
           IF U1 > S1 DISPLAY "N7 T" ELSE DISPLAY "N7 F".
           IF U1 = 3 DISPLAY "N8 T" ELSE DISPLAY "N8 F".
           IF G = NUM2 DISPLAY "G1 T" ELSE DISPLAY "G1 F".
           IF G = "12 " DISPLAY "G2 T" ELSE DISPLAY "G2 F".
           STOP RUN.
