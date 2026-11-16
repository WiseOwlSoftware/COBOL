      *>--- conformance-case
      *> id: g85_bf04_05
      *> rules: [1985-GR-VI.6.4.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR4 - ADD and SUBTRACT CORRESPONDING ignore
      *> REDEFINES, RENAMES, OCCURS and USAGE INDEX subordinates
      * G1's subordinates C (REDEFINES), D (OCCURS), H/K (REDEFINES and
      * its subordinate), M/P (OCCURS and its subordinate), X1 (USAGE IS
      * INDEX) and the level-66 S (RENAMES) are "ignored, as well as
      * those data items subordinate to" them; A, E/F and T pair with
      * G2's (VI.6.4.3 GR1, GR2) and are added (VI.6.6.4 GR3). So ADD
      * gives A=1+100=101, F=3+100=103, T=4+100=104 and leaves C, D, K,
      * P, X1 and S at 100; SUBTRACT CORRESPONDING takes the same three
      * back to 100 and again leaves the ignored items alone. An
      * implementation that paired any ignored item answers other than
      * 100 for it (C 102, D 105, K 103, P 106, S 101).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0405.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
           05 A PIC 9(3) VALUE 1.
           05 B PIC 9(3) VALUE 2.
           05 C REDEFINES B PIC 9(3).
           05 D PIC 9(3) OCCURS 2 TIMES.
           05 E.
               10 F PIC 9(3) VALUE 3.
           05 H REDEFINES E.
               10 K PIC 9(3).
           05 M OCCURS 2 TIMES.
               10 P PIC 9(3).
           05 X1 USAGE IS INDEX.
           05 T PIC 9(3) VALUE 4.
       66 S RENAMES A OF G1.
       01 G2.
           05 A PIC 9(3) VALUE 100.
           05 C PIC 9(3) VALUE 100.
           05 D PIC 9(3) VALUE 100.
           05 E.
               10 F PIC 9(3) VALUE 100.
           05 H.
               10 K PIC 9(3) VALUE 100.
           05 M.
               10 P PIC 9(3) VALUE 100.
           05 X1 PIC 9(3) VALUE 100.
           05 S PIC 9(3) VALUE 100.
           05 T PIC 9(3) VALUE 100.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO D OF G1 (1) D OF G1 (2).
           MOVE 6 TO P OF G1 (1) P OF G1 (2).
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "ADD A=" A OF G2 " C=" C OF G2 " D=" D OF G2
               " F=" F OF G2 " K=" K OF G2.
           DISPLAY "ADD P=" P OF G2 " X1=" X1 OF G2 " S=" S OF G2
               " T=" T OF G2.
           SUBTRACT CORRESPONDING G1 FROM G2.
           DISPLAY "SUB A=" A OF G2 " C=" C OF G2 " D=" D OF G2
               " F=" F OF G2 " K=" K OF G2.
           DISPLAY "SUB P=" P OF G2 " X1=" X1 OF G2 " S=" S OF G2
               " T=" T OF G2.
           STOP RUN.
