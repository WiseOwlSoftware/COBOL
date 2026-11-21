      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_2-T1_1
      *> rules: [1985-GR-VI.6.3.2-T1.1, 1985-GR-VI.6.3.2-T1.2, 1985-GR-VI.6.3.2-p1, 1985-GR-VI.6.3.2.1-p1, 1985-GR-VI.6.3.2.3-p1, 1985-GR-VI.6.3.2.3-p2, 1985-SR-VI.6.3.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.2 Table 1 row 1; §1985:VI.6.3.2 Table 1 row 2;
      *> §1985:VI.6.3.2 p1; §1985:VI.6.3.2.1 p1; §1985:VI.6.3.2.3 p1;
      *> §1985:VI.6.3.2.3 p2; §1985:VI.6.3.2.2 p1 (CONDITIONAL
      *> EXPRESSIONS). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-11.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-11 probe c1-logic (PB5453), fixed form, run at --std 85.
      * VI.6.3.2 truth values of AND, OR, NOT; VI.6.3.2.1 negation
      * and a parenthesized negated condition; VI.6.3.2.2 combined
      * conditions of several AND/OR terms; VI.6.3.2.3 precedence
      * NOT, AND, OR and parentheses departing from it; Table 1
      * permitted element pairs (OR NOT, AND (, NOT (, ( NOT, ( (,
      * ) ), ) AND, ) OR, simple first/last, ( first, ) last).
      * Expected (derived by hand from the 1985 text):
      *   AND FFFT  OR FTTT  NOT TF  PNOT TF  NOTP TF
      *   PREC FTFFTTTT  PAREN FTFFFTFT  EQV 00
      *   COMB TFFT  NEST T
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C1LOGIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X       PIC 9.
       01  Y       PIC 9.
       01  Z       PIC 9.
       01  N       PIC 99.
       01  S-AND   PIC X(4).
       01  S-OR    PIC X(4).
       01  S-NOT   PIC X(2).
       01  S-PNOT  PIC X(2).
       01  S-NOTP  PIC X(2).
       01  S-PREC  PIC X(8).
       01  S-PAR   PIC X(8).
       01  S-COMB  PIC X(4).
       01  S-NEST  PIC X.
       01  BAD     PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING X FROM 0 BY 1 UNTIL X > 1
             PERFORM VARYING Y FROM 0 BY 1 UNTIL Y > 1
               COMPUTE N = X * 2 + Y + 1
               IF X = 1 AND Y = 1
                   MOVE "T" TO S-AND (N:1)
               ELSE
                   MOVE "F" TO S-AND (N:1)
               END-IF
               IF X = 1 OR Y = 1
                   MOVE "T" TO S-OR (N:1)
               ELSE
                   MOVE "F" TO S-OR (N:1)
               END-IF
             END-PERFORM
           END-PERFORM.
           PERFORM VARYING X FROM 0 BY 1 UNTIL X > 1
               COMPUTE N = X + 1
               IF NOT X = 1
                   MOVE "T" TO S-NOT (N:1)
               ELSE
                   MOVE "F" TO S-NOT (N:1)
               END-IF
               IF (NOT X = 1)
                   MOVE "T" TO S-PNOT (N:1)
               ELSE
                   MOVE "F" TO S-PNOT (N:1)
               END-IF
               IF NOT (X = 1)
                   MOVE "T" TO S-NOTP (N:1)
               ELSE
                   MOVE "F" TO S-NOTP (N:1)
               END-IF
           END-PERFORM.
           PERFORM VARYING X FROM 0 BY 1 UNTIL X > 1
            PERFORM VARYING Y FROM 0 BY 1 UNTIL Y > 1
             PERFORM VARYING Z FROM 0 BY 1 UNTIL Z > 1
               COMPUTE N = X * 4 + Y * 2 + Z + 1
               IF X = 1 OR NOT Y = 1 AND Z = 1
                   MOVE "T" TO S-PREC (N:1)
                   IF NOT (X = 1 OR ((NOT Y = 1) AND Z = 1))
                       ADD 1 TO BAD
                   END-IF
               ELSE
                   MOVE "F" TO S-PREC (N:1)
                   IF X = 1 OR ((NOT Y = 1) AND Z = 1)
                       ADD 1 TO BAD
                   END-IF
               END-IF
               IF (X = 1 OR (NOT Y = 1)) AND Z = 1
                   MOVE "T" TO S-PAR (N:1)
               ELSE
                   MOVE "F" TO S-PAR (N:1)
               END-IF
             END-PERFORM
            END-PERFORM
           END-PERFORM.
      * combined: c1 AND c2 OR c3 AND c4 = (c1 AND c2) OR (c3 AND c4)
           MOVE 1 TO X Y. MOVE 0 TO Z.
           IF X = 1 AND Y = 1 OR Z = 1 AND X = 0
               MOVE "T" TO S-COMB (1:1)
           ELSE
               MOVE "F" TO S-COMB (1:1).
           IF X = 0 AND Y = 1 OR Z = 1 AND X = 1
               MOVE "T" TO S-COMB (2:1)
           ELSE
               MOVE "F" TO S-COMB (2:1).
           IF X = 1 AND Y = 0 AND Z = 0
               MOVE "T" TO S-COMB (3:1)
           ELSE
               MOVE "F" TO S-COMB (3:1).
           IF X = 0 OR Y = 0 OR Z = 0
               MOVE "T" TO S-COMB (4:1)
           ELSE
               MOVE "F" TO S-COMB (4:1).
      * Table 1 pairs: NOT ( , ( ( , ( NOT , OR NOT , ) ) , ) AND
           IF NOT ( ( X = 0 OR NOT Y = 1 ) AND ( Z = 1 ) )
               MOVE "T" TO S-NEST
           ELSE
               MOVE "F" TO S-NEST.
           DISPLAY "AND " S-AND "  OR " S-OR "  NOT " S-NOT
               "  PNOT " S-PNOT "  NOTP " S-NOTP.
           DISPLAY "PREC " S-PREC "  PAREN " S-PAR "  EQV " BAD.
           DISPLAY "COMB " S-COMB "  NEST " S-NEST.
           STOP RUN.
