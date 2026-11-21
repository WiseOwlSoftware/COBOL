      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_2-p1
      *> rules: [1985-GR-IV.6.4.2.1.2-p1, 1985-SR-IV.6.4.2.3.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.2 p1; §1985:IV.6.4.2.3.2 p1 (Conditional
      *> Statements and Sentences). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-07.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01SENT.
      * 1985 IV.6.4.2: conditional, imperative and delimited scope
      * statements and sentences, self-checking.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9 VALUE 0.
       01 B    PIC 9 VALUE 0.
       01 C    PIC 9 VALUE 0.
       01 N    PIC 9 VALUE 0.
       01 S1   PIC X(4) VALUE "ABCD".
       01 S2   PIC X(2) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
      * Conditional sentence: an imperative statement, then a
      * conditional statement, ended by the separator period.
           MOVE 1 TO A IF A = 1 DISPLAY "C1 THEN" ELSE DISPLAY "C1 X".
      * Imperative sentence: a sequence of imperative statements,
      * separated by a space, a comma and a semicolon.
           MOVE 2 TO A, MOVE 3 TO B; MOVE 4 TO C DISPLAY "I1 " A B C.
      * Delimited scope statement is imperative: it stands in an
      * imperative-statement position (ON SIZE ERROR of an ADD).
           MOVE 9 TO N.
           ADD 1 TO N ON SIZE ERROR
               ADD 5 TO A ON SIZE ERROR DISPLAY "WRONG" END-ADD
               DISPLAY "D1 A=" A
           END-ADD.
      * Footnote verbs without their conditional phrases are
      * imperative: STRING, COMPUTE, DIVIDE in SIZE ERROR.
           ADD 1 TO N ON SIZE ERROR
               STRING S1 DELIMITED BY SIZE INTO S2
               COMPUTE B = 7
               DIVIDE 2 INTO C
               DISPLAY "F1 S2=" S2 " B=" B " C=" C
           NOT ON SIZE ERROR DISPLAY "WRONG".
      * The imperative-statement is ended by a phrase of the
      * containing statement (NOT ON SIZE ERROR) or by a period.
           MOVE 0 TO N.
           ADD 1 TO N ON SIZE ERROR DISPLAY "WRONG" DISPLAY "WRONG"
           NOT ON SIZE ERROR DISPLAY "E1 A" DISPLAY "E1 B".
           DISPLAY "E2 N=" N.
      * Same-verb nesting: each END-ADD ends the most recent ADD.
           MOVE 9 TO N. MOVE 0 TO A.
           ADD 1 TO N ON SIZE ERROR
               ADD 1 TO A ON SIZE ERROR DISPLAY "WRONG"
                   NOT ON SIZE ERROR DISPLAY "N1 INNER"
               END-ADD
               DISPLAY "N1 OUTER-SE"
           NOT ON SIZE ERROR DISPLAY "WRONG"
           END-ADD
           DISPLAY "N1 AFTER A=" A.
           STOP RUN.
