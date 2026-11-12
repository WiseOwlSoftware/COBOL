      *>--- conformance-case
      *> id: 1985-GR-VI_6_4_3-4
      *> rules: [1985-GR-VI.6.4.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR4 (COMMON OPTIONS AND RULES FOR STATEMENTS).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CORRTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
           05 A PIC X(3) VALUE "AAA".
           05 B PIC X(3) VALUE "BBB".
           05 C REDEFINES B PIC X(3).
           05 D PIC X(3) OCCURS 2 TIMES.
           05 E.
               10 F PIC X(3) VALUE "FFF".
           05 H REDEFINES E.
               10 K PIC X(3).
           05 M OCCURS 2 TIMES.
               10 P PIC X(3).
           05 T PIC X(3) VALUE "TTT".
       66 S RENAMES A OF G1.
       01 G2.
           05 A PIC X(3) VALUE "ZZZ".
           05 C PIC X(3) VALUE "ZZZ".
           05 D PIC X(3) VALUE "ZZZ".
           05 H.
               10 K PIC X(3) VALUE "ZZZ".
           05 M.
               10 P PIC X(3) VALUE "ZZZ".
           05 S PIC X(3) VALUE "ZZZ".
           05 T PIC X(3) VALUE "ZZZ".
       PROCEDURE DIVISION.
       P1.
           MOVE "DDD" TO D OF G1 (1) D OF G1 (2).
           MOVE "PPP" TO P OF G1 (1) P OF G1 (2).
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY A OF G2.
           DISPLAY C OF G2.
           DISPLAY D OF G2.
           DISPLAY K OF G2.
           DISPLAY P OF G2.
           DISPLAY S OF G2.
           DISPLAY T OF G2.
           STOP RUN.
