      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_3-p5
      *> rules: [1985-GR-VI.6.3.3-p5, 1985-SR-VI.6.3.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.3 p5; §1985:VI.6.3.3 p1 (CONDITIONAL EXPRESSIONS).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-11.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-11 probe c2-abbrev-examples (PB5453), fixed form, --std 85.
      * The five examples of VI.6.3.3 ("Some examples of abbreviated
      * combined and negated combined relation conditions and
      * expanded equivalents follow"), each run over all 81 tuples of
      * A..D in 1..3 and compared with its printed expansion; the
      * count of disagreements must be 00 on every line.
      * Expected: X1 00  X2 00  X3 00  X4 00  X5 00
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C2ABBRV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9.
       01  B       PIC 9.
       01  C       PIC 9.
       01  D       PIC 9.
       01  T1      PIC 9.
       01  T2      PIC 9.
       01  BADS.
           05  BAD PIC 99 OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO BAD (1) BAD (2) BAD (3) BAD (4) BAD (5).
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            PERFORM VARYING B FROM 1 BY 1 UNTIL B > 3
             PERFORM VARYING C FROM 1 BY 1 UNTIL C > 3
              PERFORM VARYING D FROM 1 BY 1 UNTIL D > 3
               PERFORM CHECK-ALL
              END-PERFORM
             END-PERFORM
            END-PERFORM
           END-PERFORM.
           DISPLAY "X1 " BAD (1) "  X2 " BAD (2) "  X3 " BAD (3)
               "  X4 " BAD (4) "  X5 " BAD (5).
           STOP RUN.
       CHECK-ALL.
           MOVE 0 TO T1 T2.
           IF A > B AND NOT < C OR D MOVE 1 TO T1.
           IF ((A > B) AND (A NOT < C)) OR (A NOT < D) MOVE 1 TO T2.
           IF T1 NOT = T2 ADD 1 TO BAD (1).
           MOVE 0 TO T1 T2.
           IF A NOT EQUAL B OR C MOVE 1 TO T1.
           IF (A NOT EQUAL B) OR (A NOT EQUAL C) MOVE 1 TO T2.
           IF T1 NOT = T2 ADD 1 TO BAD (2).
           MOVE 0 TO T1 T2.
           IF NOT A = B OR C MOVE 1 TO T1.
           IF (NOT (A = B)) OR (A = C) MOVE 1 TO T2.
           IF T1 NOT = T2 ADD 1 TO BAD (3).
           MOVE 0 TO T1 T2.
           IF NOT (A GREATER B OR < C) MOVE 1 TO T1.
           IF NOT ((A GREATER B) OR (A < C)) MOVE 1 TO T2.
           IF T1 NOT = T2 ADD 1 TO BAD (4).
           MOVE 0 TO T1 T2.
           IF NOT (A NOT > B AND C AND NOT D) MOVE 1 TO T1.
           IF NOT ((((A NOT > B) AND (A NOT > C))
               AND (NOT (A NOT > D)))) MOVE 1 TO T2.
           IF T1 NOT = T2 ADD 1 TO BAD (5).
