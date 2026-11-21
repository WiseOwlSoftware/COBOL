      *>--- conformance-case
      *> id: 1985-GR-VI_1_1-p1
      *> rules: [1985-GR-VI.1.1-p1, 1985-GR-VI.1.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.1 p1; §1985:VI.1.2 p1 (INTRODUCTION TO THE NUCLEUS
      *> MODULE). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-VI.1.1-p1 / GR-VI.1.2-p1: the Nucleus level 1 verbs,
      * a three-dimensional fixed table and the debugging switch with
      * a debugging line. Expect: 06 D 3 8 2 4 X Z
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUCL1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 T1 OCCURS 2.
             03 T2 OCCURS 2.
                04 T3 OCCURS 2 PIC 9.
       01 N PIC 99 VALUE 0.
       01 M PIC 9.
       01 D PIC 9(6).
       01 X PIC X VALUE "X".
       PROCEDURE DIVISION.
       P0.
           ACCEPT D FROM DATE.
           MOVE 6 TO T3 (2, 2, 2).
           ADD T3 (2, 2, 2) TO N.
           DISPLAY N WITH NO ADVANCING.
      D    DISPLAY " D" WITH NO ADVANCING.
           MOVE 6 TO M.
           DIVIDE 2 INTO M.
           DISPLAY " " M WITH NO ADVANCING.
           MULTIPLY 2 BY M GIVING N.
           SUBTRACT 4 FROM N GIVING M.
           MOVE 8 TO M.
           DISPLAY " " M WITH NO ADVANCING.
           PERFORM P2.
           ALTER P3 TO PROCEED TO P5.
           GO TO P3.
       P2.
           SUBTRACT 6 FROM M.
           DISPLAY " " M WITH NO ADVANCING.
       P3.
           GO TO P4.
       P4.
           DISPLAY " WRONG".
           STOP RUN.
       P5.
           MULTIPLY 2 BY M.
           IF M = 4 DISPLAY " 4" WITH NO ADVANCING
           ELSE DISPLAY " WRONG".
           CONTINUE.
           PERFORM P6.
           STOP RUN.
       P6.
           DISPLAY " " X " Z".
       P7.
           EXIT.
